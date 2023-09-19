using MemberSite.Service.BLL;
using MemberSite.Service.Model;

using Microsoft.AspNetCore.Mvc;

namespace MemberSite.Controllers.Api
{
    [Route("api/[controller]")]
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class PlayerController : Controller
    {
        private readonly ServerInfoBll serverInfoBLL;
        private readonly AppLinkBLL appLinkBLL;
        private readonly DeepLinkBLL deepLinkBLL;
        public PlayerController(ServerInfoBll serverInfoBLL, AppLinkBLL appLinkBLL, DeepLinkBLL deepLinkBLL)
        {
            this.serverInfoBLL = serverInfoBLL;
            this.appLinkBLL = appLinkBLL;
            this.deepLinkBLL = deepLinkBLL;
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

        public class GetDeepLinkReq
        {
            public int SiteId { get; set; }
            public int DeepLinkId { get; set; }
        }

        [HttpPost]
        public ApiResult<List<GetDeepLinkRep>> GetDeepLink(GetDeepLinkReq req)
        {
            var apiResult = new ApiResult<List<GetDeepLinkRep>>();
            var result = deepLinkBLL.GetDeepLink(req.SiteId, req.DeepLinkId);
            apiResult.Data = result;
            apiResult.ErrorCode = 0;
            return apiResult;
        }
    }

}
