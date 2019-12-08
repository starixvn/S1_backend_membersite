using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using MemberSite.Service.BLL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MemberSite.Controllers.Api
{
    [AllowAnonymous]
    [Route("api/[controller]/[action]")]
    //[Route("api/[controller]/[action]/{LangId}")]
    [ApiController]
    public class SiteController : ControllerBase
    {
        private readonly CustInfoBLL custInfoBLL;
        private readonly Tools tools;
        private readonly SelLangBLL selLangBLL;

        public SiteController(CustInfoBLL custInfoBLL, SelLangBLL selLangBLL, Tools tools)
        {
            this.custInfoBLL = custInfoBLL;
            this.tools = tools;
            this.selLangBLL = selLangBLL;
        }

        public class LoginModel
        {
            public string UserName { get; set; }
            public string Userpwd { get; set; }
        }

        [HttpGet]
        public string test()
        {
            return selLangBLL.GetLangId();
        }

        [HttpPost]
        public async Task<ApiResult> SignIn(LoginModel loginModel)
        {
            var apiResult = new ApiResult(selLangBLL);
            string userName = loginModel.UserName;
            string userpwd = loginModel.Userpwd;
            Customer customer = null;
            string token = null;
            var userLoginResult = custInfoBLL.UserLogin(userName, userpwd, tools.GetClientIP(HttpContext), tools.GetFromUrl(HttpContext), Request.Headers["User-Agent"], ref customer, out token);

            if (userLoginResult == -1)
            {
                apiResult.ErrorCode = 1101;
                apiResult.Message = selLangBLL.GetMsg("msg_DepositProcessLogin_LoginFailed");
            }
            else if (userLoginResult == -2)
            {
                apiResult.ErrorCode = 1001;
                apiResult.Message = selLangBLL.GetMsg("msg_DepositProcessLogin_LoginFailed");
            }
            else if (userLoginResult == -3)
            {
                apiResult.ErrorCode = 1102;
                apiResult.Message = selLangBLL.GetMsg("msg_DepositProcessLogin_ACLocked");
            }
            else if (userLoginResult != 0)
            {
                apiResult.ErrorCode = 9000;
            }

            if (userLoginResult != 0)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return apiResult;
            }

            var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name,customer.UserName ),
                    new Claim(ClaimTypes.NameIdentifier,customer.CustId.ToString() ),
                    new Claim("Token",token ),
                    new Claim("LangId",customer.UserLang )
                };
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));
            apiResult.Message = Url.RouteUrl("desktop", new { action = "Index", controller = "Home", LangId = selLangBLL.GetLangId() });
            apiResult.ErrorCode = 0;
            return apiResult;
        }
        public class RegisteredModel
        {
            public string UserName { get; set; }
            public string Userpwd { get; set; }
            public int CurrencyId { get; set; }
            public string UserLang { get; set; }
            public string RefId { get; set; }
        }

        [HttpPost]
        public async Task<ApiResult> RegisterAsync([FromBody]RegisteredModel registeredModel)
        {
            var apiResult = new ApiResult(selLangBLL);
            if (User.Identity.IsAuthenticated)
            {
                apiResult.ErrorCode = 9000;
                return apiResult;
            }

            int custId = 0;
            var result = custInfoBLL.AddCustInfo(registeredModel.UserName, registeredModel.Userpwd, registeredModel.CurrencyId, registeredModel.UserLang, registeredModel.RefId, out custId);
            if (result == -1)
            {
                apiResult.ErrorCode = result;
                apiResult.Message = selLangBLL.GetMsg("err_ThisCurrencyNotSupport");
            }
            else if (result == -2)
            {
                apiResult.ErrorCode = result;
                apiResult.Message = selLangBLL.GetMsg("msg_UsernameReq");
            }
            else if (result == -3)
            {
                apiResult.ErrorCode = result;
                apiResult.Message = selLangBLL.GetMsg("msg_PasswordReq");
            }
            else if (result == -4)
            {
                apiResult.ErrorCode = result;/**/
                apiResult.Message = "";
            }
            else if (result == -5)
            {
                apiResult.ErrorCode = result;
                apiResult.Message = selLangBLL.GetMsg("msg_UsernameNotAvailable");
            }
            else if (custId == 0)
            {
                apiResult.ErrorCode = result;
                apiResult.Message = selLangBLL.GetMsg("msg_UsernameNotAvailable");
            }
            else if (result != 0)
            {
                apiResult.ErrorCode = result;
                apiResult.Message = selLangBLL.GetMsg("msg_UsernameNotAvailable");
            }
            else
            {
                Customer customer = null;
                string token = null;
                var userLoginResult = custInfoBLL.UserLogin(registeredModel.UserName, registeredModel.Userpwd, tools.GetClientIP(HttpContext), tools.GetFromUrl(HttpContext), Request.Headers["User-Agent"], ref customer, out token);

                if (userLoginResult == -1)
                {
                    apiResult.ErrorCode = 1101;
                    apiResult.Message = selLangBLL.GetMsg("msg_DepositProcessLogin_LoginFailed");
                }
                else if (userLoginResult == -2)
                {
                    apiResult.ErrorCode = 1001;
                    apiResult.Message = selLangBLL.GetMsg("msg_DepositProcessLogin_LoginFailed");
                }
                else if (userLoginResult == -3)
                {
                    apiResult.ErrorCode = 1102;
                    apiResult.Message = selLangBLL.GetMsg("msg_DepositProcessLogin_ACLocked");
                }
                else if (userLoginResult != 0)
                {
                    apiResult.ErrorCode = 9000;
                }
                else
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name,customer.UserName ),
                        new Claim(ClaimTypes.NameIdentifier,customer.CustId.ToString() ),
                        new Claim("Token",token ),
                        new Claim("LangId",customer.UserLang )
                    };
                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));
                    apiResult.ErrorCode = 0;
                    apiResult.Message = Url.RouteUrl("desktop", new { action = "Index", controller = "Home", LangId = selLangBLL.GetLangId() });
                }
            }

            return apiResult;
        }

        [HttpPost]
        public ApiResult<IEnumerable<SiteLanguage>> GetSiteLanguage()
        {
            var apiResult = new ApiResult<IEnumerable<SiteLanguage>>(selLangBLL);
            apiResult.Data = selLangBLL.GetLangIdList();
            apiResult.ErrorCode = 0;
            return apiResult;
        }
        [HttpPost]
        public ApiResult<bool> ExistsUserName(RegisteredModel registeredModel)
        {
            var apiResult = new ApiResult<bool>(selLangBLL);
            apiResult.Data = custInfoBLL.ExistsUserName(registeredModel.UserName);
            apiResult.ErrorCode = 0;
            return apiResult;
        }
    }
}