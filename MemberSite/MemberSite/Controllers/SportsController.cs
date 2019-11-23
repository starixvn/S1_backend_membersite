using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace MemberSite.Controllers
{
    public class SportsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}