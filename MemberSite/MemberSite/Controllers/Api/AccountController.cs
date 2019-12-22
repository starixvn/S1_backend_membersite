using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MemberSite.Service.Model;
using MemberSite.Service.BLL;
using System.Security.Claims;
using MemberSite.Service.Library;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;

namespace MemberSite.Controllers.Api
{
    [Authorize]
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly CustInfoBLL custInfoBLL;
        private readonly Tools tools;
        private readonly SelLangBLL selLangBLL;
        private readonly AuthorizeBLL authorizeBLL;
        private readonly BankInfoBLL bankInfoBLL;
        private readonly WalletBLL walletBLL;
        private readonly CourierAdapter courierAdapter;
        private readonly string apiUrl;
        private readonly int mainWalletType;
        private readonly MessageBLL messageBLL;

        public AccountController(IConfiguration configuration, CustInfoBLL custInfoBLL, Tools tools, SelLangBLL selLangBLL, AuthorizeBLL authorizeBLL, BankInfoBLL bankInfoBLL, WalletBLL walletBLL, CourierAdapter courierAdapter, MessageBLL messageBLL)
        {
            this.custInfoBLL = custInfoBLL;
            this.tools = tools;
            this.selLangBLL = selLangBLL;
            this.authorizeBLL = authorizeBLL;
            this.bankInfoBLL = bankInfoBLL;
            this.walletBLL = walletBLL;
            this.courierAdapter = courierAdapter;
            apiUrl = configuration.GetValue<string>("TpService");
            mainWalletType = configuration.GetValue<int>("MainWalletType");
            this.messageBLL = messageBLL;
        }

        [HttpPost]
        public async Task<ApiResult> onlineCheck()
        {
            var apiResult = new ApiResult(selLangBLL);
            bool result = authorizeBLL.LoginCheckin();
            if (!result)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                apiResult.ErrorCode = 1001;
                apiResult.Message = selLangBLL.GetMsg("msg_ForceLogedout");
            }
            else
            {
                apiResult.ErrorCode = 0;
            }

