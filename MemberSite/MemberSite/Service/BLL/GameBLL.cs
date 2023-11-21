using System.Web;
using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using MemberSite.Service.Model.Game;
using Newtonsoft.Json;

namespace MemberSite.Service.BLL
{
    public class GameBLL
    {
        private readonly MySqlClient _reportDbClient;
        private readonly ICacheHelper _cacheHelper;
        private readonly Dictionary<int, string> _openPathList;
        private readonly StatementService _statementService;
        private readonly GameService _gameService;
        private readonly ThirdPartyBll _thirdPartyBll;
        private readonly Dictionary<string, SiteCloudFrontImageInfo> _siteCloudFrontImageInfo;
        
        public GameBLL(IServiceProvider provider)
        {
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
            _cacheHelper = provider.GetRequiredService<ICacheHelper>();
            _openPathList = new Dictionary<int, string>()
            {
                { 22, "mega888"}
            };
            _statementService = provider.GetRequiredService<StatementService>();
            _gameService = provider.GetRequiredService<GameService>();
            _thirdPartyBll = provider.GetRequiredService<ThirdPartyBll>();
            var configBll = provider.GetRequiredService<ConfigBLL>();
            _siteCloudFrontImageInfo = configBll.GetConfigValuesBySiteName<SiteCloudFrontImageInfo>("SiteCloudFrontImageInfo");
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

        public async Task<IEnumerable<GameProvider>> GetGameProviderByCategory(int siteId, int currencyId, TpCategory tpCategory , Dictionary<string, string> changeGameImgName)
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
                        var tpName = changeGameImgName.TryGetValue(provider.TpName, out var changeName) ? changeName : provider.TpName;
                        provider.ImgName = $"{tpCategory}_{tpName}".ToLower();
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

        public async Task<List<TpGameIdListData>> GetTpGameIdList()
        {
            var key = $"GetTpGameIdList";
            var result = await _cacheHelper.TryGetOrCreateAsync(key,
                TimeSpan.FromMinutes(10),
                async () =>
                {
                    var result = await _gameService.GetTpGameIdList();
                    return result;
                });
            return result.ToList();
        }
        
        public async Task<IEnumerable<LobbyHeaderResponse>> GetLobbyHeader(GetLobbyHeaderRequest req ,ServerInfo serverInfo)
        {
            var key = $"GetLobbyHeader_{req.SiteId}_{req.CurrencyId}";
            var result = await _cacheHelper.TryGetOrCreateAsync(key,
                TimeSpan.FromMinutes(10),
                async () =>
                {
                    var resultData = await _gameService.GetLobbyHeader(req);
                    var imgDomain = _siteCloudFrontImageInfo[serverInfo.SiteName].BannerImageUrl;
                    
                    var groupedData = resultData.GroupBy(item => item.LobbyCategoryId);
                    var result = groupedData.Select(group =>
                        new LobbyHeaderResponse
                        {
                            LobbyCategoryId = group.Key,
                            Data = group.Select(item => new LobbyHeaderDetail
                            {
                                LobbyCategory = item.LobbyCategory,
                                TpId = item.TpId,
                                TpName = item.TpName,
                                ProductName = item.ProductName,
                                TpGameIdNo = item.TpGameIdNo,
                                GameId = item.GameId,
                                TpGameType = item.TpGameType,
                                CurrencyGameName = string.IsNullOrEmpty(item.CurrencyGameName) ? null : JsonConvert.DeserializeObject<Dictionary<string, string>>(item.CurrencyGameName),
                                PcHeaderPicUrl = $"{imgDomain}/desktop/img/Header/{item.TpName}/{item.LobbyCategory}/{item.ImgProductName}.png",
                                PcLobbyPicUrl = $"{imgDomain}/desktop/img/LobbyHeader/{item.TpName}/{item.LobbyCategory}/{item.ImgProductName}.png",
                                MobileHeaderPicUrl = $"{imgDomain}/h5/img/Header/{item.TpName}/{item.LobbyCategory}/{item.ImgProductName}.png",
                                MobileLobbyPicUrl = $"{imgDomain}/h5/img/LobbyHeader/{item.TpName}/{item.LobbyCategory}/{item.ImgProductName}.png",
                                Sort = item.Sort
                            }).OrderBy(x=>x.Sort)
                        });
                    return result;
                });
            
            return result;
        }
    }
}