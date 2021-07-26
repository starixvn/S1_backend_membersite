using Microsoft.AspNetCore.Mvc;
using MemberSite.Service.BLL;

namespace MemberSite.Controllers
{
    public class EventsController : Controller
    {
        private readonly SelLangBLL selLangBLL;
        public EventsController(SelLangBLL selLangBLL)
        {
            this.selLangBLL = selLangBLL;
        }

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Event(string eventName)
        {
            return View($"./{eventName}/{selLangBLL.GetLangId()}");
        }
    }
}