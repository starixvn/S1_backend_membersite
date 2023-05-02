using MemberSite.Service.BLL;
using Microsoft.AspNetCore.Mvc;

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