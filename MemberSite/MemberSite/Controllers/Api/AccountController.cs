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

        public AccountController(CustInfoBLL custInfoBLL, Tools tools, SelLangBLL selLangBLL, AuthorizeBLL authorizeBLL, BankInfoBLL bankInfoBLL)
        {
            this.custInfoBLL = custInfoBLL;
            this.tools = tools;
            this.selLangBLL = selLangBLL;
            this.authorizeBLL = authorizeBLL;
            this.bankInfoBLL = bankInfoBLL;
        }
        [HttpPost]
        public async Task<ApiResult> LoginCheckin()
        {
            var apiResult = new ApiResult(selLangBLL);
            bool result = authorizeBLL.LoginCheckin();
            if (!result)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                apiResult.ErrorCode = 1001;
                apiResult.Message = selLangBLL.GetMsg("msg_loggedout");
            }
            else
            {
                apiResult.ErrorCode = 0;
            }

            return apiResult;
        }

        [HttpPost]
        public async Task<ApiResult> Logout()
        {
            var apiResult = new ApiResult(selLangBLL);
            bool result = authorizeBLL.Logout();

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            apiResult.ErrorCode = 0;
            apiResult.Message = "";

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
            if (languageModel.LangId != authData.LangId)
            {
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
                apiResult.Message = "";
            }
            else
            {
                apiResult.ErrorCode = 0;
                apiResult.Message = "";
            }

            return apiResult;
        }

        public class ChangePasswordModel
        {
            public string OldPassword { set; get; }
            public string NewPassword { set; get; }
        }

        [HttpPost]
        public ApiResult ChangePassword(ChangePasswordModel changePasswordModel)
        {
            string oldPassword = changePasswordModel.OldPassword;
            string newPassword = changePasswordModel.NewPassword;

            var apiResult = new ApiResult(selLangBLL);
            var authData = authorizeBLL.GetAuthData();

            if (oldPassword == newPassword)
            {
                apiResult.ErrorCode = -1;
                apiResult.Message = "";
            }
            else if (!tools.CheckUserInputPassword(newPassword))
            {
                apiResult.ErrorCode = -2;
                apiResult.Message = "";
            }
            else
            {
                apiResult.ErrorCode = custInfoBLL.SetPassword(authData.CustId, oldPassword, newPassword);

                if (apiResult.ErrorCode == -3)
                {
                    apiResult.Message = "";
                }
                else if (apiResult.ErrorCode == -4)
                {
                    apiResult.Message = "";
                }
                else
                {
                    apiResult.ErrorCode = 0;
                    apiResult.Message = "";
                }
            }

            return apiResult;
        }

        [HttpPost]
        public ApiResult SetProfile(Dictionary<string, string> custProfile)
        {
            var apiResult = new ApiResult(selLangBLL);
            var authData = authorizeBLL.GetAuthData();

            apiResult.ErrorCode = custInfoBLL.SetProfile(authData.CustId, custProfile);
            return apiResult;
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
            var bankInfoSysList = bankInfoBLL.GetBankInfoSysListByCurrency(custInfo.CurrencyId);

            if (bankInfoSysList == null || bankInfoSysList.Where(x => x.SysId == bankInfo.SysId).Count() == 0)
            {
                apiResult.ErrorCode = -1;
                return apiResult;
            }

            var custBankInfo = custInfoBLL.GetCustBankInfo(authData.CustId);
            if (custBankInfo == null && string.IsNullOrEmpty(bankInfo.AccountName))
            {
                apiResult.ErrorCode = -2;
                return apiResult;
            }
            else if (custBankInfo != null && custBankInfo.BankInfo != null && custBankInfo.bankInfoDetail.Count() >= 3)
            {
                apiResult.ErrorCode = -3;
                return apiResult;
            }
            else if (custBankInfo != null && custBankInfo.bankInfoDetail != null)
            {
                bankInfoList = custBankInfo.bankInfoDetail.ToList();

                if (bankInfoList.Where(x => x.SysId == bankInfo.SysId && x.AccountNo == bankInfo.AccountNo).Count() > 0)
                {
                    apiResult.ErrorCode = -4;
                    return apiResult;
                }
            }
            else
            {
                bankInfoList = new List<BankInfoDetail>();
            }

            bankInfoList.Add(new BankInfoDetail { AccountNo = bankInfo.AccountNo, SysId = bankInfo.SysId });

            apiResult.ErrorCode = custInfoBLL.SetCustBankInfo(authData.CustId, bankInfo.AccountName, bankInfoList);
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
                return apiResult;
            }
            else if (custBankInfo != null && custBankInfo.bankInfoDetail != null)
            {
                bankInfoList = custBankInfo.bankInfoDetail.Where(x => !(x.SysId == bankInfo.SysId && x.AccountNo == bankInfo.AccountNo));
            }

            apiResult.ErrorCode = custInfoBLL.SetCustBankInfo(authData.CustId, custBankInfo.AccountName, bankInfoList);
            return apiResult;
        }
    }
}