using System.Web;
using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using MemberSite.Service.Model.Game;

namespace MemberSite.Service.BLL
{
    public class GameBLL
    {
        private readonly MySqlClient _reportDbClient;
        private readonly ICacheHelper _cacheHelper;
        private readonly Dictionary<int, string> _openPathList;
        private readonly StatementService _statementService;
        private readonly ThirdPartyBll _thirdPartyBll;

        public GameBLL(IServiceProvider provider)
        {
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
            _cacheHelper = provider.GetRequiredService<ICacheHelper>();
            _openPathList = new Dictionary<int, string>()
            {
                { 22, "mega888"}
            };
            _statementService = provider.GetRequiredService<StatementService>();
            _thirdPartyBll = provider.GetRequiredService<ThirdPartyBll>();
        }

        public bool IsMaintain(int siteId, int tpId, string tpGameType, out TpMaintain result)
        {
            result = null;
            try
            {
                var tpMaintainList = _thirdPartyBll.GetTpMaintainListWithCache(siteId);

                if (!tpMaintainList.Any())
                {
                    return false;
                }

                if (string.IsNullOrEmpty(tpGameType))
                {
                    return false;
                }

                var gameType = int.Parse(tpGameType);
                foreach (var item in tpMaintainList)
                {
                    if ((item.TpId == tpId && item.TpGameType == 0) || (item.TpId == tpId && item.TpGameType == gameType))
                    {
                        result = item;
                        return true;
                    }
                }

                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<IEnumerable<GameProvider>> GetGameProviderByCategory(int siteId, int currencyId, TpCategory tpCategory)
        {
            return await _cacheHelper.TryGetOrCreateAsync($"GetGameProviderByCategory:{currencyId}:{siteId}:{tpCategory}",
                TimeSpan.FromMinutes(10),
                async () =>
                {
                    var providerList = await _reportDbClient.QueryAsync<GameProvider>("Member_TpList_GetByCategory", new
                    {
                        CurrencyId = currencyId,
                        SiteId = siteId,
                        TpCategoryId = (int)tpCategory,
                    });
                    foreach (var provider in providerList)
                    {
                        provider.ImgName = $"{tpCategory}_{provider.TpName}".ToLower();
                        provider.OpenPath = _openPathList.TryGetValue(provider.TpId, out var openPath) ? openPath : null;
                    }
                    return providerList;
                });
        }

        public string GetMaintainUrl(TpMaintain tpMaintain, string host, string langId, string entPageUrl = null)
        {
            var requestId = Guid.NewGuid().ToString();        
            
            //維護結束後要回到哪個url，GotoGame入口只能回到首頁
            var returnUrl = entPageUrl ?? host;
            var tpName = _statementService.GetTpListWithCache()?.First(i => i.Id == tpMaintain.TpId)?.TpName;
            var maintainUrl = $"{host}/tp/index_{langId}.html?" +
                $"t={HttpUtility.UrlEncode(tpName)}&" +
                $"e={HttpUtility.UrlEncode($"{tpMaintain.EndDate:u}")}&" +
                $"i={tpMaintain.TpId}&" +
                $"g={tpMaintain.TpGameType}&" +
                $"r={requestId}";

            _cacheHelper.StringSet($"TpMaintain:{requestId}", returnUrl, TimeSpan.FromDays(1));          
            return maintainUrl;
        }
    }
}