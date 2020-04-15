using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MemberSite.Controllers
{
    [Authorize]
    public class MessageController : Controller
    {
        public IActionResult PersonalMsg()
        {
            return View();
        }

        public IActionResult Announcement()
        {
            return View();
        }
    }
}