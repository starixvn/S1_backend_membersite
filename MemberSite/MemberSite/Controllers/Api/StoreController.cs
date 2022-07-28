using MemberSite.Service.BLL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.Store.Frontend;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static MemberSite.Extensions.MethodExtension;

namespace MemberSite.Controllers.Api
{
    [Route("api/[controller]/[action]")]
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
        public Task<object> GetGiftListAsync(GetGiftListRequest request)
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

            return _storeBLL.GetGiftListAsync(apiRequest);

        }

        [HttpPost]
        public Task<object> GetMyCoinAsync()
        {

            var authData = _authorizeBLL.GetAuthData();
            if (authData == null || authData.CustId == 0)
                return Task.FromResult<object>(new ApiResult()
                {
                    ErrorCode = -1,
                    Message = "Cust is Invalid.Please Contact to CSD."
                });

            return _storeBLL.GetMyCoinAsync(new StoreGetMyCoinRequest
            {
                CustId = authData.CustId
            });
        }

        [HttpPost]
        public Task<object> GetMyPurchesAsync(GetMyPurchesRequest request)
        {

            var authData = _authorizeBLL.GetAuthData();
            if (authData == null || authData.CustId == 0)
                return Task.FromResult<object>(new ApiResult()
                {
                    ErrorCode = -1,
                    Message = "Cust is Invalid.Please Contact to CSD."
                });

            return _storeBLL.GetMyPurchesAsync(new StoreGetMyPurchesRequest
            {
                CustId = authData.CustId,
                Lang = authData.LangId,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            });
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
