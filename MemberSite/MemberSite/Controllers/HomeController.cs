using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MemberSite.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index(string refId, string affId, string aff)
        {
            if (!User.Identity.IsAuthenticated)
            {
                if (aff != null)
                {
                    CookieOptions option = new CookieOptions();
                    option.Expires = DateTime.Now.AddMonths(1);
                    Response.Cookies.Append(nameof(aff), aff, option);
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
                    if (aff == null)
                    {
                        Response.Cookies.Delete(nameof(aff));
                    }
                }

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

            return View();
        }

        public IActionResult Register(string refId, string affId, string aff)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToRoute(new { action = "Index", controller = "Home" });
            }

            if (aff != null)
            {
                CookieOptions option = new CookieOptions();
                option.Expires = DateTime.Now.AddMonths(1);
                Response.Cookies.Append(nameof(aff), aff, option);
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
                if (aff == null)
                {
                    Response.Cookies.Delete(nameof(aff));
                }
            }

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