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
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace MemberSite.Controllers.Api
{
    [Authorize]
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class AccountStrictController : ControllerBase
    {
        private readonly SelLangBLL _selLangBLL;
        private readonly AuthorizeBLL _authorizeBLL;
        private readonly CustInfoBll _custInfoBLL;
        private readonly BankInfoBLL _bankInfoBLL;
        private readonly CommonBLL _commonBLL;
        private readonly ServerInfoBll _serverInfoBLL;
        private readonly ImageEditorBLL _imageEditorBll;

        public AccountStrictController(IServiceProvider provider)
        {
            _selLangBLL = provider.GetRequiredService<SelLangBLL>();
            _authorizeBLL = provider.GetRequiredService<AuthorizeBLL>();
            _custInfoBLL = provider.GetRequiredService<CustInfoBll>();
            _bankInfoBLL = provider.GetRequiredService<BankInfoBLL>();
            _commonBLL = provider.GetRequiredService<CommonBLL>();
            _serverInfoBLL = provider.GetRequiredService<ServerInfoBll>();
            _imageEditorBll = provider.GetRequiredService<ImageEditorBLL>();
        }

        public class BankInfo
        {
            public int SysId { set; get; }
            public string AccountName { set; get; }
            public string AccountNo { set; get; }
            public string IFSCCode { set; get; }
        }

        [HttpPost]
        public ApiResult SetCustBankInfo(BankInfo bankInfo)
        {
            var apiResult = new ApiResult(_selLangBLL);
            var authData = _authorizeBLL.GetAuthData();
            List<BankInfoDetail> bankInfoList = null;
            var BankInfoLogList = new List<BankInfoDetail>();

            var custInfo = _custInfoBLL.GetCustInfoWithCache(authData.CustId);
            //
            var bankInfoSysList = _bankInfoBLL.GetBankInfoSysListByCurrencyForCtrl(custInfo.CurrencyId);

            if (bankInfoSysList == null || bankInfoSysList.Count(x => x.SysId == bankInfo.SysId) == 0)
            {
                apiResult.ErrorCode = -1;
                apiResult.Message = _selLangBLL.GetMsg("msg_UpdateFailed");
                return apiResult;
            }

            var custBankInfo = _custInfoBLL.GetCustBankInfo(authData.CustId);
            if (custBankInfo == null && string.IsNullOrEmpty(bankInfo.AccountName))
            {
                apiResult.ErrorCode = -2;
                apiResult.Message = _selLangBLL.GetMsg("msg_NeedAccountName");
                return apiResult;
            }
            else if (custBankInfo != null && custBankInfo.BankInfo != null && custBankInfo.bankInfoDetail?.Count() >= 3)
            {
                apiResult.ErrorCode = -3;
                apiResult.Message = _selLangBLL.GetMsg("msg_UpdateFailed");
                return apiResult;
            }
            else if (custBankInfo != null && custBankInfo.bankInfoDetail != null)
            {
                bankInfoList = custBankInfo.bankInfoDetail.ToList();

                if (bankInfoList.Count(x => x.SysId == bankInfo.SysId && x.AccountNo == bankInfo.AccountNo) > 0)
                {
                    apiResult.ErrorCode = -4;
                    apiResult.Message = _selLangBLL.GetMsg("msg_BankAccountExists");
                    return apiResult;
                }
            }
            else
            {
                bankInfoList = new List<BankInfoDetail>();
            }

            bankInfoList.Add(new BankInfoDetail { AccountNo = bankInfo.AccountNo, SysId = bankInfo.SysId, IFSCCode = bankInfo.IFSCCode });
            BankInfoLogList.Add(new BankInfoDetail { AccountNo = bankInfo.AccountNo, SysId = bankInfo.SysId, IFSCCode = bankInfo.IFSCCode });
            apiResult.ErrorCode = _custInfoBLL.InsertCustBankInfo(authData.CustId, bankInfo.AccountName, bankInfoList, BankInfoLogList);
            apiResult.Message = _selLangBLL.GetMsg("msg_Updatesuccessful");
            return apiResult;
        }

        [HttpPost]
        public ApiResult DelCustBankInfo(BankInfo bankInfo)
        {
            var apiResult = new ApiResult(_selLangBLL);
            var authData = _authorizeBLL.GetAuthData();
            IEnumerable<BankInfoDetail> bankInfoList = null;

            var custBankInfo = _custInfoBLL.GetCustBankInfo(authData.CustId);
            if (custBankInfo == null || custBankInfo.bankInfoDetail == null)
            {
                apiResult.ErrorCode = -2;
                apiResult.Message = _selLangBLL.GetMsg("msg_UpdateFailed");
                return apiResult;
            }
            else if (custBankInfo != null && custBankInfo.bankInfoDetail != null)
            {
                bankInfoList = custBankInfo.bankInfoDetail.Where(x => !(x.SysId == bankInfo.SysId && x.AccountNo == bankInfo.AccountNo));
            }

            apiResult.ErrorCode = _custInfoBLL.SetCustBankInfo(authData.CustId, custBankInfo.AccountName, bankInfoList);
            apiResult.Message = _selLangBLL.GetMsg("msg_Updatesuccessful");
            return apiResult;
        }
        public class USDTDetailreq
        {
            public string AccountName { set; get; }
            public string NickName { set; get; }
            public string USDTAddress { set; get; }
            public string Network { set; get; }
            public int CurrencyId { set; get; }
        }

        [HttpPost]
        public ApiResult SetCustUSDTInfo(USDTDetailreq uSDTDetailreq)
        {
            var apiResult = new ApiResult(_selLangBLL);
            var authData = _authorizeBLL.GetAuthData();

            List<USDTDetail> uSDTInfoList = null;
            var uSDTInfoLogList = new List<USDTDetail>();

            var custInfo = _custInfoBLL.GetCustInfoWithCache(authData.CustId);
            //if (custInfo.CurrencyId != 96)
            //{
            //    apiResult.ErrorCode = -6;
            //    apiResult.Message = "Error Currency ";
            //    return apiResult;
            //}
            //

            var custBankInfo = _custInfoBLL.GetCustBankInfo(authData.CustId);
            if (custBankInfo == null && string.IsNullOrEmpty(uSDTDetailreq.AccountName))
            {
                apiResult.ErrorCode = -2;
                apiResult.Message = _selLangBLL.GetMsg("msg_NeedAccountName");
                return apiResult;
            }
            else if (custBankInfo != null && custBankInfo.BankInfo != null && custBankInfo.usdtDetail?.Count() >= 3)
            {
                apiResult.ErrorCode = -3;
                apiResult.Message = _selLangBLL.GetMsg("msg_UpdateFailed");
                return apiResult;
            }
            else if (custBankInfo != null && custBankInfo.usdtDetail != null)
            {
                uSDTInfoList = custBankInfo.usdtDetail.ToList();

                if (uSDTInfoList.Any(x => x.USDTAddress == uSDTDetailreq.USDTAddress
                    && x.CurrencyId == uSDTDetailreq.CurrencyId)) // 可能幣種不同但錢包位址相同
                {
                    apiResult.ErrorCode = -4;
                    apiResult.Message = _selLangBLL.GetMsg("msg_UpdateFailed");
                    return apiResult;
                }
            }
            else
            {
                uSDTInfoList = new List<USDTDetail>();
            }

            var currencyId = uSDTDetailreq.CurrencyId == 0 ? 96 : uSDTDetailreq.CurrencyId;
            uSDTInfoList.Add(new USDTDetail { NickName = uSDTDetailreq.NickName, USDTAddress = uSDTDetailreq.USDTAddress , Network = uSDTDetailreq.Network, CurrencyId = currencyId });
            uSDTInfoLogList.Add(new USDTDetail { NickName = uSDTDetailreq.NickName, USDTAddress = uSDTDetailreq.USDTAddress , Network = uSDTDetailreq.Network, CurrencyId = currencyId });

            apiResult.ErrorCode = _custInfoBLL.InsertCustUSDTInfo(authData.CustId, uSDTDetailreq.AccountName, uSDTInfoList,uSDTInfoLogList);
            apiResult.Message = _selLangBLL.GetMsg("msg_Updatesuccessful");
            return apiResult;
        }
        public class CustUSDTInfoBatchreq
        {
            public string AccountName { set; get; }
            public IEnumerable<USDTDetail> USDTInfo { set; get; }
        }

        [HttpPost]
        public ApiResult SetCustUSDTInfoBatch(CustUSDTInfoBatchreq custUSDTInfoBatchreq)
        {
            var apiResult = new ApiResult(_selLangBLL);
            var authData = _authorizeBLL.GetAuthData();

            List<USDTDetail> uSDTInfoList = null;
            var uSDTInfoLogList = new List<USDTDetail>();
            
            if (custUSDTInfoBatchreq.USDTInfo.GroupBy(i => new { i.USDTAddress, i.CurrencyId }) // 可能幣種不同但錢包位址相同
                .Any(g => g.Count() > 1))
            {
                apiResult.ErrorCode = -4; //輸入資料重複
                apiResult.Message = _selLangBLL.GetMsg("msg_UpdateFailed");
                return apiResult;

            }
            var custBankInfo = _custInfoBLL.GetCustBankInfo(authData.CustId);
            if (custBankInfo == null && string.IsNullOrEmpty(custUSDTInfoBatchreq.AccountName))
            {
                apiResult.ErrorCode = -2;
                apiResult.Message = _selLangBLL.GetMsg("msg_NeedAccountName");
                return apiResult;
            }
            if (custBankInfo != null && custBankInfo.BankInfo != null && (custBankInfo.usdtDetail?.Count() >= 3 || (custBankInfo.usdtDetail?.Count() + custUSDTInfoBatchreq.USDTInfo?.Count() > 3)))
            {
                apiResult.ErrorCode = -3;
                apiResult.Message = _selLangBLL.GetMsg("msg_UpdateFailed");
                return apiResult;
            }
            if (custBankInfo != null && custBankInfo.usdtDetail != null)
            {
                uSDTInfoList = custBankInfo.usdtDetail.ToList();
                foreach (var custUSDTInfo in custUSDTInfoBatchreq.USDTInfo)
                {
                    if (uSDTInfoList.Any(x => x.USDTAddress == custUSDTInfo.USDTAddress
                        && x.CurrencyId == custUSDTInfo.CurrencyId)) // 可能幣種不同但錢包位址相同
                    {
                        apiResult.ErrorCode = -4;
                        apiResult.Message = _selLangBLL.GetMsg("msg_UpdateFailed");
                        return apiResult;
                    }
                }
            }
            else
            {
                uSDTInfoList = new List<USDTDetail>();
            }
            foreach (var custUSDTInfo in custUSDTInfoBatchreq.USDTInfo)
            {
                uSDTInfoList.Add(new USDTDetail { NickName = custUSDTInfo.NickName, USDTAddress = custUSDTInfo.USDTAddress, Network = custUSDTInfo.Network, CurrencyId = custUSDTInfo.CurrencyId });
                uSDTInfoLogList.Add(new USDTDetail { NickName = custUSDTInfo.NickName, USDTAddress = custUSDTInfo.USDTAddress , Network = custUSDTInfo.Network, CurrencyId = custUSDTInfo.CurrencyId });
            }
            
            apiResult.ErrorCode = _custInfoBLL.InsertCustUSDTInfo(authData.CustId, custUSDTInfoBatchreq.AccountName, uSDTInfoList ,uSDTInfoLogList );
            apiResult.Message = _selLangBLL.GetMsg("msg_Updatesuccessful");
            return apiResult;
        }
        [HttpPost]
        public ApiResult DelCustUSDTInfo(USDTDetailreq uSDTDetailreq)
        {
            var apiResult = new ApiResult(_selLangBLL);
            var authData = _authorizeBLL.GetAuthData();

            var custInfo = _custInfoBLL.GetCustInfoWithCache(authData.CustId);
            //if (custInfo.CurrencyId != 96)
            //{
            //    apiResult.ErrorCode = -6;
            //    apiResult.Message = "Error Currency ";
            //    return apiResult;
            //}

            IEnumerable<USDTDetail> USDTInfoList = null;

            var custBankInfo = _custInfoBLL.GetCustBankInfo(authData.CustId);
            if (custBankInfo == null || custBankInfo.usdtDetail == null)
            {
                apiResult.ErrorCode = -2;
                apiResult.Message = _selLangBLL.GetMsg("msg_UpdateFailed");
                return apiResult;
            }

            if (custBankInfo != null && custBankInfo.usdtDetail != null)
            {
                USDTInfoList = custBankInfo.usdtDetail.Where(x => 
                    !(x.USDTAddress == uSDTDetailreq.USDTAddress && 
                    x.CurrencyId == uSDTDetailreq.CurrencyId));
            }

            apiResult.ErrorCode = _custInfoBLL.SetCustUSDTInfo(authData.CustId, custBankInfo.AccountName, USDTInfoList);
            apiResult.Message = _selLangBLL.GetMsg("msg_Updatesuccessful");
            return apiResult;
        }
        //        public class SetAccountNameReq
        //        {
        //            public string Name { get; set; }
        //        }
        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="content"></param>
        ///// <returns></returns>
        //        [HttpPost]
        //        public ApiResult SetAccountName(SetAccountNameReq content)
        //        {
        //            var apiResult = new ApiResult(selLangBLL);
        //            var authData = authorizeBLL.GetAuthData();
        //            var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);
        //            //check input
        //            if (string.IsNullOrEmpty(content.Name))
        //            {
        //                apiResult.ErrorCode = -3;
        //                apiResult.Message = selLangBLL.GetMsg("msg_NeedAccountName");
        //                return apiResult;
        //            }

        //            if (content.Name.Length > 50)
        //            {
        //                apiResult.ErrorCode = -4;
        //                return apiResult;
        //            }

        //            //check AccountName exist or not
        //            //existed
        //            if (custInfoBLL.GetCustBankInfo(authData.CustId) != null)
        //            {
        //                apiResult.ErrorCode = -5;
        //                return apiResult;
        //            }
        //            //not existed, update (custInfoBLL.SetCustBankInfo only return 0 or -1)
        //            apiResult.ErrorCode = custInfoBLL.SetCustBankInfo(authData.CustId, content.Name, null);
        //            apiResult.Message = selLangBLL.GetMsg(apiResult.ErrorCode == 0 ? "msg_Updatesuccessful" : "msg_UpdateFailed");

        //            return apiResult;
        //        }

        public class SetAccountNameReq
        {
            [JsonProperty("name")]
            public string Name { get; set; }
        }

        /// <summary>
        /// For Goal 123 add account name direct
        /// </summary>
        /// <param name="content"></param>
        /// <returns>ApiResult</returns>
        [HttpPost]
        public ApiResult SetAccountName(SetAccountNameReq content)
        {
            var apiResult = new ApiResult(_selLangBLL);
            var authData = _authorizeBLL.GetAuthData();
            //check input
            if (string.IsNullOrEmpty(content.Name))
            {
                apiResult.ErrorCode = -3;
                apiResult.Message = _selLangBLL.GetMsg("msg_NeedAccountName");
                return apiResult;
            }
            if (content.Name.Length > 50)
            {
                apiResult.ErrorCode = -4;
                return apiResult;
            }

            //check AccountName exist or not
            //existed
            if (_custInfoBLL.CheckAccountName(authData.CustId))
            {
                apiResult.ErrorCode = -5;
                return apiResult;
            }
            //not existed, update (custInfoBLL.SetCustBankInfo only return 0 or -1)
            apiResult.ErrorCode = _custInfoBLL.SetCustBankInfo(authData.CustId, content.Name, null);
            apiResult.Message = _selLangBLL.GetMsg(apiResult.ErrorCode == 0 ? "msg_Updatesuccessful" : "msg_UpdateFailed");

            return apiResult;
        }

        [HttpPost]
        public ApiResult<object> SetProfile(Dictionary<string, string> custProfile)
        {
            var apiResult = new ApiResult<object>(_selLangBLL);
            var authData = _authorizeBLL.GetAuthData();

            var beforeCustInfo = _custInfoBLL.GetCustInfoProfile(authData.CustId);
            if (string.IsNullOrEmpty(beforeCustInfo.AccountName) && !string.IsNullOrEmpty(beforeCustInfo.PhoneNo))
            {
                string AccountName = custProfile[nameof(AccountName)];
                var result = _custInfoBLL.SetCustBankInfo(authData.CustId, AccountName, null);
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

                if (long.TryParse(PhoneNo, out var phoneNo))
                {
                    PhoneNo = phoneNo.ToString();
                }
                else
                {
                    apiResult.ErrorCode = 21;
                    apiResult.Message = $"({AreaCode}){PhoneNo} {_selLangBLL.GetMsg("msg_PhoneWrongFormat")}";
                    return apiResult;
                }

                if (_custInfoBLL.ExistsPhoneNo(authData.CustId, AreaCode, PhoneNo))
                {
                    apiResult.ErrorCode = 21;
                    apiResult.Message = $"({AreaCode}){PhoneNo} {_selLangBLL.GetMsg("msg_HasExist")}";
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
                    var result = _custInfoBLL.SetCustBankInfo(authData.CustId, custProfile["AccountName"], null);
                }
            }
            
            if (custProfile.TryGetValue("Email", out var email) && !string.IsNullOrEmpty(email) &&
                _custInfoBLL.ExistsEmail(authData.CustId, email))
            {
                apiResult.ErrorCode = 21;
                apiResult.Message = $"{email} {_selLangBLL.GetMsg("msg_HasExist")}";
                return apiResult;
            }

            if (!string.IsNullOrEmpty(beforeCustInfo.Email))
            {
                custProfile["Email"] = beforeCustInfo.Email;
            }

            apiResult.ErrorCode = _custInfoBLL.SetProfile(authData.CustId, custProfile);
            if (apiResult.ErrorCode == 0)
            {
                apiResult.Message = _selLangBLL.GetMsg("msg_Updatesuccessful");

                var custInfo = _custInfoBLL.GetCustInfoProfile(authData.CustId);
                apiResult.Data = new
                {
                    LangCode = _selLangBLL.GetLangId(),
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
                apiResult.Message = _selLangBLL.GetMsg("msg_UpdateFailed");
            }

            return apiResult;
        }

        [HttpPost]
        public ApiResult<object> SetProfileV2(Dictionary<string, string> custProfile)
        {
            var apiResult = new ApiResult<object>(_selLangBLL);
            var authData = _authorizeBLL.GetAuthData();
            try
            {
                var beforeCustInfo = _custInfoBLL.GetCustInfoProfile(authData.CustId);
                if (string.IsNullOrEmpty(beforeCustInfo.AccountName) && !string.IsNullOrEmpty(beforeCustInfo.PhoneNo))
                {
                    string AccountName = custProfile[nameof(AccountName)];
                    var result = _custInfoBLL.SetCustBankInfo(authData.CustId, AccountName, null);

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

                    if (long.TryParse(PhoneNo, out var phoneNo))
                    {
                        PhoneNo = phoneNo.ToString();
                    }
                    else
                    {
                        apiResult.ErrorCode = 21;
                        apiResult.Message = $"({AreaCode}){PhoneNo} {_selLangBLL.GetMsg("msg_PhoneWrongFormat")}";
                        return apiResult;
                    }

                    if (_custInfoBLL.ExistsPhoneNo(authData.CustId, AreaCode, PhoneNo))
                    {
                        apiResult.ErrorCode = 21;
                        apiResult.Message = $"({AreaCode}){PhoneNo} {_selLangBLL.GetMsg("msg_HasExist")}";
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
                        var result = _custInfoBLL.SetCustBankInfo(authData.CustId, custProfile["AccountName"], null);
                    }
                }

                if (custProfile.TryGetValue("Email", out var email) && !string.IsNullOrEmpty(email) &&
                    _custInfoBLL.ExistsEmail(authData.CustId, email))
                {
                    apiResult.ErrorCode = 21;
                    apiResult.Message = $"{email} {_selLangBLL.GetMsg("msg_HasExist")}";
                    return apiResult;
                }

                if (!string.IsNullOrEmpty(beforeCustInfo.Email))
                {
                    custProfile["Email"] = beforeCustInfo.Email;
                }

                apiResult.ErrorCode = _custInfoBLL.SetProfileV2(authData.CustId, custProfile);
                if (apiResult.ErrorCode == 0)
                {
                    apiResult.Message = _selLangBLL.GetMsg("msg_Updatesuccessful");

                    var custInfo = _custInfoBLL.GetCustInfoProfile(authData.CustId);
                    apiResult.Data = new
                    {
                        LangCode = _selLangBLL.GetLangId(),
                        custInfo.UserName,
                        custInfo.Email,
                        custInfo.AccountName,
                        custInfo.Birth_d,
                        custInfo.Birth_m,
                        custInfo.Birth_y,
                        custInfo.RegTime,
                        LastLogin = custInfo.LastLoginTime,
                        Country = custInfo.CountryCode,
                        custInfo.AreaCode,
                        custInfo.PhoneNo
                    };
                }
                else
                {
                    apiResult.Message = _selLangBLL.GetMsg("msg_UpdateFailed");
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
            var apiResult = new ApiResult<object>(_selLangBLL);
            var authData = _authorizeBLL.GetAuthData();
            var PerCountry = new Dictionary<int, string>()
            {
                    {2,"MY" },{4,"TH" },{13,"CS" },{15,"ID" },{51,"VN" },{61,"IN" },{96,"MY" }
                };

            try
            {
                var custInfo = _custInfoBLL.GetCustInfoProfile(authData.CustId);
                var _perCountry = "";
                if (custInfo != null)
                {
                    if (custInfo.CurrencyId == 96)
                    {
                        _perCountry = _custInfoBLL.GetCountryNameByAreaCode(custInfo.AreaCode);
                    }
                    else
                    {
                        PerCountry.TryGetValue(custInfo.CurrencyId, out _perCountry);
                    }
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
                    Country = string.IsNullOrEmpty(custInfo.CountryCode) ? null : custInfo.CountryCode,
                    custInfo.AreaCode,
                    custInfo.PhoneNo,
                    PerCountry = _perCountry,
                    custInfo.CurrencyId,
                    langId = custInfo.UserLang,
                    custInfo.UserLevel,
                    custInfo.PhoneVerified,
                    custInfo.EnablePromotion,
                    custInfo.ActStatus
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
        public async Task<ApiResult> UploadCustPhotoId([FromForm] CustPhotoIdReq custPhotoIdReq)
        {
            var apiResult = new ApiResult();
            var authData = _authorizeBLL.GetAuthData();
            var custInfo = _custInfoBLL.GetCustInfoWithCache(authData.CustId);

            if (!_contentTypesCheck.ContainsKey(custPhotoIdReq.PhotoId.ContentType))
            {
                return new ApiResult { ErrorCode = -1 , Message = _selLangBLL.GetMsg("msg_UpdateFailed") };
            }

            using var fileStream = custPhotoIdReq.PhotoId.OpenReadStream();
            using var uploadedImage = await SixLabors.ImageSharp.Image.LoadAsync(fileStream);
            var fileBytes = await _imageEditorBll.Resize(uploadedImage);
                
            var fileName = custPhotoIdReq.PhotoId.FileName;
            fileName = fileName.Substring(fileName.LastIndexOf('\\') + 1);
            fileName = $"{fileName.Substring(0, fileName.LastIndexOf('.'))}.jpg";

            var result = _custInfoBLL.SetCustPhotoId(custInfo.CustId, custInfo.SiteId, fileName, fileBytes);

            if (result > 0)
            {
                apiResult.ErrorCode = 0;
            }
            else
            {
                apiResult.ErrorCode = -1;
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
            var authData = _authorizeBLL.GetAuthData();
            if (_custInfoBLL.SetResetPwd(authData.CustId, resetPassword.NewPassword))
            {
                apiResult.ErrorCode = 0;
                apiResult.Message = _selLangBLL.GetMsg("ResetPwdSuccess");
            }
            else
            {
                apiResult.Message = _selLangBLL.GetMsg("msg_UpdateFailed");
            }

            return apiResult;
        }

        [HttpPost]
        public async Task<ApiResult> SendVerifySMSAsync()
        {
            var authData = _authorizeBLL.GetAuthData();
            var apiResult = await _custInfoBLL.SendVerifySMSAsync(authData.CustId);

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
            var authData = _authorizeBLL.GetAuthData();
            var apiResult = await _custInfoBLL.AcknowledgeVerifySMS(authData.CustId, _serverInfoBLL.GetServerInfo().SiteName, verifyPhoneReq.request_id, verifyPhoneReq.code);

            return apiResult;
        }


        [HttpPost]
        public async Task<ApiResult<SendOTPSMSResult>> SendOTPSMSAsync()
        {
            var authData = _authorizeBLL.GetAuthData();
            var apiResult = await _commonBLL.SendOTPSMSAsync(authData.CustId);

            return apiResult;
        }
        public class VerifyOTPReq
        {
            public string inputOTPcode { set; get; }
        }

        [HttpPost]
        public async Task<ApiResult> VerifyOTPAsync(VerifyOTPReq verifyOTPReq)
        {
            var authData = _authorizeBLL.GetAuthData();
            var apiResult = await _custInfoBLL.VerifyOTPAsync(authData.CustId, verifyOTPReq.inputOTPcode);

            return apiResult;
        }

        [HttpPost]
        public async Task<ApiResult<SendOTPSMSResult>> GetOTPVerifyInfo()
        {
            var authData = _authorizeBLL.GetAuthData();
            var apiResult = await _commonBLL.GetOTPVerifyInfo(authData.CustId);

            return apiResult;
        }
    }
}