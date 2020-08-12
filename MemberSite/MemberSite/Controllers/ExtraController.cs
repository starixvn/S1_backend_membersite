using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using MemberSite.Service.BLL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace MemberSite.Controllers
{
    public class ExtraController : Controller
    {
        private readonly CustInfoBLL custInfoBLL;
        private readonly SelLangBLL selLangBLL;
        private readonly CommonBLL commonBLL;
        private readonly Tools tools;

        public ExtraController(CustInfoBLL custInfoBLL, SelLangBLL selLangBLL, CommonBLL commonBLL, Tools tools)
        {
            this.custInfoBLL = custInfoBLL;
            this.selLangBLL = selLangBLL;
            this.commonBLL = commonBLL;
            this.tools = tools;
        }
        public class SignupModel : Api.SiteController.RegisteredModel
        {
            public string SignupUrl { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> SignupAsync([FromForm]SignupModel signupModel)
        {
            string message = "";
            int errorCode = 99;

            if (User.Identity.IsAuthenticated)
            {
                return RedirectToRoute(new { action = "Index", controller = "Home" });
            }

            int custId = 0;
            var result = custInfoBLL.AddCustInfo(signupModel.UserName, signupModel.Userpwd, signupModel.Email, signupModel.CurrencyId, signupModel.UserLang, signupModel.RefId, signupModel.AffId, signupModel.Aff, out custId, signupModel.SignupUrl);
            if (result == -1)
            {
                errorCode = result;
                message = selLangBLL.GetMsg("err_ThisCurrencyNotSupport");
            }
            else if (result == -2)
            {
                errorCode = result;
                message = selLangBLL.GetMsg("msg_UsernameReq");
            }
            else if (result == -3)
            {
                errorCode = result;
                message = selLangBLL.GetMsg("msg_PasswordReq");
            }
            else if (result == -4)
            {
                errorCode = result;/**/
                message = "";
            }
            else if (result == -5)
            {
                errorCode = result;
                message = selLangBLL.GetMsg("msg_UsernameNotAvailable");
            }
            else if (result == -19)
            {
                errorCode = result;
                message = selLangBLL.GetMsg("mag_AffiliateNotExist");
            }
            else if (custId == 0)
            {
                errorCode = result;
                message = selLangBLL.GetMsg("msg_UsernameNotAvailable");
            }
            else if (result != 0)
            {
                errorCode = result;
                message = selLangBLL.GetMsg("msg_UsernameNotAvailable");
            }
            else
            {
                commonBLL.SendWelcomeMail(custId);

                Customer customer = null;
                string token = null;
                var userLoginResult = custInfoBLL.UserLogin(signupModel.UserName, signupModel.Userpwd, tools.GetClientIP(), tools.GetFromUrl(), Request.Headers["User-Agent"], ref customer, out token);

                if (userLoginResult == -1)
                {
                    errorCode = 1101;
                    message = selLangBLL.GetMsg("msg_DepositProcessLogin_LoginFailed");
                }
                else if (userLoginResult == -2)
                {
                    errorCode = 1001;
                    message = selLangBLL.GetMsg("msg_DepositProcessLogin_LoginFailed");
                }
                else if (userLoginResult == -3)
                {
                    errorCode = 1102;
                    message = selLangBLL.GetMsg("msg_DepositProcessLogin_ACLocked");
                }
                else if (userLoginResult != 0)
                {
                    errorCode = 9000;
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
                    errorCode = 0;
                    return RedirectToRoute(new { action = "Index", controller = "Home" });
                }
            }

            return Redirect($"{signupModel.SignupUrl}{(signupModel.SignupUrl.IndexOf("?") > -1 ? $"&message={HttpUtility.UrlEncode(message)}&errorCode={errorCode}" : $"?message={HttpUtility.UrlEncode(message)}&errorCode={errorCode}")}");
        }

    }
}