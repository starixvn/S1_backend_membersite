using MemberSite.Service.BLL;
using Microsoft.AspNetCore.Mvc;

namespace MemberSite.Controllers
{
    public class CasinoController : Controller
    {
        private readonly TemplateBLL templateBLL;

        public CasinoController(TemplateBLL templateBLL)
        {
            this.templateBLL = templateBLL;
        }
        public IActionResult Index()
        {
            return View(templateBLL.GetTemplatePath(Url));
        }

        public IActionResult Lobby()
        {
            return View(templateBLL.GetTemplatePath(Url));
        }
    }
}