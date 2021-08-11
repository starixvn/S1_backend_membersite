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