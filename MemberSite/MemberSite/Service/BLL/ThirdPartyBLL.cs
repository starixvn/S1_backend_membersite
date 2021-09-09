using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace MemberSite.Service.BLL
{
    public class ThirdPartyBLL
    {
        private readonly ThirdPartyService thirdPartyService;
        private readonly SelLangBLL selLangBLL;
        private readonly Tools tools;
        private readonly ConcurrentDictionary<string, TpCurrency> TpCurrencyList;
        private readonly ConcurrentDictionary<string, ThirdPartyModel> ThirdPartyModelContainer;
        private readonly CustInfoBLL custInfoBLL;
        private readonly CourierAdapter courierAdapter;
        private readonly string apiUrl;
        private readonly ILogger<ThirdPartyBLL> _logger;
        private readonly CacheHelper _cacheHelper;

        readonly Dictionary<string, string> sportTypeDic = new Dictionary<string, string>() {
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
        public ThirdPartyBLL(IServiceProvider provider)
        {
            thirdPartyService = provider.GetRequiredService<ThirdPartyService>();
            selLangBLL = provider.GetRequiredService<SelLangBLL>();
            tools = provider.GetRequiredService<Tools>();
            TpCurrencyList = new ConcurrentDictionary<string, TpCurrency>();
            ThirdPartyModelContainer = new ConcurrentDictionary<string, ThirdPartyModel>();
            custInfoBLL = provider.GetRequiredService<CustInfoBLL>();
            courierAdapter = provider.GetRequiredService<CourierAdapter>();
            var configuration = provider.GetRequiredService<IConfiguration>();
            apiUrl = configuration.GetValue<string>("TpService");
            _logger = provider.GetRequiredService<ILogger<ThirdPartyBLL>>();
            _cacheHelper = provider.GetRequiredService<CacheHelper>();
        }

        public ThirdPartyModel GetThirdPartyModel(int tpId, int siteId)
        {

            var sKet = $"{tpId}_{siteId}";
            if (!ThirdPartyModelContainer.ContainsKey(sKet))
            {
                var thirdPartyModel = thirdPartyService.GetThirdPartyBySite(tpId, siteId);
                if (thirdPartyModel != null)
                    ThirdPartyModelContainer.TryAdd(sKet, thirdPartyModel);
            }

            return ThirdPartyModelContainer.ContainsKey(sKet) ? ThirdPartyModelContainer[sKet] : null;
        }

        public TpCurrency GetTpCurrency(int tpId, int currencyId)
        {
            var key = $"{tpId}_{currencyId}";
            if (!TpCurrencyList.ContainsKey(key))
            {
                TpCurrencyList.TryAdd(key, thirdPartyService.GetTpCurrency(tpId, currencyId));
            }

            var tpCurrency = TpCurrencyList[key];
            return tpCurrency;
        }
        public IEnumerable<TpWallet> GetTpWallet(int custId)
        {
            return thirdPartyService.GetTpWallet(custId);
        }

        public IEnumerable<TpTransferInfoHistory> GetTransferHistory(int custId, long startDate, long endDate, ref int pageNumber, out int totalPages, out int rowCount)
        {
            var CustInfo = custInfoBLL.GetCustInfoWithCache(custId);
            var tpTransferInfoHistory = thirdPartyService.GetTransferHistory(CustInfo.CustId, startDate, endDate, CustInfo.SiteId, 5, ref pageNumber, out totalPages, out rowCount);
            if (tpTransferInfoHistory != null)
            {
                foreach (var tpTransferInfo in tpTransferInfoHistory)
                {
                    tpTransferInfo.TpName = selLangBLL.GetResourceValue($"lbl_Wallet_{GetThirdPartyModel(tpTransferInfo.TpId, CustInfo.SiteId).TpName}");
                }
            }
            return tpTransferInfoHistory;
        }
        public IEnumerable<TpTransferInfoHistory> GetTransferRecent(int custId)
        {
            var CustInfo = custInfoBLL.GetCustInfoWithCache(custId);
            var tpTransferInfoHistory = thirdPartyService.GetTransferRecent(custId, CustInfo.SiteId, 5);
            if (tpTransferInfoHistory != null)
            {
                foreach (var tpTransferInfo in tpTransferInfoHistory)
                {
                    tpTransferInfo.TpName = selLangBLL.GetResourceValue($"lbl_Wallet_{GetThirdPartyModel(tpTransferInfo.TpId, CustInfo.SiteId).TpName}");
                }
            }
            return tpTransferInfoHistory;
        }

        private class TpInfo
        {
            public int TpId { set; get; }
            public int CustId { set; get; }
            public Dictionary<string, string> inputs { set; get; }
        }

        /*Sports Wallet*/
        public async Task<ApiResult<UserBalance>> GetSportsBalanceAsync(int custId)
        {
            var myUrl = $"{apiUrl}GetBalance";
            var apiResult = new ApiResult<UserBalance>();
            var oTpInfo = new TpInfo
            {
                TpId = 1,
                CustId = custId,
                inputs = new Dictionary<string, string>()
            };

            try
            {
                var data = await courierAdapter.MyPostAsync<UserBalance, TpInfo>(myUrl, oTpInfo);
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
        /*Sports Wallet*/
        public async Task<ApiResult<UserBalance>> GetTpWalletBalanceAsync(int tpId, int custId)
        {
            var myUrl = $"{apiUrl}GetBalance";
            var apiResult = new ApiResult<UserBalance>();
            var oTpInfo = new TpInfo
            {
                TpId = tpId,
                CustId = custId,
                inputs = new Dictionary<string, string>()
            };

            try
            {
                var data = await courierAdapter.MyPostAsync<UserBalance, TpInfo>(myUrl, oTpInfo);
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
            return thirdPartyService.GetTpWalletList(custId, listType);
        }

        public IEnumerable<TpMaintain> GetTpMaintainListWithCache(int siteId)
        {
            var key = $"GetTpMaintainList{siteId}";
            return _cacheHelper.CacheTryGetValueSet<IEnumerable<TpMaintain>>(key,
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(30)),
                i => thirdPartyService.GetTpMaintainList(siteId)
                    .Where(item => item.MaintainCheck(DateTime.UtcNow, _logger)));
        }
    }
}
