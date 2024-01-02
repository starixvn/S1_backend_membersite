using MemberSite.Service.BLL;
using MemberSite.Service.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MemberSite.Controllers.Api
{
    [AllowAnonymous]
    [Route("api/[controller]/[action]")]
    [Route("api/[controller]/[action]/{LangId}")]
    [ApiController]
    public class LiveStreamingController : ControllerBase
    {
        private readonly ServerInfoBll _serverInfoBll;
        private readonly AuthorizeBLL _authorizeBll;
        private readonly CustInfoBll _custInfoBll;
        private readonly SelLangBLL _selLangBll;
        private readonly LiveStreamingBll _liveStreamingBll;
        private readonly ILogger<HotEventController> _logger;

        public LiveStreamingController(IServiceProvider provider)
        {
            _serverInfoBll = provider.GetRequiredService<ServerInfoBll>();
            _authorizeBll = provider.GetRequiredService<AuthorizeBLL>();
            _custInfoBll = provider.GetRequiredService<CustInfoBll>();
            _selLangBll = provider.GetRequiredService<SelLangBLL>();
            _liveStreamingBll = provider.GetRequiredService<LiveStreamingBll>();
            _logger = provider.GetRequiredService<ILogger<HotEventController>>();
        }

        public class GetDataListReq
        {
            public int CurrencyId { get; set; }
        }
        [HttpPost]
        public async Task<ApiResult<IEnumerable<SportLiveStreamingResponse>>> GetDataList(GetDataListReq req, [FromRoute] string langId = null)
        {
            var apiResult = new ApiResult<IEnumerable<SportLiveStreamingResponse>>();

            try
            {
                var serverInfo = _serverInfoBll.GetServerInfo();
                var authData = _authorizeBll.GetAuthData();
                var siteId = 0;
                var currencyId = req.CurrencyId;
                if (authData is { CustId: > 0 })
                {
                    var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
                    siteId = custInfo.SiteId;
                    langId = _selLangBll.GetLangId();
                    currencyId = custInfo.CurrencyId;
                }
                else
                {
                    siteId = serverInfo.SiteId;
                    langId = _selLangBll.CheckLangId(langId) ? langId : _selLangBll.GetLangId();
                }

                apiResult.ErrorCode = 0;
                apiResult.Data = await _liveStreamingBll.GetSportLiveStreaming(siteId, currencyId, langId, serverInfo);
            }
            catch (Exception ex)
            {
                apiResult.Message = ex.Message;
            }

            return apiResult;
        }
    }
}
