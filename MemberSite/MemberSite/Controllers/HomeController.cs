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

namespace MemberSite.Controllers
{
    public class HomeController : Controller
    {
        private readonly TemplateBLL templateBLL;
        private readonly AuthBLL authBLL;
        private readonly CustInfoBLL custInfoBLL;
        private readonly Tools tools;

        public HomeController(TemplateBLL templateBLL, AuthBLL authBLL, CustInfoBLL custInfoBLL, Tools tools)
        {
            this.templateBLL = templateBLL;
            this.authBLL = authBLL;
            this.custInfoBLL = custInfoBLL;
            this.tools = tools;
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
            return View();
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
                    return RedirectToRoute(new { action = "ChangePwd", controller = "Account" });
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
    }
}