            return apiResult;
        }

        [HttpPost]
        public async Task<ApiResult> SignOut()
        {
            var apiResult = new ApiResult(selLangBLL);
            bool result = authorizeBLL.Logout();

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            apiResult.ErrorCode = 0;
            apiResult.Message = selLangBLL.GetMsg("msg_loggedout");

            return apiResult;
        }

        public class LanguageModel
        {
            public string LangId { set; get; }
        }

        [HttpPost]
        public async Task<ApiResult> SetLanguage(LanguageModel languageModel)
        {
            var apiResult = new ApiResult(selLangBLL);
            var authData = authorizeBLL.GetAuthData();
            int result = custInfoBLL.SetLanguage(authData.CustId, languageModel.LangId);

            if (result == -1)
            {
                apiResult.ErrorCode = result;
                apiResult.Message = "";/*Not Support Language*/
                return apiResult;
            }

            // create a new identity from the old one
            var identity = new ClaimsIdentity(User.Identity);

            // update claim value
            identity.RemoveClaim(identity.FindFirst("LangId"));
            identity.AddClaim(new Claim("LangId", languageModel.LangId));

            // tell the SignInAsync to use this new identity
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

            apiResult.ErrorCode = 0;
            apiResult.Message = Url.RouteUrl("desktop", new { action = HttpContext.Request.RouteValues["action"], controller = HttpContext.Request.RouteValues["controller"], languageModel.LangId });

            return apiResult;
        }

        public class ChangePasswordModel
        {
            public string OldPassword { set; get; }
            public string NewPassword { set; get; }
        }

        [HttpPost]/*會驗證[舊密碼]所以可以放在不嚴格的*/
        public ApiResult ChangePassword(ChangePasswordModel changePasswordModel)
        {
            string oldPassword = changePasswordModel.OldPassword;
            string newPassword = changePasswordModel.NewPassword;

            var apiResult = new ApiResult(selLangBLL);
            var authData = authorizeBLL.GetAuthData();

            if (oldPassword == newPassword)
            {
                apiResult.ErrorCode = -1;
                apiResult.Message = selLangBLL.GetMsg("msg_SamePassword");
            }
            else if (!tools.CheckUserInputPassword(newPassword))
            {
                apiResult.ErrorCode = -2;
                apiResult.Message = selLangBLL.GetMsg("msg_PasswordVaildate");
            }
            else
            {
                apiResult.ErrorCode = custInfoBLL.SetPassword(authData.CustId, oldPassword, newPassword);

                if (apiResult.ErrorCode == -3)
                {
                    apiResult.Message = selLangBLL.GetMsg("msg_InvalidOldPassword");
                }
                else if (apiResult.ErrorCode == -4)
                {
                    apiResult.Message = selLangBLL.GetMsg("msg_UpdateFailed");
                }
                else
                {
                    apiResult.ErrorCode = 0;
                    apiResult.Message = selLangBLL.GetMsg("msg_Updatesuccessful");
                }
            }

            return apiResult;
        }

        public class ProfileModel
        {
            public string Email { set; get; }
        }

        [HttpPost]
        public ApiResult<bool> ExistsEmail(ProfileModel profileModel)
        {
            var apiResult = new ApiResult<bool>(selLangBLL);
            var authData = authorizeBLL.GetAuthData();
            apiResult.Data = custInfoBLL.ExistsEmail(authData.CustId, profileModel.Email);
            apiResult.ErrorCode = 0;
            return apiResult;
        }

        [HttpPost]
        public async Task<ApiResult<decimal>> GetBalanceAsync()
        {
            var apiResult = new ApiResult<decimal>(selLangBLL);
            var authData = authorizeBLL.GetAuthData();
            apiResult.Data = mainWalletType == 1 ? walletBLL.GetBalance(authData.CustId) : await Get3rdBalanceAsync(1, authData.CustId);
            apiResult.ErrorCode = 0;
            return apiResult;
        }

        private class UserBalance
        {
            public decimal balance { get; set; }
            public decimal outstanding { get; set; }
            public byte Currency { get; set; }
        }

        private class TpInfo
        {
            public int TpId { set; get; }
            public int CustId { set; get; }
            public Dictionary<string, string> inputs { set; get; }
        }

        /*3rd Wallter*/
        private async Task<decimal> Get3rdBalanceAsync(int tpId, int custId)
        {
            var myUrl = $"{apiUrl}GetBalance";
            var oTpInfo = new TpInfo
            {
                TpId = tpId,
                CustId = custId,
                inputs = new Dictionary<string, string>()
            };

            var data = await courierAdapter.MyPostAsync<UserBalance, TpInfo>(myUrl, oTpInfo);

            return data.ErrorCode == 0 ? data.Data.balance : 0;
        }

        [HttpPost]
        public ApiResult SetProfile(Dictionary<string, string> custProfile)
        {
            var apiResult = new ApiResult(selLangBLL);
            var authData = authorizeBLL.GetAuthData();

            apiResult.ErrorCode = custInfoBLL.SetProfile(authData.CustId, custProfile);
            return apiResult;
        }

        [HttpPost]
        public ApiResult<CustBankInfoRep> GetCustBankInfo()
        {
            var apiResult = new ApiResult<CustBankInfoRep>(selLangBLL);
            var authData = authorizeBLL.GetAuthData();
            var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);
            var bankInfoSysList = bankInfoBLL.GetBankInfoSysListByCurrency(custInfo.CurrencyId);

            var custBankInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<CustBankInfoRep>(Newtonsoft.Json.JsonConvert.SerializeObject(custInfoBLL.GetCustBankInfo(authData.CustId)));

            if (custBankInfo != null && custBankInfo.bankInfoDetail != null && custBankInfo.bankInfoDetail.Count() > 0)
            {
                foreach (var bankInfo in custBankInfo.bankInfoDetail)
                {
                    var refBankInfo = bankInfoSysList.Where(x => x.SysId == bankInfo.SysId).FirstOrDefault();
                    if (refBankInfo != null)
                    {
                        bankInfo.BankName = refBankInfo.BankName;
                        bankInfo.BankLogo = refBankInfo.BankLogo;
                        bankInfo.BankSite = refBankInfo.BankSite;
                    }
                }
            }

            apiResult.Data = custBankInfo;
            apiResult.ErrorCode = 0;

            return apiResult;
        }

        public class MsgReq
        {
            public int PageNumber { get; set; }
            //public int PageSize { get; set; }
        }

        [HttpPost]
        public ApiListResult<Msg> GetMsgList(MsgReq msgReq)
        {
            var authData = authorizeBLL.GetAuthData();
            var apiResult = new ApiListResult<Msg>();
            int pageNumber = msgReq.PageNumber;
            int totalPages;
            int rowCount;
            apiResult.Data = messageBLL.GetMsgList(authData.CustId, 5, ref pageNumber, out totalPages, out rowCount);
            apiResult.PageNumber = pageNumber;
            apiResult.TotalPages = totalPages;
            apiResult.RowCount = rowCount;
            apiResult.ErrorCode = 0;
            return apiResult;
        }

        public class MsgContentReq
        {
            public int MsgId { get; set; }
            //public int PageSize { get; set; }
        }

        [HttpPost]
        public ApiResult<MsgContent> GetMsgContent(MsgContentReq msgContentReq)
        {
            var authData = authorizeBLL.GetAuthData();
            var apiResult = new ApiResult<MsgContent>();
            apiResult.Data = messageBLL.GetMsgContent(authData.CustId, msgContentReq.MsgId);
            apiResult.ErrorCode = 0;
            return apiResult;
        }

        public class DelMsgReq
        {
            public int[] MsgIds { get; set; }
        }

        [HttpPost]
        public ApiResult DelMsg(DelMsgReq msgReq)
        {
            var authData = authorizeBLL.GetAuthData();
            var apiResult = new ApiResult<MsgContent>();
            if (messageBLL.DelMsg(authData.CustId, msgReq.MsgIds))
            {
                apiResult.ErrorCode = 0;
            }
            else
            {
                apiResult.ErrorCode = 99;
            }

            return apiResult;
        }
        //

        [HttpPost]
        public ApiResult GetMsgCount()
        {
            var authData = authorizeBLL.GetAuthData();
            var apiResult = new ApiResult<int>();
            apiResult.Data = messageBLL.GetMsgCount(authData.CustId);
            apiResult.ErrorCode = 0;

            return apiResult;
        }
        //[HttpPost]
        //public ApiResult DelCustBankInfo(BankInfo bankInfo)
        //{
        //    var apiResult = new ApiResult(selLangBLL);
        //    var authData = authorizeBLL.GetAuthData();
        //    IEnumerable<BankInfoDetail> bankInfoList = null;

        //    var custBankInfo = custInfoBLL.GetCustBankInfo(authData.CustId);
        //    if (custBankInfo == null || custBankInfo.BankInfo == null)
        //    {
        //        apiResult.ErrorCode = -2;
        //        return apiResult;
        //    }
        //    else if (custBankInfo != null && custBankInfo.bankInfoDetail != null)
        //    {
        //        bankInfoList = custBankInfo.bankInfoDetail.Where(x => !(x.SysId == bankInfo.SysId && x.AccountNo == bankInfo.AccountNo));
        //    }

        //    apiResult.ErrorCode = custInfoBLL.SetCustBankInfo(authData.CustId, custBankInfo.AccountName, bankInfoList);
        //    return apiResult;
        //}
    }
}