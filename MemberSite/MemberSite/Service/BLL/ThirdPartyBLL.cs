using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Google.Protobuf.WellKnownTypes.Field;

namespace MemberSite.Service.BLL
{
    public class ThirdPartyBLL
    {
        private readonly ThirdPartyService thirdPartyService;
        private readonly SelLangBLL selLangBLL;
        private readonly Tools tools;
        private ConcurrentDictionary<string, TpCurrency> TpCurrencyList;
        private ConcurrentDictionary<string, ThirdPartyModel> ThirdPartyModelContainer;
        private readonly ServerInfoBLL serverInfoBLL;
        private readonly CustInfoBLL custInfoBLL;
        private readonly CourierAdapter courierAdapter;
        private readonly string apiUrl;
        private readonly ILogger<ThirdPartyBLL> logger;

        Dictionary<string, string> sportTypeDic = new Dictionary<string, string>() {
            {"1","soccer" },
            {"2","basketball" },
            {"3","football" },
            {"4","icehockey" },
            {"5","tennis" },
            {"8","baseball" } ,
            {"9","badminton" } ,
            {"43","esports" },
            {"161","numbergame" },
            {"180","virtualsports" },
            {"2000","OLYMPIC TOKYO 2020"}
        };
        public ThirdPartyBLL(IServiceProvider provider)
        {
            thirdPartyService = provider.GetRequiredService<ThirdPartyService>();
            selLangBLL = provider.GetRequiredService<SelLangBLL>();
            tools = provider.GetRequiredService<Tools>();
            TpCurrencyList = new ConcurrentDictionary<string, TpCurrency>();
            ThirdPartyModelContainer = new ConcurrentDictionary<string, ThirdPartyModel>();
            serverInfoBLL = provider.GetRequiredService<ServerInfoBLL>(); 
            custInfoBLL = provider.GetRequiredService<CustInfoBLL>();
            courierAdapter = provider.GetRequiredService<CourierAdapter>();
            var configuration = provider.GetRequiredService<IConfiguration>();
            apiUrl = configuration.GetValue<string>("TpService");
            logger = provider.GetRequiredService<ILogger<ThirdPartyBLL>>();
        }

        public string GetTpLanId(int tpId)
        {
            return thirdPartyService.GetTpLanId(tpId, selLangBLL.GetLangId());
        }

        public ThirdPartyModel GetThirdPartyModel(int tpId, int siteId)
        {

            string sKet = $"{tpId}_{siteId}";
            if (!ThirdPartyModelContainer.ContainsKey(sKet))
            {
                var thirdPartyModel = thirdPartyService.GetThirdPartyBySite(tpId, siteId);
                if (thirdPartyModel != null)
                    ThirdPartyModelContainer.TryAdd(sKet, thirdPartyModel);
            }

            return ThirdPartyModelContainer.ContainsKey(sKet) ? ThirdPartyModelContainer[sKet] : null;
        }

        public string getEntryUrl(int tpId, Dictionary<string, string> parameters)
        {
            Dictionary<string, string> inputs = JsonConvert.DeserializeObject<Dictionary<string, string>>(thirdPartyService.GetThirdPartyBySite(tpId, serverInfoBLL.GetServerInfo().SiteId).Setting);
            string urlKey = "DesktopBfUrl";
            string device = null;
            parameters.TryGetValue("device", out device);

            if (string.Compare(device, "wap", StringComparison.OrdinalIgnoreCase) == 0)
            {
                urlKey = "WAPUrl";
            }
            else if (tools.IsMobile())
            {
                urlKey = "MobileBfUrl";
            }
            string entryUrl = inputs[urlKey];
            var tpLanId = thirdPartyService.GetTpLanId(tpId, selLangBLL.GetLangId());

            switch (tpId)
            {
                case 1:
                    string SportType = null;
                    parameters.TryGetValue("gameId", out SportType);

                    if (tools.IsMobile())
                    {
                        string types = $"{SportType},0,l";
                        if (SportType == "43")
                        {
                            sportTypeDic.TryGetValue(SportType, out types);
                            entryUrl = $"{entryUrl}?lang={tpLanId}&homeUrl={parameters["homeUrl"]}&signupUrl={parameters["signupUrl"]}&LoginUrl={parameters["LoginUrl"]}&types={types}";
                        }
                        else if (SportType == "2000")
                        {
                            entryUrl = $"{entryUrl}?lang={tpLanId}&homeUrl={parameters["homeUrl"]}&signupUrl={parameters["signupUrl"]}&LoginUrl={parameters["LoginUrl"]}&types=soccer&menutype=1&market=E";
                        }
                        else
                        {
                            entryUrl = $"{entryUrl}?lang={tpLanId}&homeUrl={parameters["homeUrl"]}&signupUrl={parameters["signupUrl"]}&LoginUrl={parameters["LoginUrl"]}#Sports/s={SportType}_0&d=l";
                        }
                    }
                    else
                    {
                        string act = null;
                        string gameId = null;
                        string matchId = null;

                        if (parameters.TryGetValue("gameId", out gameId))
                        {
                            sportTypeDic.TryGetValue(gameId, out act);
                        }
                        parameters.TryGetValue("matchid", out matchId);
                        
                        if (SportType == "43")
                        {
                            entryUrl = $"{entryUrl}?lang={tpLanId}&game={act}";
                        }
                        else if (SportType == "2000")
                        {
                            entryUrl = $"{entryUrl}?lang={tpLanId}&game=soccer&menutype=1&market=E";
                        }
                        else
                        {
                            if (matchId == null)
                            {
                                entryUrl = $"{entryUrl}?lang={tpLanId}&act={act}";
                            }
                            else
                            {
                                entryUrl = $"{entryUrl}?lang={tpLanId}&act={act}&matchid={matchId}";
                            }
                        }
                    }

                    return entryUrl;
                default:
                    return null;
            }
        }
        public TpCurrency GetTpCurrency(int tpId, int currencyId)
        {
            string key = $"{tpId}_{currencyId}";
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
            return thirdPartyService.GetTpWalletList(custId,listType);
        }

        public IEnumerable<TpMaintain> GetTpMaintainList(int siteId)
        {
            return thirdPartyService.GetTpMaintainList(siteId).Where(item => item.MaintainCheck(DateTime.UtcNow, logger));
        }
    }
}
