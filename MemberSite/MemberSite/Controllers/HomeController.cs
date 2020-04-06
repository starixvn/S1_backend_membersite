using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;

namespace MemberSite.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index(string refId, string affId)
        {
            if (!User.Identity.IsAuthenticated)
            {
                if (refId != null)
                {
                    CookieOptions option = new CookieOptions();
                    option.Expires = DateTime.Now.AddMonths(1);
                    Response.Cookies.Append(nameof(refId), refId, option);

                    if (affId == null)
                    {
                        Response.Cookies.Delete(nameof(affId));
                    }
                }

                if (affId != null)
                {
                    CookieOptions option = new CookieOptions();
                    option.Expires = DateTime.Now.AddMonths(1);
                    Response.Cookies.Append(nameof(affId), affId, option);
                }
            }

            return View();
        }
        public IActionResult Register(string refId, string affId)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToRoute(new { action = "Index", controller = "Home" });
            }

            if (refId != null)
            {
                CookieOptions option = new CookieOptions();
                option.Expires = DateTime.Now.AddMonths(1);
                Response.Cookies.Append(nameof(refId), refId, option);

                if (affId == null)
                {
                    Response.Cookies.Delete(nameof(affId));
                }
            }

            if (affId != null)
            {
                CookieOptions option = new CookieOptions();
                option.Expires = DateTime.Now.AddMonths(1);
                Response.Cookies.Append(nameof(affId), affId, option);
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