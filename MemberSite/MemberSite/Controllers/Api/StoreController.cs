using MemberSite.Service.BLL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.Store.Frontend;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static MemberSite.Extensions.MethodExtension;

namespace MemberSite.Controllers.Api
{

    [Route("api/[controller]/[action]/{LangId}")]
    [ApiController]
    public class StoreController : ControllerBase
    {

        private readonly SelLangBLL _selLangBLL;
        private readonly ServerInfoBll _serverInfoBLL;
        private readonly AuthorizeBLL _authorizeBLL;
        private readonly CustInfoBll _custInfoBLL;
        private readonly StoreBLL _storeBLL;
        private readonly ILogger<PromotionController> _logger;

        public StoreController(IServiceProvider provider)
        {
            _selLangBLL = provider.GetRequiredService<SelLangBLL>();
            _serverInfoBLL = provider.GetRequiredService<ServerInfoBll>();
            _authorizeBLL = provider.GetRequiredService<AuthorizeBLL>();
            _custInfoBLL = provider.GetRequiredService<CustInfoBll>();
            _storeBLL = provider.GetRequiredService<StoreBLL>();
            _logger = provider.GetRequiredService<ILogger<PromotionController>>();
        }


        [HttpPost]
        public async Task<object> GetGiftListAsync(GetGiftListRequest request)
        {
            try
            {
                var apiRequest = new StoreGetGiftListRequest
                {
                    SiteId = _serverInfoBLL.GetServerInfo().SiteId,
                    CurrencyId = GetCurrencyId(),
                    CategoryIds = request.CategoryIds,
                    Tags = request.Tags,
                    Lang = _selLangBLL.GetLangId(),
                    OrderByField = request.OrderByField,
                    Desc = request.Desc
                };

                return await _storeBLL.GetGiftListAsync(apiRequest);
            }
            catch (Exception e)
            {
                _logger.LogError(e, GetCurrentMethod());
                return new ApiResult();
            }
        }

        private int GetCurrencyId()
        {
            if (User.Identity.IsAuthenticated)
            {
                var authData = _authorizeBLL.GetAuthData();
                var custInfo = _custInfoBLL.GetCustInfoWithCache(authData.CustId);
                return custInfo.CurrencyId;
            }
            else
                return _selLangBLL.GetPreferCurrencyId();
        }

    }
}
