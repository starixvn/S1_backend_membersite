using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using LazZiya.ImageResize;
using MemberSite.Service.BLL;
using MemberSite.Service.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MemberSite.Controllers.Api
{
    [Authorize]
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class AccountStrictController : ControllerBase
    {
        private readonly SelLangBLL selLangBLL;
        private readonly AuthorizeBLL authorizeBLL;
        private readonly CustInfoBLL custInfoBLL;
        private readonly BankInfoBLL bankInfoBLL;
        private readonly CommonBLL commonBLL;
        private readonly ServerInfoBLL serverInfoBLL;

        public AccountStrictController(SelLangBLL selLangBLL, AuthorizeBLL authorizeBLL, CustInfoBLL custInfoBLL, BankInfoBLL bankInfoBLL, CommonBLL commonBLL, ServerInfoBLL serverInfoBLL)
        {
            this.selLangBLL = selLangBLL;
            this.authorizeBLL = authorizeBLL;
            this.custInfoBLL = custInfoBLL;
            this.bankInfoBLL = bankInfoBLL;
            this.commonBLL = commonBLL;
            this.serverInfoBLL = serverInfoBLL;
        }
        public class BankInfo
        {
            public int SysId { set; get; }
            public string AccountName { set; get; }
            public string AccountNo { set; get; }
        }

        [HttpPost]
        public ApiResult SetCustBankInfo(BankInfo bankInfo)
        {
            var apiResult = new ApiResult(selLangBLL);
            var authData = authorizeBLL.GetAuthData();
            List<BankInfoDetail> bankInfoList = null;

            var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);
            //
            var bankInfoSysList = bankInfoBLL.GetBankInfoSysListByCurrencyForCtrl(custInfo.CurrencyId);

            if (bankInfoSysList == null || bankInfoSysList.Where(x => x.SysId == bankInfo.SysId).Count() == 0)
            {
                apiResult.ErrorCode = -1;
                apiResult.Message = selLangBLL.GetMsg("msg_UpdateFailed");
                return apiResult;
            }

            var custBankInfo = custInfoBLL.GetCustBankInfo(authData.CustId);
            if (custBankInfo == null && string.IsNullOrEmpty(bankInfo.AccountName))
            {
                apiResult.ErrorCode = -2;
                apiResult.Message = selLangBLL.GetMsg("msg_UpdateFailed");
                return apiResult;
            }
            else if (custBankInfo != null && custBankInfo.BankInfo != null && custBankInfo.bankInfoDetail?.Count() >= 3)
            {
                apiResult.ErrorCode = -3;
                apiResult.Message = selLangBLL.GetMsg("msg_UpdateFailed");
                return apiResult;
            }
            else if (custBankInfo != null && custBankInfo.bankInfoDetail != null)
            {
                bankInfoList = custBankInfo.bankInfoDetail.ToList();

                if (bankInfoList.Where(x => x.SysId == bankInfo.SysId && x.AccountNo == bankInfo.AccountNo).Count() > 0)
                {
                    apiResult.ErrorCode = -4;
                    apiResult.Message = selLangBLL.GetMsg("msg_UpdateFailed");
                    return apiResult;
                }
            }
            else
            {
                bankInfoList = new List<BankInfoDetail>();
            }

            bankInfoList.Add(new BankInfoDetail { AccountNo = bankInfo.AccountNo, SysId = bankInfo.SysId });

            apiResult.ErrorCode = custInfoBLL.SetCustBankInfo(authData.CustId, bankInfo.AccountName, bankInfoList);
            apiResult.Message = selLangBLL.GetMsg("msg_Updatesuccessful");
            return apiResult;
        }

        [HttpPost]
        public ApiResult DelCustBankInfo(BankInfo bankInfo)
        {
            var apiResult = new ApiResult(selLangBLL);
            var authData = authorizeBLL.GetAuthData();
            IEnumerable<BankInfoDetail> bankInfoList = null;

            var custBankInfo = custInfoBLL.GetCustBankInfo(authData.CustId);
            if (custBankInfo == null || custBankInfo.BankInfo == null)
            {
                apiResult.ErrorCode = -2;
                apiResult.Message = selLangBLL.GetMsg("msg_UpdateFailed");
                return apiResult;
            }
            else if (custBankInfo != null && custBankInfo.bankInfoDetail != null)
            {
                bankInfoList = custBankInfo.bankInfoDetail.Where(x => !(x.SysId == bankInfo.SysId && x.AccountNo == bankInfo.AccountNo));
            }

            apiResult.ErrorCode = custInfoBLL.SetCustBankInfo(authData.CustId, custBankInfo.AccountName, bankInfoList);
            apiResult.Message = selLangBLL.GetMsg("msg_Updatesuccessful");
            return apiResult;
        }
        public class SetAccountNameReq
        {
            public string Name { get; set; }
        }
/// <summary>
/// 
/// </summary>
/// <param name="content"></param>
/// <returns></returns>
        [HttpPost]
        public ApiResult SetAccountName(SetAccountNameReq content)
        {
            var apiResult = new ApiResult(selLangBLL);
            var authData = authorizeBLL.GetAuthData();
            var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);
            //check input
            if (string.IsNullOrEmpty(content.Name))
            {
                apiResult.ErrorCode = -3;
                apiResult.Message = selLangBLL.GetMsg("msg_NeedAccountName");
                return apiResult;
            }

            if (content.Name.Length > 50)
            {
                apiResult.ErrorCode = -4;
                return apiResult;
            }

            //check AccountName exist or not
            //existed
            if (custInfoBLL.GetCustBankInfo(authData.CustId) != null)
            {
                apiResult.ErrorCode = -5;
                return apiResult;
            }
            //not existed, update (custInfoBLL.SetCustBankInfo only return 0 or -1)
            apiResult.ErrorCode = custInfoBLL.SetCustBankInfo(authData.CustId, content.Name, null);
            apiResult.Message = selLangBLL.GetMsg(apiResult.ErrorCode == 0 ? "msg_Updatesuccessful" : "msg_UpdateFailed");

            return apiResult;
        }

        [HttpPost]
        public ApiResult<object> SetProfile(Dictionary<string, string> custProfile)
        {
            var apiResult = new ApiResult<object>(selLangBLL);
            var authData = authorizeBLL.GetAuthData();

            var beforeCustInfo = custInfoBLL.GetCustInfoProfile(authData.CustId);
            if (string.IsNullOrEmpty(beforeCustInfo.AccountName) && !string.IsNullOrEmpty(beforeCustInfo.PhoneNo))
            {
                string AccountName = custProfile[nameof(AccountName)];
                var result = custInfoBLL.SetCustBankInfo(authData.CustId, AccountName, null);
                if (!string.IsNullOrEmpty(beforeCustInfo.CountryCode))
                    custProfile["CountryCode"] = beforeCustInfo.CountryCode;
                if (!string.IsNullOrEmpty(beforeCustInfo.AreaCode))
                    custProfile["AreaCode"] = beforeCustInfo.AreaCode;
                if (!string.IsNullOrEmpty(beforeCustInfo.PhoneNo))
                    custProfile["PhoneNo"] = beforeCustInfo.PhoneNo;
            }
            else
            {
                string AreaCode = custProfile[nameof(AreaCode)];
                string PhoneNo = custProfile[nameof(PhoneNo)];
                if (custInfoBLL.ExistsPhoneNo(authData.CustId, AreaCode, PhoneNo))
                {
                    apiResult.ErrorCode = 21;
                    apiResult.Message = $"({AreaCode}){PhoneNo} " + selLangBLL.GetMsg("msg_HasExist");
                    return apiResult;
                }

                if (!string.IsNullOrEmpty(beforeCustInfo.Birth_d))
                    custProfile["Birth_d"] = beforeCustInfo.Birth_d;
                if (!string.IsNullOrEmpty(beforeCustInfo.Birth_m))
                    custProfile["Birth_m"] = beforeCustInfo.Birth_m;
                if (!string.IsNullOrEmpty(beforeCustInfo.Birth_y))
                    custProfile["Birth_y"] = beforeCustInfo.Birth_y;

                if (!string.IsNullOrEmpty(beforeCustInfo.AccountName))
                {
                    custProfile["AccountName"] = beforeCustInfo.AccountName;
                }
                else
                {
                    var result = custInfoBLL.SetCustBankInfo(authData.CustId, custProfile["AccountName"], null);
                }
            }

            if (!string.IsNullOrEmpty(beforeCustInfo.Email))
            {
                custProfile["Email"] = beforeCustInfo.Email;
            }

            apiResult.ErrorCode = custInfoBLL.SetProfile(authData.CustId, custProfile);
            if (apiResult.ErrorCode == 0)
            {
                apiResult.Message = selLangBLL.GetMsg("msg_Updatesuccessful");

                var custInfo = custInfoBLL.GetCustInfoProfile(authData.CustId);
                apiResult.Data = new
                {
                    LangCode = selLangBLL.GetLangId(),
                    custInfo.UserName,
                    custInfo.Email,
                    custInfo.AccountName,
                    custInfo.Birth_d,
                    custInfo.Birth_m,
                    custInfo.Birth_y,
                    custInfo.RegTime,
                    LastLogin = custInfo.LastLoginTime,
                    Country = custInfo.CountryCode,
                    custInfo.PhoneNo
                };
            }
            else
            {
                apiResult.Message = selLangBLL.GetMsg("msg_UpdateFailed");
            }

            return apiResult;
        }

        [HttpPost]
        public ApiResult<object> SetProfileV2(Dictionary<string, string> custProfile)
        {
            var apiResult = new ApiResult<object>(selLangBLL);
            var authData = authorizeBLL.GetAuthData();
            try
            {
                var beforeCustInfo = custInfoBLL.GetCustInfoProfile(authData.CustId);
                if (string.IsNullOrEmpty(beforeCustInfo.AccountName) && !string.IsNullOrEmpty(beforeCustInfo.PhoneNo))
                {
                    string AccountName = custProfile[nameof(AccountName)];
                    var result = custInfoBLL.SetCustBankInfo(authData.CustId, AccountName, null);

                    if (!string.IsNullOrEmpty(beforeCustInfo.CountryCode))
                        custProfile["CountryCode"] = beforeCustInfo.CountryCode;

                    if (!string.IsNullOrEmpty(beforeCustInfo.AreaCode))
                        custProfile["AreaCode"] = beforeCustInfo.AreaCode;

                    if (!string.IsNullOrEmpty(beforeCustInfo.PhoneNo))
                        custProfile["PhoneNo"] = beforeCustInfo.PhoneNo;
                }
                else
                {
                    string AreaCode = custProfile[nameof(AreaCode)];
                    string PhoneNo = custProfile[nameof(PhoneNo)];
                    if (custInfoBLL.ExistsPhoneNo(authData.CustId, AreaCode, PhoneNo))
                    {
                        apiResult.ErrorCode = 21;
                        apiResult.Message = $"({AreaCode}){PhoneNo} " + selLangBLL.GetMsg("msg_HasExist");
                        return apiResult;
                    }

                    if (!string.IsNullOrEmpty(beforeCustInfo.Birth_d))
                        custProfile["Birth_d"] = beforeCustInfo.Birth_d;

                    if (!string.IsNullOrEmpty(beforeCustInfo.Birth_m))
                        custProfile["Birth_m"] = beforeCustInfo.Birth_m;

                    if (!string.IsNullOrEmpty(beforeCustInfo.Birth_y))
                        custProfile["Birth_y"] = beforeCustInfo.Birth_y;

                    if (!string.IsNullOrEmpty(beforeCustInfo.AccountName))
                    {
                        custProfile["AccountName"] = beforeCustInfo.AccountName;
                    }
                    else
                    {
                        var result = custInfoBLL.SetCustBankInfo(authData.CustId, custProfile["AccountName"], null);
                    }
                }

                if (!string.IsNullOrEmpty(beforeCustInfo.Email))
                {
                    custProfile["Email"] = beforeCustInfo.Email;
                }

                apiResult.ErrorCode = custInfoBLL.SetProfileV2(authData.CustId, custProfile);
                if (apiResult.ErrorCode == 0)
                {
                    apiResult.Message = selLangBLL.GetMsg("msg_Updatesuccessful");

                    var custInfo = custInfoBLL.GetCustInfoProfile(authData.CustId);
                    apiResult.Data = new
                    {
                        LangCode = selLangBLL.GetLangId(),
                        custInfo.UserName,
                        custInfo.Email,
                        custInfo.AccountName,
                        custInfo.Birth_d,
                        custInfo.Birth_m,
                        custInfo.Birth_y,
                        custInfo.RegTime,
                        LastLogin = custInfo.LastLoginTime,
                        Country = custInfo.CountryCode,
                        custInfo.PhoneNo
                    };
                }
                else
                {
                    apiResult.Message = selLangBLL.GetMsg("msg_UpdateFailed");
                }
            }
            catch (Exception ex)
            {
                apiResult.ErrorCode = 9000;
                apiResult.RawMessage = ex.ToString();
                apiResult.Message = ex.ToString();
            }
            return apiResult;
        }

        [HttpPost]
        public ApiResult<object> GetCustProfile()
        {
            var apiResult = new ApiResult<object>(selLangBLL);
            var authData = authorizeBLL.GetAuthData();
            Dictionary<int, string> PerCountry = new Dictionary<int, string>()
            {
                    {2,"MY" },{4,"TH" },{13,"CS" },{15,"ID" },{51,"VN" }
                };

            try
            {
                var custInfo = custInfoBLL.GetCustInfoProfile(authData.CustId);
                string _perCountry = "";
                if (custInfo != null)
                {
                    PerCountry.TryGetValue(custInfo.CurrencyId, out _perCountry);
                }

                apiResult.Data = new
                {
                    custInfo.UserName,
                    custInfo.Email,
                    custInfo.AccountName,
                    custInfo.Birth_d,
                    custInfo.Birth_m,
                    custInfo.Birth_y,
                    custInfo.RegTime,
                    LastLogin = custInfo.LastLoginTime,
                    Country = custInfo.CountryCode,
                    custInfo.PhoneNo,
                    PerCountry = _perCountry,
                    custInfo.CurrencyId,
                    langId = custInfo.UserLang,
                    custInfo.UserLevel,
                    custInfo.PhoneVerified
                };

                apiResult.ErrorCode = 0;
            }
            catch (Exception ex)
            {
                apiResult.RawMessage = ex.Message;
            }

            return apiResult;
        }

        private readonly Dictionary<string, System.Drawing.Imaging.ImageFormat> _contentTypesCheck = new Dictionary<string, System.Drawing.Imaging.ImageFormat>(comparer: StringComparer.OrdinalIgnoreCase)
        {
            {"image/png",System.Drawing.Imaging.ImageFormat.Png},
            {"image/jpeg",System.Drawing.Imaging.ImageFormat.Jpeg},
            { "image/gif",System.Drawing.Imaging.ImageFormat.Gif}
        };
        public class CustPhotoIdReq
        {
            public IFormFile PhotoId { get; set; }
        }

        [HttpPost]
        public ApiResult UploadCustPhotoId([FromForm] CustPhotoIdReq custPhotoIdReq)
        {
            decimal xSize = 500m;
            var apiResult = new ApiResult();
            var authData = authorizeBLL.GetAuthData();
            var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);
            decimal xCell = 1;
            if (_contentTypesCheck.ContainsKey(custPhotoIdReq.PhotoId.ContentType))
            {
                using var fileStream = custPhotoIdReq.PhotoId.OpenReadStream();
                using var uploadedImage = Image.FromStream(fileStream);
                using var ms = new System.IO.MemoryStream();

                if (uploadedImage.Width > xSize || uploadedImage.Height > xSize)
                {

                    if (uploadedImage.Width > uploadedImage.Height)
                    {
                        xCell = xSize / uploadedImage.Width;
                    }
                    else
                    {
                        xCell = xSize / uploadedImage.Height;
                    }

                }

                var graphicOptions = new GraphicOptions();
                graphicOptions.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
                graphicOptions.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;
                var img = ImageResize.Scale(uploadedImage, Convert.ToInt32(uploadedImage.Width * xCell), Convert.ToInt32(uploadedImage.Height * xCell), graphicOptions);
                img.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);

                string fileName = custPhotoIdReq.PhotoId.FileName;
                fileName = fileName.Substring(fileName.LastIndexOf('\\') + 1);
                fileName = $"{fileName.Substring(0, fileName.LastIndexOf('.'))}.jpg";

                byte[] fileBytes = ms.ToArray();
                var result = custInfoBLL.SetCustPhotoId(custInfo.CustId, custInfo.SiteId, fileName, fileBytes);

                if (result > 0)
                {
                    apiResult.ErrorCode = 0;
                }
                else
                {
                    apiResult.ErrorCode = -1;
                }
            }
            else
            {

            }

            return apiResult;
        }

        public class ResetPasswordModel
        {
            public string NewPassword { set; get; }
        }

        [HttpPost]
        public ApiResult ResetPassword(ResetPasswordModel resetPassword)
        {
            var apiResult = new ApiResult();
            var authData = authorizeBLL.GetAuthData();
            if (custInfoBLL.SetResetPwd(authData.CustId, resetPassword.NewPassword))
            {
                apiResult.ErrorCode = 0;
                apiResult.Message = selLangBLL.GetMsg("ResetPwdSuccess");
            }
            else
            {
                apiResult.Message = selLangBLL.GetMsg("msg_UpdateFailed");
            }

            return apiResult;
        }

        [HttpPost]
        public async Task<ApiResult> SendVerifySMSAsync()
        {
            var authData = authorizeBLL.GetAuthData();
            var apiResult = await custInfoBLL.SendVerifySMSAsync(authData.CustId);

            return apiResult;
        }

        public class VerifyPhoneReq
        {
            public string request_id { set; get; }
            public string code { set; get; }
        }
        [HttpPost]
        public async Task<ApiResult> VerifyPhoneAsync(VerifyPhoneReq verifyPhoneReq)
        {
            var authData = authorizeBLL.GetAuthData();
            var apiResult = await custInfoBLL.AcknowledgeVerifySMS(authData.CustId, serverInfoBLL.GetServerInfo().SiteName, verifyPhoneReq.request_id, verifyPhoneReq.code);

            return apiResult;
        }


        [HttpPost]
        public async Task<ApiResult<SendOTPSMSResult>> SendOTPSMSAsync()
        {
            var authData = authorizeBLL.GetAuthData();
            var apiResult = await commonBLL.SendOTPSMSAsync(authData.CustId);

            return apiResult;
        }
        public class VerifyOTPReq
        {
            public string inputOTPcode { set; get; }
        }

        [HttpPost]
        public async Task<ApiResult> VerifyOTPAsync(VerifyOTPReq verifyOTPReq)
        {
            var authData = authorizeBLL.GetAuthData();
            var apiResult = await custInfoBLL.VerifyOTPAsync(authData.CustId, verifyOTPReq.inputOTPcode);

            return apiResult;
        }
    }
}