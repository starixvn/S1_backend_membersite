using MemberSite.Service.BLL;
using Microsoft.AspNetCore.Mvc;

namespace MemberSite.Areas.Mobile.Controllers
{
    [Area("Mobile")]
    [Route("[area]/[controller]/[action]/{LangId}")]
    //[MiddlewareFilter(typeof(LocalizationPipeline))]
    public class HomeController : Controller
    {
        private readonly SelLangBLL selLangBLL;

        public HomeController(SelLangBLL selLangBLL)
        {
            this.selLangBLL = selLangBLL;
        }
        [HttpGet]
        public IActionResult Index()
        {
            //RedirectToRoute("")
            return Content("Mobile"+ selLangBLL.GetLangId());
        }
    }
}