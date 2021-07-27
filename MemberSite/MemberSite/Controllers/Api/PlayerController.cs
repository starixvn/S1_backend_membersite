using Microsoft.AspNetCore.Mvc;

namespace MemberSite.Controllers.Api
{
    [Route("api/[controller]")]
    public class PlayerController : Controller
    {
        [HttpGet]
        [HttpHead]
        public IActionResult Index(string url)
        {
            string poster = url.Replace("video.m3u8", "poster.jpg?v=1627386751522");

            ViewData["url"] = url;
            ViewData["poster"] = poster;
            return View();
        }
    }
}
