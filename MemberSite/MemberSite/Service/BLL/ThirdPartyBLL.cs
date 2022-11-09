using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace MemberSite.Service.BLL
{
    public class ThirdPartyBll
    {
        private readonly ThirdPartyService _thirdPartyService;
        private readonly SelLangBLL _selLangBll;
        private readonly ConcurrentDictionary<string, TpCurrency> _tpCurrencyList;
        private readonly ConcurrentDictionary<string, ThirdPartyModel> _thirdPartyModelContainer;
        private readonly CustInfoBll _custInfoBll;
        private readonly CourierAdapter _courierAdapter;
        private readonly string _apiUrl;
        private readonly ILogger<ThirdPartyBll> _logger;
        private readonly CacheHelper _cacheHelper;

        readonly Dictionary<string, string> _sportTypeDic = new Dictionary<string, string>() {
            {"1","soccer" },
            {"2","basketball" },
            {"3","football" },
            {"4","icehockey" },
            {"5","tennis" },
            {"8","baseball" } ,
            {"9","badminton" } ,
            {"43","esports" },
            {"50", "indiancricket" },
            {"161","numbergame" },
            {"180","virtualsports" }
        };
        public ThirdPartyBll(IServiceProvider provider)
        {
            _thirdPartyService = provider.GetRequiredService<ThirdPartyService>();
            _selLangBll = provider.GetRequiredService<SelLangBLL>();
            _tpCurrencyList = new ConcurrentDictionary<string, TpCurrency>();
            _thirdPartyModelContainer = new ConcurrentDictionary<string, ThirdPartyModel>();
            _custInfoBll = provider.GetRequiredService<CustInfoBll>();
            _courierAdapter = provider.GetRequiredService<CourierAdapter>();
            var configuration = provider.GetRequiredService<IConfiguration>();
            _apiUrl = configuration.GetValue<string>("TpService");
            _logger = provider.GetRequiredService<ILogger<ThirdPartyBll>>();
            _cacheHelper = provider.GetRequiredService<CacheHelper>();
        }

        public ThirdPartyModel GetThirdPartyModel(int tpId, int siteId)
        {

            var sKet = $"{tpId}_{siteId}";
            if (!_thirdPartyModelContainer.ContainsKey(sKet))
            {
                var thirdPartyModel = _thirdPartyService.GetThirdPartyBySite(tpId, siteId);
                if (thirdPartyModel != null)
                    _thirdPartyModelContainer.TryAdd(sKet, thirdPartyModel);
            }

            return _thirdPartyModelContainer.ContainsKey(sKet) ? _thirdPartyModelContainer[sKet] : null;
        }

        public TpCurrency GetTpCurrency(int tpId, int currencyId)
        {
            var key = $"{tpId}_{currencyId}";
            if (!_tpCurrencyList.ContainsKey(key))
            {
                _tpCurrencyList.TryAdd(key, _thirdPartyService.GetTpCurrency(tpId, currencyId));
            }

            var tpCurrency = _tpCurrencyList[key];
            return tpCurrency;
        }
        public IEnumerable<TpWallet> GetTpWallet(int custId)
        {
            return _thirdPartyService.GetTpWallet(custId);
        }

        public IEnumerable<TpTransferInfoHistory> GetTransferHistory(int custId, long startDate, long endDate, ref int pageNumber, out int totalPages, out int rowCount)
        {
            var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
            var tpTransferInfoHistory = _thirdPartyService.GetTransferHistory(custInfo.CustId, startDate, endDate, custInfo.SiteId, 5, ref pageNumber, out totalPages, out rowCount);
            if (tpTransferInfoHistory != null)
            {
                foreach (var tpTransferInfo in tpTransferInfoHistory)
                {
                    if (tpTransferInfo.IsAdjustment)
                    {
                        tpTransferInfo.Amount = tpTransferInfo.Direction == 1
                            ? tpTransferInfo.Amount
                            : tpTransferInfo.Amount * -1;
                    }
                    tpTransferInfo.TpName = _selLangBll.GetResourceValue($"lbl_Wallet_{GetThirdPartyModel(tpTransferInfo.TpId, custInfo.SiteId).TpName}");
                }
            }
            return tpTransferInfoHistory;
        }
        public IEnumerable<TpTransferInfoHistory> GetTransferRecent(int custId)
        {
            var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
            var tpTransferInfoHistory = _thirdPartyService.GetTransferRecent(custId, custInfo.SiteId, 5);
            if (tpTransferInfoHistory != null)
            {
                foreach (var tpTransferInfo in tpTransferInfoHistory)
                {
                    tpTransferInfo.TpName = _selLangBll.GetResourceValue($"lbl_Wallet_{GetThirdPartyModel(tpTransferInfo.TpId, custInfo.SiteId).TpName}");
                }
            }
            return tpTransferInfoHistory;
        }

        private class TpInfo
        {
            public int TpId { set; get; }
            public int CustId { set; get; }
            public Dictionary<string, string> Inputs { set; get; }
        }

        /*Sports Wallet*/
        public async Task<ApiResult<UserBalance>> GetTpWalletBalanceAsync(int tpId, int custId)
        {
            var myUrl = $"{_apiUrl}GetBalance";
            var apiResult = new ApiResult<UserBalance>();
            var oTpInfo = new TpInfo
            {
                TpId = tpId,
                CustId = custId,
                Inputs = new Dictionary<string, string>()
            };

            try
            {
                var data = await _courierAdapter.MyPostAsync<UserBalance, TpInfo>(myUrl, oTpInfo);
                apiResult.Data = data.Data;
                apiResult.ErrorCode = data.ErrorCode;
                apiResult.RawErrorCode = data.RawErrorCode;
                apiResult.RawMessage = data.RawMessage;
            }
            catch (Exception ex)
            {
                apiResult.ErrorCode = 9000;
                apiResult.RawMessage = ex.Message;
            }

            return apiResult;
        }
        public IEnumerable<TpWalletList> GetTpWalletList(int custId, int listType)
        {
            return _thirdPartyService.GetTpWalletList(custId, listType);
        }

        public IEnumerable<TpMaintain> GetTpMaintainListWithCache(int siteId)
        {
            var key = $"GetTpMaintainList{siteId}";
            return _cacheHelper.TryGetOrCreate(key,
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(30)),
                i => _thirdPartyService.GetTpMaintainList(siteId)
                    .Where(item => item.MaintainCheck(DateTime.UtcNow)));
        }
    }
}
