using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MemberSite.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        public IActionResult Profile()
        {
            return View();
        }
        public IActionResult ChangePwd()
        {
            return View();
        }
        public IActionResult CustBankInfo()
        {
            return View();
        }
        public IActionResult Messages()
        {
            return View();
        }
    }
}