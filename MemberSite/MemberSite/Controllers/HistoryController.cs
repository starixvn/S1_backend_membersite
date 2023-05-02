using MemberSite.Service.BLL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MemberSite.Controllers
{
    [Authorize]
    public class HistoryController : Controller
    {
        private readonly TemplateBLL templateBLL;

        public HistoryController(TemplateBLL templateBLL)
        {
            this.templateBLL = templateBLL;
        }

        public IActionResult Deposit()
        {
            return View(templateBLL.GetTemplatePath(Url));
        }
        public IActionResult Withdrawal()
        {
            return View(templateBLL.GetTemplatePath(Url));
        }
        public IActionResult Transfer()
        {
            return View(templateBLL.GetTemplatePath(Url));
        }

        public IActionResult Statement()
        {
            return View(templateBLL.GetTemplatePath(Url));
        }
    }
}