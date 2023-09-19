using System.Security.Claims;

using MemberSite.Service.BLL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace MemberSite.Controllers
{
    public class HomeController : Controller
    {
        private readonly AuthBLL authBLL;
        private readonly CustInfoBll custInfoBLL;
        private readonly Tools tools;

        public HomeController(AuthBLL authBLL, CustInfoBll custInfoBLL, Tools tools)
        {
            this.authBLL = authBLL;
            this.custInfoBLL = custInfoBLL;
            this.tools = tools;
        }
        public IActionResult Index(string refId, string affId, string aff)
        {
            if (User.Identity?.IsAuthenticated == false)
            {
                // VN Affiliate
                if (aff != null)
                {
                    var option = new CookieOptions
                    {
                        Expires = DateTime.Now.AddMonths(1)
                    };
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
                    var option = new CookieOptions
                    {
                        Expires = DateTime.Now.AddMonths(1)
                    };
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
                    var option = new CookieOptions
                    {
                        Expires = DateTime.Now.AddMonths(1)
                    };
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

            return Ok();
        }

        public IActionResult Register(string refId, string affId, string aff)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return Ok();
            }

            // VN Affiliate
            if (aff != null)
            {
                var option = new CookieOptions
                {
                    Expires = DateTime.Now.AddMonths(1)
                };
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
                var option = new CookieOptions
                {
                    Expires = DateTime.Now.AddMonths(1)
                };
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
                var option = new CookieOptions
                {
                    Expires = DateTime.Now.AddMonths(1)
                };
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

            return Ok();
        }

        public async Task<IActionResult> ResetPasswordAsync(string key)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToRoute(new { action = "Index", controller = "Home" });
            }
            else if (authBLL.Authenticate(key, out CustInfoProfile custInfoProfile))
            {
                Customer customer = null;
                var fromUrl = tools.GetFromUrl();
                var deviceType = (int)tools.GetDeviceId(fromUrl);
                var userLoginResult = custInfoBLL.UserLoginForResetPwd(custInfoProfile.UserName, tools.GetClientIP(), fromUrl, Request.Headers["User-Agent"], deviceType, ref customer, out var token);
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
    }
}