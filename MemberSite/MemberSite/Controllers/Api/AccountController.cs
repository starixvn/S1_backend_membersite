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

        public AccountController(CustInfoBLL custInfoBLL, Tools tools, SelLangBLL selLangBLL, AuthorizeBLL authorizeBLL)
        {
            this.custInfoBLL = custInfoBLL;
            this.tools = tools;
            this.selLangBLL = selLangBLL;
            this.authorizeBLL = authorizeBLL;
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
    }
}