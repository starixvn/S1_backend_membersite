using MemberSite.Service.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MemberSite.Controllers.Api
{

    [Route("api/[controller]/[action]/{LangId}")]
    [ApiController]
    public class StoreController : ControllerBase
    {

        private readonly SelLangBLL _selLangBLL;
        private readonly PromotionBll _promotionBLL;
        private readonly ServerInfoBll _serverInfoBLL;
        private readonly AuthorizeBLL _authorizeBLL;
        private readonly CustInfoBll _custInfoBLL;
        private readonly string _bonusServiceUrl;
        private readonly CourierAdapter _courierAdapter;
        private readonly ILogger<PromotionController> _logger;
        private readonly Tools _tools;

        public StoreController(IServiceProvider provider)
        {
            _selLangBLL = provider.GetRequiredService<SelLangBLL>();
            _promotionBLL = provider.GetRequiredService<PromotionBll>();
            _serverInfoBLL = provider.GetRequiredService<ServerInfoBll>();
            _authorizeBLL = provider.GetRequiredService<AuthorizeBLL>();
            _custInfoBLL = provider.GetRequiredService<CustInfoBll>();
            _bonusServiceUrl = provider.GetRequiredService<IConfiguration>().GetValue<string>("BonusService");
            _courierAdapter = provider.GetRequiredService<CourierAdapter>();
            _logger = provider.GetRequiredService<ILogger<PromotionController>>();
            _tools = provider.GetRequiredService<Tools>();
        }


        [HttpPost]
        public async Task<ApiResult<object>> GetGiftListAsync(PromotionDeviceReq promotionDeviceReq)
        {
            var result = new ApiResult<List<CustPromotion>>();
            try
            {
                var serverInfo = _serverInfoBLL.GetServerInfo();
                var isLogin = User.Identity.IsAuthenticated;
                var custId = 0;
                int currencyId;
                if (isLogin)
                {
                    var authData = _authorizeBLL.GetAuthData();
                    var custInfo = _custInfoBLL.GetCustInfoWithCache(authData.CustId);
                    custId = authData.CustId;
                    currencyId = custInfo.CurrencyId;
                }
                else
                {
                    currencyId = _selLangBLL.GetPreferCurrencyId();
                }

                var apiUrl = $"{_bonusServiceUrl}GetPromotionListV3";
                var req = new CustPromotionRequest
                {
                    SiteId = serverInfo.SiteId,
                    CustId = custId,
                    CurrencyId = currencyId,
                    DeviceType = promotionDeviceReq.DeviceType,
                    IsLogin = isLogin,
                    LangId = _selLangBLL.GetLangId(),
                    DefLang = serverInfo.DefLang
                };

                result = _courierAdapter.MyPostAsync<List<CustPromotion>, CustPromotionRequest>(apiUrl, req).Result;
            }
            catch (Exception e)
            {
                _logger.LogError(e, GetCurrentMethod());
            }

            return result;
        }

    }
}
