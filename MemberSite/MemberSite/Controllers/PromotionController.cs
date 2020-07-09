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
        private TemplateBLL templateBLL;

        public PromotionController(SelLangBLL selLangBLL, TemplateBLL templateBLL)
        {
            this.selLangBLL = selLangBLL;
            this.templateBLL = templateBLL;
        }
        public IActionResult Index()
        {
            return View(templateBLL.GetTemplatePath(Url));
        }

        [HttpGet]
        public IActionResult Bonus(string BonusCode)
        {
            return View($"./{BonusCode}/{selLangBLL.GetLangId()}");
        }
    }
}