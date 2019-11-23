using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace MemberSite.Areas.Mobile.Controllers
{
    [Area("Mobile")]
    [Route("[area]/[controller]")]
    public class HomeController : Controller
    {
        [HttpGet("[action]", Name = "Index")]
        public IActionResult Index()
        {
            return Content("Mobile");
        }
    }
}