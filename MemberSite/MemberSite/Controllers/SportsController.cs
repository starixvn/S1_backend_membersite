using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MemberSite.Service.BLL;
using Microsoft.AspNetCore.Mvc;

namespace MemberSite.Controllers
{
    public class SportsController : Controller
    {
        private readonly TemplateBLL templateBLL;

        public SportsController(TemplateBLL templateBLL)
        {
            this.templateBLL = templateBLL;
        }
        public IActionResult Index()
        {
            return View(templateBLL.GetTemplatePath(Url));
        }
    }
}