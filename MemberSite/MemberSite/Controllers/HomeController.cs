using MemberSite.Service.BLL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;

namespace MemberSite.Controllers
{
    public class HomeController : Controller
    {
        private readonly TemplateBLL templateBLL;
        private readonly AuthBLL authBLL;
        private readonly CustInfoBLL custInfoBLL;
        private readonly Tools tools;
        private readonly SelLangBLL selLangBLL;
        private readonly CommonBLL commonBLL;

        public HomeController(TemplateBLL templateBLL, AuthBLL authBLL, CustInfoBLL custInfoBLL, Tools tools, SelLangBLL selLangBLL, CommonBLL commonBLL)
        {
            this.templateBLL = templateBLL;
            this.authBLL = authBLL;
            this.custInfoBLL = custInfoBLL;
            this.tools = tools;
            this.selLangBLL = selLangBLL;
            this.commonBLL = commonBLL;
        }
        public IActionResult Index(string refId, string affId, string aff)
        {
            if (!User.Identity.IsAuthenticated)
            {
                // VN Affiliate
                if (aff != null)
                {
                    CookieOptions option = new CookieOptions();
                    option.Expires = DateTime.Now.AddMonths(1);
                    Response.Cookies.Append(nameof(aff), aff, option);

                    if (affId == null)
                    {
                        Response.Cookies.Delete(nameof(affId));
                    }
                    if (refId == null)
                    {
                        Response.Cookies.Delete(nameof(refId));
                    }
                }

                // Marketing Affiliate
                if (refId != null)
                {
                    CookieOptions option = new CookieOptions();
                    option.Expires = DateTime.Now.AddMonths(1);
                    Response.Cookies.Append(nameof(refId), refId, option);

                    if (affId == null)
                    {
                        Response.Cookies.Delete(nameof(affId));
                    }
                    if (aff == null)
                    {
                        Response.Cookies.Delete(nameof(aff));
                    }
                }

                // Affiliate
                if (affId != null)
                {
                    CookieOptions option = new CookieOptions();
                    option.Expires = DateTime.Now.AddMonths(1);
                    Response.Cookies.Append(nameof(affId), affId, option);

                    if (refId == null)
                    {
                        Response.Cookies.Delete(nameof(refId));
                    }
                    if (aff == null)
                    {
                        Response.Cookies.Delete(nameof(aff));
                    }
                }
            }

            return View(templateBLL.GetTemplatePath(Url));
        }

        public IActionResult Register(string refId, string affId, string aff)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToRoute(new { action = "Index", controller = "Home" });
            }

            // VN Affiliate
            if (aff != null)
            {
                CookieOptions option = new CookieOptions();
                option.Expires = DateTime.Now.AddMonths(1);
                Response.Cookies.Append(nameof(aff), aff, option);

                if (affId == null)
                {
                    Response.Cookies.Delete(nameof(affId));
                }
                if (refId == null)
                {
                    Response.Cookies.Delete(nameof(refId));
                }
            }

            // Marketing Affiliate
            if (refId != null)
            {
                CookieOptions option = new CookieOptions();
                option.Expires = DateTime.Now.AddMonths(1);
                Response.Cookies.Append(nameof(refId), refId, option);

                if (affId == null)
                {
                    Response.Cookies.Delete(nameof(affId));
                }
                if (aff == null)
                {
                    Response.Cookies.Delete(nameof(aff));
                }
            }

            //Affiliate
            if (affId != null)
            {
                CookieOptions option = new CookieOptions();
                option.Expires = DateTime.Now.AddMonths(1);
                Response.Cookies.Append(nameof(affId), affId, option);

                if (refId == null)
                {
                    Response.Cookies.Delete(nameof(refId));
                }
                if (aff == null)
                {
                    Response.Cookies.Delete(nameof(aff));
                }
            }

            return View(templateBLL.GetTemplatePath(Url));
        }

        public IActionResult ForgetPassowrd()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToRoute(new { action = "Index", controller = "Home" });
            }

            return View(templateBLL.GetTemplatePath(Url));
        }

        public async Task<IActionResult> ResetPasswordAsync(string key)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToRoute(new { action = "Index", controller = "Home" });
            }
            else if (authBLL.Authenticate(key, out CustInfoProfile custInfoProfile))
            {
                Customer customer = null;
                int userLoginResult = custInfoBLL.UserLoginForResetPwd(custInfoProfile.UserName, tools.GetClientIP(), tools.GetFromUrl(), Request.Headers["User-Agent"], ref customer, out string token);
                if (userLoginResult == 0)
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name,custInfoProfile.UserName ),
                        new Claim(ClaimTypes.NameIdentifier, customer.CustId.ToString()),
                        new Claim("Token",token ),
                        new Claim("LangId",custInfoProfile.UserLang )
                    };
                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));
                    return RedirectToRoute(new { action = "ResetPwd", controller = "Account" });
                }
                else if (userLoginResult == -3)
                {
                    return StatusCode(401);
                }
            }

            return StatusCode(403);
        }

        public IActionResult Static()
        {
            return View();
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
            var result = custInfoBLL.AddCustInfo(signupModel.UserName, signupModel.Userpwd, signupModel.Email, signupModel.CurrencyId, signupModel.UserLang, signupModel.RefId, signupModel.AffId, signupModel.Aff, out custId);
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