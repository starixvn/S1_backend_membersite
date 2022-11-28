using MemberSite.Service.BLL;
using MemberSite.Service.Model;
using Microsoft.AspNetCore.Mvc;

namespace MemberSite.Controllers.Api
{
    [Route("api/[controller]/[action]/{LangId}")]
    [ApiController]
    public class VideoController : Controller
    {
        private readonly AuthorizeBLL authorizeBLL;
        private readonly ServerInfoBll serverInfoBLL;
        private readonly SelLangBLL selLangBLL;
        private readonly CustInfoBll custInfoBLL;
        private readonly LiveStreamingBll liveStreamingBLL;
        private readonly LatestHighlightDataBll latestHighlightDataBLL;


        public VideoController(AuthorizeBLL authorizeBLL, ServerInfoBll serverInfoBLL, SelLangBLL selLangBLL, CustInfoBll custInfoBLL, LiveStreamingBll liveStreamingBLL, LatestHighlightDataBll latestHighlightDataBLL)
        {
            this.authorizeBLL = authorizeBLL;
            this.serverInfoBLL = serverInfoBLL;
            this.selLangBLL = selLangBLL;
            this.custInfoBLL = custInfoBLL;
            this.liveStreamingBLL = liveStreamingBLL;
            this.latestHighlightDataBLL = latestHighlightDataBLL;
        }
        [HttpPost]
        public ApiResult<IEnumerable<LiveStreamingData>> GetLiveStreamingList()
        {
            var apiResult = new ApiResult<IEnumerable<LiveStreamingData>>();

            var serverInfo = serverInfoBLL.GetServerInfo();
            int SiteId;
            var authData = authorizeBLL.GetAuthData();
            if (authData is { CustId: > 0 })
            {
                var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);
                SiteId = custInfo.SiteId;
            }
            else
            {
                SiteId = serverInfo.SiteId;
            }

            apiResult.ErrorCode = 0;
            apiResult.Data = liveStreamingBLL.GetLiveStreamingList(SiteId, selLangBLL.GetLangId());

            return apiResult;
        }
        [HttpPost]
        public ApiResult<LatestHighlightData> GetLatestHighlightData()
        {
            var apiResult = new ApiResult<LatestHighlightData>();

            var serverInfo = serverInfoBLL.GetServerInfo();
            int SiteId;
            var authData = authorizeBLL.GetAuthData();
            if (authData is { CustId: > 0 })
            {
                var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);
                SiteId = custInfo.SiteId;
            }
            else
            {
                SiteId = serverInfo.SiteId;
            }

            apiResult.ErrorCode = 0;
            apiResult.Data = latestHighlightDataBLL.GetLatestHighlightData(SiteId, selLangBLL.GetLangId());

            return apiResult;
        }

    }
}
