using System.Net;
using MemberSite.Service.BLL;
using MemberSite.Service.BLL.Interface;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MemberSite.Controllers.Api
{
    [AllowAnonymous]
    [Route("api/[controller]/[action]")]
    [Route("api/[controller]/[action]/{LangId}")]
    [ApiController]
    public class HotGameController : ControllerBase
    {
        private readonly IHotGameBLL _hotGameBll;
        private readonly AuthorizeBLL _authorizeBll;
        private readonly CustInfoBll _customerInfoBll;
        private readonly ServerInfoBll _serverInfoBll;
        private readonly SelLangBLL _selLangBll;
        private readonly ICacheHelper _cacheHelper;
        private readonly CheckAttackBLL _checkAttackBll;

        public HotGameController(IServiceProvider provider)
        {
            _hotGameBll = provider.GetRequiredService<IHotGameBLL>();
            _authorizeBll = provider.GetRequiredService<AuthorizeBLL>();
            _customerInfoBll = provider.GetRequiredService<CustInfoBll>();
            _serverInfoBll = provider.GetRequiredService<ServerInfoBll>();
            _selLangBll = provider.GetRequiredService<SelLangBLL>();
            _cacheHelper = provider.GetRequiredService<ICacheHelper>();
            _checkAttackBll = provider.GetRequiredService<CheckAttackBLL>();
        }

        [HttpPost]
        public async Task<ApiResult<Dictionary<string, IEnumerable<HotGameResponse>>>> GetDataList(GetHotGameDataListRequest request, [FromRoute] string langId = null)
        {
            if (_checkAttackBll.CheckAttackIp(Request))
            {
                Request.HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                return null;
            }
            
            int currencyId;
            int siteId;
            var authData = _authorizeBll.GetAuthData();
            var isLogin = authData is { CustId: > 0 };
            if (isLogin)
            {
                var customerInfo = _customerInfoBll.GetCustInfoWithCache(authData.CustId);
                siteId = customerInfo.SiteId;
                currencyId = customerInfo.CurrencyId;
                langId = _selLangBll.GetLangId();
            }
            else
            {
                var serverInfo = _serverInfoBll.GetServerInfo();
                siteId = serverInfo.SiteId;
                currencyId = request.CurrencyId ?? _selLangBll.GetPreferCurrencyId();
                langId = _selLangBll.CheckLangId(langId) ? langId : _selLangBll.GetLangId();
            }

            var apiResult = new ApiResult<Dictionary<string, IEnumerable<HotGameResponse>>>();
            // 未登入檢查是否有Cache
            var key = $"HotGame:GetDataList:{siteId}:{currencyId}:{request.DeviceType}:{langId}";
            if (!isLogin && _cacheHelper.TryGetValue(key, out Dictionary<string, IEnumerable<HotGameResponse>> hotGameList))
            {
                apiResult.Data = hotGameList;
                apiResult.ErrorCode = (int)ErrorCode.Success;
                return apiResult;
            }

            try
            {
                var data = await _hotGameBll.GetHotGames(siteId, currencyId, request.DeviceType, langId);
                apiResult.Data = data;
                apiResult.ErrorCode = (int)ErrorCode.Success;
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
