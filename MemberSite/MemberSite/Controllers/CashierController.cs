using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace MemberSite.Controllers
{
    public class CashierController : Controller
    {
        public IActionResult Deposit()
        {
            return View();
        }
    }
}