using MemberSite.Service.BLL;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;

namespace MemberSite.Controllers
{
    public class HomeController : Controller
    {
        private readonly TemplateBLL templateBLL;

        public HomeController(TemplateBLL templateBLL)
        {
            this.templateBLL = templateBLL;
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

            return View();
        }

        public IActionResult ForgetPassowrd()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToRoute(new { action = "Index", controller = "Home" });
            }
            return View();
        }

        public IActionResult Static()
        {
            return View();
        }
    }
}