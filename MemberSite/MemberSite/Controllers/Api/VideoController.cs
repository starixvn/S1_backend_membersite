using MemberSite.Service.BLL;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
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
        private readonly VideoDataBll videoDataBLL;


        public VideoController(AuthorizeBLL authorizeBLL, ServerInfoBll serverInfoBLL, SelLangBLL selLangBLL, CustInfoBll custInfoBLL, LiveStreamingBll liveStreamingBLL, VideoDataBll videoDataBLL)
        {
            this.authorizeBLL = authorizeBLL;
            this.serverInfoBLL = serverInfoBLL;
            this.selLangBLL = selLangBLL;
            this.custInfoBLL = custInfoBLL;
            this.liveStreamingBLL = liveStreamingBLL;
            this.videoDataBLL = videoDataBLL;
        }
        [HttpPost]
        public ApiResult<IEnumerable<LiveStreamingData>> GetLiveStreamingList()
        {
            var apiResult = new ApiResult<IEnumerable<LiveStreamingData>>();

            var serverInfo = serverInfoBLL.GetServerInfo();
            int SiteId;
            if (User.Identity.IsAuthenticated)
            {
                var authData = authorizeBLL.GetAuthData();
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
        public ApiResult<VideoData> GetLatestHighlightData()
        {
            var apiResult = new ApiResult<VideoData>();

            var serverInfo = serverInfoBLL.GetServerInfo();
            int siteId;
            var currencyId = (int)CurrencyId.MYR;
            
            if (User.Identity.IsAuthenticated)
            {
                var authData = authorizeBLL.GetAuthData();
                var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);
                siteId = custInfo.SiteId;
                currencyId = custInfo.CurrencyId;
            }
            else
            {
                siteId = serverInfo.SiteId;
            }

            apiResult.ErrorCode = 0;
            apiResult.Data = videoDataBLL.GetVideoData(siteId,currencyId,selLangBLL.GetLangId());

            return apiResult;
        }

    }
}
