using System.Net;
using MemberSite.Service.BLL;
using MemberSite.Service.Model;

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

        public HotEventController(IServiceProvider provider)
        {
            _serverInfoBll = provider.GetRequiredService<ServerInfoBll>();
            _authorizeBll = provider.GetRequiredService<AuthorizeBLL>();
            _custInfoBll = provider.GetRequiredService<CustInfoBll>();
            _selLangBll = provider.GetRequiredService<SelLangBLL>();
            _hotEventBll = provider.GetRequiredService<HotEventBLL>();
            _checkAttackBll = provider.GetRequiredService<CheckAttackBLL>();
            _logger = provider.GetRequiredService<ILogger<HotEventController>>();
        }

        [HttpPost]
        public async Task<ApiResult<IEnumerable<HotEventResponse>>> GetDataList([FromRoute] string langId = null)
        {
            var apiResult = new ApiResult<IEnumerable<HotEventResponse>>();

            if (_checkAttackBll.CheckAttackIp(Request))
            {
                Request.HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                return null;
            }
            
            try
            {
                var serverInfo = _serverInfoBll.GetServerInfo();
                var authData = _authorizeBll.GetAuthData();
                var siteId = 0;
                if (authData is { CustId: > 0 })
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

                apiResult.ErrorCode = 0;
                apiResult.Data = await _hotEventBll.GetHotEventDataList(siteId, langId, serverInfo);
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
