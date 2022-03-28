using MemberSite.Service.BLL;
using MemberSite.Service.Model;
using Microsoft.AspNetCore.Mvc;

namespace MemberSite.Controllers.Api
{
    [Route("api/[controller]")]
    [Route("api/[controller]/[action]")]
    public class PlayerController : Controller
    {
        private readonly ServerInfoBll serverInfoBLL;
        private readonly AppLinkBLL appLinkBLL;
        public PlayerController(ServerInfoBll serverInfoBLL, AppLinkBLL appLinkBLL)
        {
            this.serverInfoBLL = serverInfoBLL;
            this.appLinkBLL = appLinkBLL;
        }
        [HttpGet]
        [HttpHead]
        public IActionResult Index(string url)
        {
            var poster = url.Replace("video.m3u8", "poster.jpg?v=1627386751522");

            ViewData["url"] = url;
            ViewData["poster"] = poster;
            return View();
        }
        [HttpPost]
        public ApiResult<List<GetAppLinkRep>> GetAppLink()
        {
            var apiResult = new ApiResult<List<GetAppLinkRep>>();
            var serverInfo = serverInfoBLL.GetServerInfo();
            var result = appLinkBLL.GetAppLink(serverInfo.SiteId);
            apiResult.Data = result;
            apiResult.ErrorCode = 0;
            return apiResult;
        }
    }
  
}
