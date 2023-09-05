using MemberSite.Extensions;
using MemberSite.Service.BLL;
using MemberSite.Service.Model;
using MemberSite.Service.Model.Store.Frontend;

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
        public async Task<object> GetGiftListAsync(GetGiftListRequest request, string LangId = "en")
        {
            var custId = 0;
            var currencyId = 0;

            var authData = _authorizeBLL.GetAuthData();
            if (authData is { CustId: > 0 })
            {
                var custInfo = _custInfoBLL.GetCustInfoWithCache(authData.CustId);
                currencyId = custInfo.CurrencyId;
                custId = authData.CustId;
            }
            else
                currencyId = (int)StoreExtension.GetCurrencyByLangId(LangId);

            var apiRequest = new StoreGetGiftListRequest
            {
                CustId = custId,
                SiteId = _serverInfoBLL.GetServerInfo().SiteId,
                CurrencyId = currencyId,
                CategoryIds = request.CategoryIds,
                Tags = request.Tags,
                Lang = LangConverter(LangId), // 應前端要求，一律改吃LangId
                OrderByField = request.OrderByField,
                Desc = request.Desc,
                PageSize = request.PageSize
            };

            return await _storeBLL.GetGiftListAsync(apiRequest);
        }

        [HttpPost]
        public async Task<object> GetMyCoinAsync()
        {
            var authData = _authorizeBLL.GetAuthData();
            if (authData == null || authData.CustId == 0)
                return Task.FromResult<object>(new ApiResult()
                {
                    ErrorCode = -1,
                    Message = "Cust is Invalid.Please Contact to CSD."
                });

            return await _storeBLL.GetMyCoinAsync(new StoreGetMyCoinRequest
            {
                CustId = authData.CustId
            });
        }

        [HttpPost]
        public async Task<object> GetGiftTagsAsync(string LangId = "")
        {
            var lang = string.IsNullOrEmpty(LangId)
                ? _selLangBLL.GetLangId()
                : LangId; // 因前端需求，有帶LangId時優先使用LangId

            return await _storeBLL.GetGiftTagsAsync(new StoreGetTagsRequest
            {
                LangCodes = new List<string>() { LangConverter(lang) }
            });
        }

        [HttpPost]
        public async Task<object> GetMyPurchesAsync(GetMyPurchesRequest request)
        {
            var authData = _authorizeBLL.GetAuthData();
            if (authData == null || authData.CustId == 0)
                return Task.FromResult<object>(new ApiResult()
                {
                    ErrorCode = -1,
                    Message = "Cust is Invalid.Please Contact to CSD."
                });

            return await _storeBLL.GetMyPurchesAsync(new StoreGetMyPurchesRequest
            {
                CustId = authData.CustId,
                Lang = authData.LangId,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            });
        }

        public async Task<object> SetFavoriteAsync(SetFavoriteRequest request)
        {
            try
            {
                var authData = _authorizeBLL.GetAuthData();
                if (authData is not { CustId: > 0 })
                {
                    return new ApiResult(-1, _selLangBLL.GetMsg("msg_LoginToAccess"));
                }

                var apiRequest = new StoreSetFavoriteRequest
                {
                    CustId = authData.CustId,
                    GiftId = request.GiftId,
                    IsFavorite = request.IsFavorite
                };

                return await _storeBLL.SetFavoriteAsync(apiRequest);
            }
            catch (Exception e)
            {
                _logger.LogError(e, GetCurrentMethod());
                return new ApiResult();
            }
        }

        public async Task<object> GetPointBalanceAsync()
        {
            try
            {
                var authData = _authorizeBLL.GetAuthData();
                if (authData is not { CustId: > 0 })
                {
                    return new ApiResult(-1, _selLangBLL.GetMsg("msg_LoginToAccess"));
                }

                var apiRequest = new StoreGetBalanceRequest
                {
                    CustId = authData.CustId
                };

                var result = await _storeBLL.GetBalanceAsync(apiRequest);

                return result;
            }
            catch (Exception e)
            {
                _logger.LogError(e, GetCurrentMethod());
                return new ApiResult();
            }
        }

        private static string LangConverter(string lang)
        {
            if (string.IsNullOrWhiteSpace(lang))
            {
                return lang;
            }

            return lang.Contains("en", StringComparison.OrdinalIgnoreCase)
                ? "en" : lang;
        }
    }
}
