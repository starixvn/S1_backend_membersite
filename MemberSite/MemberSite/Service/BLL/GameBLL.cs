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

        public GameBLL(IServiceProvider provider)
        {
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
            _cacheHelper = provider.GetRequiredService<ICacheHelper>();
            _openPathList = new Dictionary<int, string>()
            {
                { 22, "mega888"}
            };
        }

        public bool IsMaintain(IEnumerable<TpMaintain> tpMaintainList, int tpId, string tpGameType, out TpMaintain result)
        {
            result = null;
            try
            {
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
    }
}