using Microsoft.AspNetCore.Mvc;

namespace MemberSite.Controllers.Api
{
    [Route("api/[controller]")]
    public class SpeedController:Controller
    {
        [HttpGet]
        [HttpHead]
        public IActionResult Index()
        {
            return View("~/Views/Speed/Speed.cshtml");
        }
    }
}