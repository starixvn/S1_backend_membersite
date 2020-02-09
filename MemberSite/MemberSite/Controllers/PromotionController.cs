using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MemberSite.Service.BLL;
using Microsoft.AspNetCore.Mvc;

namespace MemberSite.Controllers
{
    public class PromotionController : Controller
    {
        private readonly SelLangBLL selLangBLL;

        public PromotionController(SelLangBLL selLangBLL)
        {
            this.selLangBLL = selLangBLL;
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Bonus(string BonusCode)
        {
            return View($"./{BonusCode}/{selLangBLL.GetLangId()}");
        }
    }
}