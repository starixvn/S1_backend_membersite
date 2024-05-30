using System.Net;
using MemberSite.Service.BLL;
using MemberSite.Service.BLL.Interface;
using MemberSite.Service.Model;

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
        private readonly CheckAttackBLL _checkAttackBll;

        public HotGameController(IHotGameBLL hotGameBll,
            AuthorizeBLL authorizeBll,
            CustInfoBll customerInfoBll,
            ServerInfoBll serverInfoBll,
            SelLangBLL selLangBll,
            CheckAttackBLL checkAttackBll)
        {
            this._hotGameBll = hotGameBll;
            this._authorizeBll = authorizeBll;
            this._customerInfoBll = customerInfoBll;
            this._serverInfoBll = serverInfoBll;
            this._selLangBll = selLangBll;
            this._checkAttackBll = checkAttackBll;
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
            if (authData is { CustId: > 0 })
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

            try
            {
                var data = await _hotGameBll.GetHotGames(siteId, currencyId, request.DeviceType, langId);
                apiResult.Data = data;
                apiResult.ErrorCode = 0;
            }
            catch (Exception ex)
            {
                apiResult.Message = ex.Message;
            }

            return apiResult;
        }
    }
}
