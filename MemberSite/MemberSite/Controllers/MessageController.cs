using MemberSite.Service.BLL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MemberSite.Controllers
{
    [Authorize]
    public class MessageController : Controller
    {
        private readonly TemplateBLL templateBLL;

        public MessageController(TemplateBLL templateBLL)
        {
            this.templateBLL = templateBLL;
        }

        public IActionResult PersonalMsg()
        {
            return View(templateBLL.GetTemplatePath(Url));
        }

        public IActionResult Announcement()
        {
            return View(templateBLL.GetTemplatePath(Url));
        }
    }
}