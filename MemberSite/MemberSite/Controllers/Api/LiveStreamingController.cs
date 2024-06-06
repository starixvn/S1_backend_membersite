using System.Net;

using MemberSite.Service.BLL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using static MemberSite.Controllers.Api.SiteController;

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
        private readonly CheckAttackBLL _checkAttackBll;
        private readonly ICacheHelper _cacheHelper;

        public LiveStreamingController(IServiceProvider provider)
        {
            _serverInfoBll = provider.GetRequiredService<ServerInfoBll>();
            _authorizeBll = provider.GetRequiredService<AuthorizeBLL>();
            _custInfoBll = provider.GetRequiredService<CustInfoBll>();
            _selLangBll = provider.GetRequiredService<SelLangBLL>();
            _liveStreamingBll = provider.GetRequiredService<LiveStreamingBll>();
            _logger = provider.GetRequiredService<ILogger<HotEventController>>();
            _checkAttackBll = provider.GetRequiredService<CheckAttackBLL>();
            _cacheHelper = provider.GetRequiredService<ICacheHelper>();
        }

        public class GetDataListReq
        {
            public int CurrencyId { get; set; }
        }
        [HttpPost]
        public async Task<ApiResult<IEnumerable<SportLiveStreamingResponse>>> GetDataList(GetDataListReq req, [FromRoute] string langId = null)
        {
            var apiResult = new ApiResult<IEnumerable<SportLiveStreamingResponse>>();
            //if (await _checkAttackBll.CheckAttackIp(Request))
            //{
            //    Request.HttpContext.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            //    return null;
            //}

            try
            {
                var serverInfo = _serverInfoBll.GetServerInfo();
                var authData = _authorizeBll.GetAuthData();
                var isLogin = authData is { CustId: > 0 };
                var siteId = 0;
                var currencyId = req.CurrencyId;

                if (isLogin)
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
                // 未登入檢查是否有Cache
                var key = $"LiveStreaming:GetDataList:{siteId}:{currencyId}:{langId}";
                if (!isLogin && _cacheHelper.TryGetValue(key, out IEnumerable<SportLiveStreamingResponse> liveStreamingList))
                {
                    apiResult.Data = liveStreamingList;
                    apiResult.ErrorCode = (int)ErrorCode.Success;
                    return apiResult;
                }

                apiResult.ErrorCode = (int)ErrorCode.Success;
                apiResult.Data = await _liveStreamingBll.GetSportLiveStreaming(siteId, currencyId, langId, serverInfo);

                // 新增未登入快取
                if (!isLogin && apiResult.Data != null)
                {
                    _cacheHelper.Create(key, apiResult.Data, TimeSpan.FromMinutes(10));
                }
            }
            catch (Exception ex)
            {
                apiResult.Message = ex.Message;
            }

            return apiResult;
        }
    }
}
