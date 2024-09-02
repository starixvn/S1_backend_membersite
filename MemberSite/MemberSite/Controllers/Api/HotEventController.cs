using System.Net;
using MemberSite.Service.BLL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using static MemberSite.Extensions.MethodExtension;

namespace MemberSite.Controllers.Api
{
    [AllowAnonymous]
    [Route("api/[controller]/[action]")]
    [Route("api/[controller]/[action]/{LangId}")]
    [ApiController]
    public class HotEventController : ControllerBase
    {
        private readonly ServerInfoBll _serverInfoBll;
        private readonly AuthorizeBLL _authorizeBll;
        private readonly CustInfoBll _custInfoBll;
        private readonly SelLangBLL _selLangBll;
        private readonly HotEventBLL _hotEventBll;
        private readonly CheckAttackBLL _checkAttackBll;
        private readonly ILogger<HotEventController> _logger;
        private readonly ICacheHelper _cacheHelper;

        public HotEventController(IServiceProvider provider)
        {
            _serverInfoBll = provider.GetRequiredService<ServerInfoBll>();
            _authorizeBll = provider.GetRequiredService<AuthorizeBLL>();
            _custInfoBll = provider.GetRequiredService<CustInfoBll>();
            _selLangBll = provider.GetRequiredService<SelLangBLL>();
            _hotEventBll = provider.GetRequiredService<HotEventBLL>();
            _checkAttackBll = provider.GetRequiredService<CheckAttackBLL>();
            _logger = provider.GetRequiredService<ILogger<HotEventController>>();
            _cacheHelper = provider.GetRequiredService<ICacheHelper>();
        }

        [HttpPost]
        public async Task<ApiResult<IEnumerable<HotEventResponse>>> GetDataList([FromRoute] string langId = null)
        {
            var apiResult = new ApiResult<IEnumerable<HotEventResponse>>();

            if (await _checkAttackBll.CheckAttackIp(Request, 100))
            {
                Request.HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                return new ApiResult<IEnumerable<HotEventResponse>>()
                {
                    ErrorCode = (int)ErrorCode.Attack,
                    Data = new List<HotEventResponse>(),
                    Message = "GetHotEvent request exceeds the limit count."
                };
            }
            
            try
            {
                var serverInfo = _serverInfoBll.GetServerInfo();
                var authData = _authorizeBll.GetAuthData();
                var siteId = 0;
                var isLogin = authData is { CustId: > 0 };
                if (isLogin)
                {
                    var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
                    siteId = custInfo.SiteId;
                    langId = _selLangBll.GetLangId();
                }
                else
                {
                    siteId = serverInfo.SiteId;
                    langId = _selLangBll.CheckLangId(langId) ? langId : _selLangBll.GetLangId();
                }

                // 未登入檢查是否有Cache
                var key = $"HotEvent:GetDataList:{siteId}:{langId}";
                if (!isLogin && _cacheHelper.TryGetValue(key, out IEnumerable<HotEventResponse> hotEventList))
                {
                    apiResult.Data = hotEventList;
                    apiResult.ErrorCode = (int)ErrorCode.Success;
                    return apiResult;
                }

                apiResult.ErrorCode = (int)ErrorCode.Success;
                apiResult.Data = await _hotEventBll.GetHotEventDataList(siteId, langId, serverInfo);

                // 新增未登入快取
                if (!isLogin && apiResult.Data != null)
                {
                    _cacheHelper.Create(key, apiResult.Data, TimeSpan.FromMinutes(10));
                }
            }
            catch (Exception ex)
            {
                apiResult.Message = ex.Message;
                _logger.LogError(ex, GetCurrentAsyncMethod());
            }

            return apiResult;
        }
    }
}
