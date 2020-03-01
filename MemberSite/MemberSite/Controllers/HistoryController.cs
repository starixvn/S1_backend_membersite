using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MemberSite.Controllers
{
    [Authorize]
    public class HistoryController : Controller
    {
        public IActionResult Deposit()
        {
            return View();
        }
        public IActionResult Withdrawal()
        {
            return View();
        }
        public IActionResult Transfer()
        {
            return View();
        }
    }
}