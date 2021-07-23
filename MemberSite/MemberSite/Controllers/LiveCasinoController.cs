using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MemberSite.Service.BLL;
using MemberSite.Service.Library;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace MemberSite.Controllers
{
    public class LiveCasinoController : Controller
    {
        private readonly TemplateBLL templateBLL;

        public LiveCasinoController(TemplateBLL templateBLL)
        {
            this.templateBLL = templateBLL;
        }

        public IActionResult Index()
        {
            return View(templateBLL.GetTemplatePath(Url));
        }
    }
}