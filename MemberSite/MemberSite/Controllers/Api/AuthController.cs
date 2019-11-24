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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MemberSite.Controllers.Api
{
    [Route("api/[controller]/[action]")]
    //[Route("api/[controller]/[action]/{LangId}")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly CustInfoBLL custInfoBLL;
        private readonly ServerInfoBLL serverInfoBLL;
        private readonly Tools tools;
        private readonly SelLangBLL selLangBLL;

        public AuthController(CustInfoBLL custInfoBLL, ServerInfoBLL serverInfoBLL, SelLangBLL selLangBLL, Tools tools)
        {
            this.custInfoBLL = custInfoBLL;
            this.serverInfoBLL = serverInfoBLL;
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
        public async Task<ApiResult> Validation([FromBody]LoginModel loginModel)
        {
            var apiResult = new ApiResult(selLangBLL);
            var serverInfo = serverInfoBLL.GetServerInfo();
            string userName = loginModel.UserName;
            string userpwd = loginModel.Userpwd;
            Customer customer = null;
            string token = null;
            var userLoginResult = custInfoBLL.UserLogin(userName, serverInfo.SiteId, userpwd, tools.GetClientIP(this.HttpContext), tools.GetFromUrl(this.HttpContext), Request.Headers["User-Agent"], ref customer, out token);

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
            apiResult.ErrorCode = 0;
            return apiResult;
        }
    }
}