using System.Security.Claims;

using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using MemberSite.Service.Model.Game;
using MemberSite.Service.Model.Lobby;

using Newtonsoft.Json;

namespace MemberSite.Service.BLL
{
    public class SlotGameBLL
    {
        private readonly ILogger<SlotGameBLL> _logger;
        private readonly MySqlClient _reportDbClient;
        private readonly ICacheHelper _cacheHelper;
        private readonly Dictionary<string, SiteCloudFrontImageInfo> _siteCloudFrontImageInfo;

        private readonly AuthorizeBLL _authorizeBll;
        private readonly ServerInfoBll _serverInfoBll;
        private readonly CustInfoBll _custInfoBll;
        private readonly GameBLL _gameBll;
        private readonly SelLangBLL _selLangBll;
        private readonly CasinoBll _casinoBll;
        private readonly LobbyCategoryBLL _lobbyCategoryBll;

        private readonly CasinoService _casinoService;
        private readonly Dictionary<int, int> _specialVenderLogoTp;
        private readonly List<int> _excludeTp;
        private readonly Dictionary<string, string> _changeGameImgName;
        private readonly int _isStage;


        public SlotGameBLL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<SlotGameBLL>>();
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
            _cacheHelper = provider.GetRequiredService<ICacheHelper>();
            var configBll = provider.GetRequiredService<ConfigBLL>();
            _siteCloudFrontImageInfo = configBll.GetConfigValuesBySiteName<SiteCloudFrontImageInfo>("SiteCloudFrontImageInfo");
            _isStage = configBll.GetConfigValueByEnvironment<int>("IsStage");
            _authorizeBll = provider.GetRequiredService<AuthorizeBLL>();
            _serverInfoBll = provider.GetRequiredService<ServerInfoBll>();
            _custInfoBll = provider.GetRequiredService<CustInfoBll>();
            _gameBll = provider.GetRequiredService<GameBLL>();
            _selLangBll = provider.GetRequiredService<SelLangBLL>();
            _casinoBll = provider.GetRequiredService<CasinoBll>();
            _lobbyCategoryBll = provider.GetRequiredService<LobbyCategoryBLL>();
            _casinoService = provider.GetRequiredService<CasinoService>();
            _specialVenderLogoTp = new Dictionary<int, int>()
            {
                { 67, 30 },
                { 109, 52},
                { 110, 52},
            };
            _excludeTp = new List<int>
            {
                1, //SABA
                41,//SBSW
                42,//SBO
            };
            _changeGameImgName = new Dictionary<string, string>()
            {
              {"Iconic","AT" },
              {"Kiss918","918Kiss" },
              {"END","ES" },
              {"EBet","WE" },
            };
        }

        public async Task<GetSlotGamePageResponse> GetSlotGamePage(GetSlotGamePageRequest request, ClaimsPrincipal user)
        {
            var serverInfo = _serverInfoBll.GetServerInfo();
            var (siteId, currencyId, custId) = GetUserInfo(request.CurrencyId, user, serverInfo);
            var imgUrl = _siteCloudFrontImageInfo[serverInfo.SiteName].OldImageUrl;

            // 取得SlotGame相關資料
            var providerList = await GetSlotGameProviderList(siteId, currencyId, TpCategory.Casino);
            var slotGameList = await GetSlotGameList(siteId, currencyId, request.DeviceType);
            var lobbyCategorySequence = await _lobbyCategoryBll.GetLobbyCategorySequenceByLobbyCategoryId(siteId, currencyId, LobbyCategory.Slots);
            var slotGameTopRanking = await GetSlotGameTopRanking(siteId, currencyId);

            // 整理資料與排序
            var slotGameProviderDict = slotGameList.GroupBy(i => i.TpId).ToDictionary(i => i.Key, i => i.Key);
            providerList = FilterAndSortProviderList(providerList, slotGameProviderDict, lobbyCategorySequence);
            slotGameList = SortSlotGameList(slotGameList, slotGameTopRanking);

            // 未登入狀態直接回傳
            if (custId == 0)
            {
                return new GetSlotGamePageResponse(imgUrl, providerList, slotGameList, request.SlotGameCategoryList);
            }

            //取得User的SlotGame相關資料         
            var disableGame = await GetDisableGameList(custId);
            var favoriteAndRecentlyPlayed = await GetFavoriteAndRecentlyPlayed(custId);

            return new GetSlotGamePageResponse(
                imgUrl: imgUrl,
                providerList: providerList,
                slotGameList: slotGameList,
                favorite: favoriteAndRecentlyPlayed.FavoriteList,
                recentlyPlayed: favoriteAndRecentlyPlayed.RecentlyPlayedList,
                disableGame: disableGame,
                slotGameCategoryList: request.SlotGameCategoryList);
        }

        /// <summary>
        /// 依條件取得SlotGame列表
        /// </summary>
        public async Task<List<SlotGameInfo>> GetSlotGameList(int siteId, int currencyId, int deviceType)
        {
            var statusList = new List<int> { 1 };
            if (_isStage == 1)
            {
                statusList.Add(3);
            }

            var key = $"GetSlotGameList:{_isStage}:{currencyId}:{siteId}:{deviceType}";
            var result = await _cacheHelper.TryGetOrCreateAsync(key,
                TimeSpan.FromMinutes(10),
                async () =>
                {
                    var result = await _reportDbClient.QueryAsync<SlotGameInfo>("Service_SlotGame_Get", new
                    {
                        CurrencyId = currencyId,
                        SiteId = siteId,
                        DeviceType = deviceType,
                        StatusJson = JsonConvert.SerializeObject(statusList)
                    });
                    return result;
                });
            return result.ToList();
        }

        /// <summary>
        /// 透過共用方法取回Slot Game Provider
        /// </summary>
        private async Task<List<GameProvider>> GetSlotGameProviderList(int siteId, int currencyId, TpCategory tpCategory)
        {
            var providerList = await _gameBll.GetGameProviderByCategory(siteId, currencyId, tpCategory, _changeGameImgName);
            var result = providerList.Where(i => !_excludeTp.Contains(i.TpId)).ToList();
            foreach (var provider in result)
            {
                provider.GameImgDisplayText = _changeGameImgName.TryGetValue(provider.TpName, out var changeName) ? changeName : provider.TpName;
            }
            return result;
        }

        /// <summary>
        /// 根據登入狀態回傳SiteId、CurrencyId、CustId
        /// </summary>
        private (int SiteId, int CurrencyId, int CustId) GetUserInfo(int? inputCurrencyId, ClaimsPrincipal user, ServerInfo serverInfo)
        {
            if (user?.Identity is { IsAuthenticated: true })
            {
                var authData = _authorizeBll.GetAuthData();
                var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
                return (custInfo.SiteId, custInfo.CurrencyId, authData.CustId);
            }
            else
            {
                var currencyId = _selLangBll.GetPreferCurrencyId();
                if (inputCurrencyId.HasValue)
                {
                    currencyId = inputCurrencyId.Value;
                }
                return (serverInfo.SiteId, currencyId, 0);
            }
        }

        public async Task<IEnumerable<DisableTpGame>> GetDisableGameList(int custId)
        {
            return await _casinoService.GetTpDisableUserByCustIdAsync(custId);
        }

        public async Task<FavoriteAndRecentlyPlayed> GetFavoriteAndRecentlyPlayed(int custId)
        {
            var result = new FavoriteAndRecentlyPlayed();
            var data = await _casinoService.GetFavoriteGamesAsync(custId);

            if (data == null)
            {
                return result;
            }

            if (!string.IsNullOrEmpty(data.Favorite))
            {
                result.FavoriteList = JsonConvert.DeserializeObject<IEnumerable<string>>(data.Favorite)
                    .Select(gameInfo => new UserGameInfo(gameInfo)).ToList();
            }

            if (!string.IsNullOrEmpty(data.RecentlyPlayed))
            {
                result.RecentlyPlayedList = JsonConvert.DeserializeObject<IEnumerable<string>>(data.RecentlyPlayed)
                    .Select(gameInfo => new UserGameInfo(gameInfo)).ToList();
            }
            return result;
        }

        public async Task<RecentlyViewedResponse> GetRecentlyViewed(RecentlyViewedRequest request, ClaimsPrincipal user)
        {
            var recentlyViewedInfo = new List<RecentlyViewedInfo>();

            var serverInfo = _serverInfoBll.GetServerInfo();
            var (siteId, currencyId, custId) = GetUserInfo(request.CurrencyId, user, serverInfo);
            var imgUrl = _siteCloudFrontImageInfo[serverInfo.SiteName].OldImageUrl;
            var bannerUrl = _siteCloudFrontImageInfo[serverInfo.SiteName].BannerImageUrl;
            var deviceType = request.DeviceType;
            var devicePath = deviceType == (int)HotGameDeviceType.Pc ? "desktop" : "h5";

            // 未登入狀態直接回傳
            if (custId == 0)
            {
                return new RecentlyViewedResponse(imgUrl, bannerUrl, recentlyViewedInfo);
            }

            var data = await _custInfoBll.GetCustRecentlyViewed(custId);
            if (data == null || !data.Any())
            {
                return new RecentlyViewedResponse(imgUrl, bannerUrl, recentlyViewedInfo);
            }

            // 取出有byCurrency跟Site設定的遊戲資訊
            var tpIds = _casinoBll.GetProviderIdByCurrency(currencyId, serverInfo.SiteId);
            // 取出Slot的遊戲資訊
            var slotGameList = await GetSlotGameList(siteId, currencyId, request.DeviceType);
            // 取出其他大廳的遊戲資訊
            var tpGameIdList = await _gameBll.GetTpGameIdList();

            foreach (var detail in tpGameIdList)
            {
                detail.ImgTpName = _changeGameImgName.TryGetValue(detail.ImgTpName, out var changeName) ? changeName : detail.ImgTpName;
            }

            foreach (var item in data)
            {
                var isMaintain = false;
                var gameName = new Dictionary<string, string>();
                var gameLogo = new Dictionary<string, string>();
                string[] parts = item.RecentlyViewed.Split(':');
                var type = int.Parse(parts[0]);
                var tpId = int.Parse(parts[1]);
                var tpGameType = int.Parse(parts[2]);
                var categoryId = int.Parse(parts[3]);
                var gameId = parts[4];
                var betType = (parts[5] == "0") ? "" : parts[5];
                var imgTpName = tpGameIdList.FirstOrDefault(r => r.TpId == tpId)?.ImgTpName;
                var tpGameIdNo = 0;

                // TP 關閉資料
                if (!tpIds.Contains(tpId))
                {
                    _logger.LogWarning($"GetRecentlyViewed tpIds disable : {tpId}");
                    continue;
                }

                // 檢查維護
                if (_gameBll.IsMaintain(siteId, tpId, tpGameType.ToString(), out var tpMaintain))
                {
                    isMaintain = true;
                }

                if (categoryId == (int)TpCategory.Casino && type == (int)RecentlyViewedTypeEnum.GameName)
                {
                    var gameSlotDetail = slotGameList.FirstOrDefault(i => i.TpId == tpId && (i.GameId == gameId || i.AppId == gameId));
                    if (gameSlotDetail == null)
                    {
                        _logger.LogWarning($"GetRecentlyViewed gameSlotDetail null : {tpId} - {gameId}");
                        continue;
                    }

                    var tpGameIdListDetail = tpGameIdList.FirstOrDefault(i => i.TpId == tpId && i.TpGameType == tpGameType);
                    gameName = gameSlotDetail.GameName;
                    gameLogo = gameSlotDetail.ImgName.ToDictionary(item => item.Key, item => $"{imgUrl}/{imgTpName}/{item.Value}");
                    tpGameIdNo = (int)(tpGameIdListDetail?.TpGameIdNo);
                }
                else
                {
                    var tpGameIdListDetail = tpGameIdList.FirstOrDefault(i => i.TpId == tpId && i.TpGameType == tpGameType && i.GameId == gameId && i.BetType == betType);
                    if (tpGameIdListDetail == null)
                    {
                        _logger.LogWarning($"GetRecentlyViewed tpGameIdListDetail null : {tpId} - {tpGameType} - {gameId} - {betType}");
                        continue;
                    }

                    if (tpGameIdListDetail.TpGameIdStatus != 1)
                    {
                        _logger.LogWarning($"GetRecentlyViewed TpGameIdStatus disable : {tpId} - {tpGameType} - {gameId} - {betType}");
                        continue;
                    }

                    gameName = tpGameIdListDetail.GameName;
                    gameLogo.Add("en", $"{bannerUrl}/{devicePath}/img/productimg/{tpGameIdListDetail.ImgTpName}/{tpGameIdListDetail.LobbyCategoryName}/{tpGameIdListDetail.ImgName}");
                    tpGameIdNo = tpGameIdListDetail.TpGameIdNo;
                }

                string venderLogoTpId = _specialVenderLogoTp.ContainsKey(tpGameIdNo) && _specialVenderLogoTp.ContainsValue(tpId) ? $"{tpId}-{tpGameIdNo}" : tpId.ToString();

                recentlyViewedInfo.Add(new RecentlyViewedInfo()
                {
                    TpId = tpId,
                    TpGameType = tpGameType,
                    CategoryId = categoryId,
                    GameId = gameId,
                    BetType = betType,
                    TpName = imgTpName,
                    GameName = gameName,
                    GameLogo = gameLogo,
                    TpLogo = $"{bannerUrl}/{devicePath}/img/venderlogo/{venderLogoTpId}.png",
                    TpGameIdNo = tpGameIdNo,
                    IsMaintain = isMaintain,
                });
            }

            return new RecentlyViewedResponse(imgUrl, bannerUrl, recentlyViewedInfo);
        }

        /// <summary>
        /// 1.先排除不在SlotGameList的provider
        /// 2.依照lobby的設定進行排序
        /// </summary>
        private List<GameProvider> FilterAndSortProviderList(List<GameProvider> providerList, Dictionary<int, int> slotGameProviderDict, List<LobbyCategorySequenceInfo> lobbyCategorySequence)
        {
            return providerList
                .Where(i => slotGameProviderDict.ContainsKey(i.TpId) || i.TpId == 22) // 針對mega888特別顯示
                .Select(pld =>
                {
                    // Tp組入排序欄位
                    pld.Sort = lobbyCategorySequence.FirstOrDefault(x => x.TpId == pld.TpId)?.Sort ?? 99999;
                    return pld;
                })
                .OrderBy(x => x.Sort)
                .ToList();
        }

        /// <summary>
        /// 排序SlotGame清單
        /// </summary>
        private List<SlotGameInfo> SortSlotGameList(List<SlotGameInfo> slotGameList, List<SlotGameTopRankingInfo> slotGameTopRanking)
        {
            // 遊戲組入排序欄位
            return slotGameList
               .Select(sgl =>
               {
                   // 不分TpId排序參數 (TpId = 0的設定資料)
                   sgl.Top = slotGameTopRanking.Find(x => x.NoneTpId == 0 && x.SlotGameId == sgl.SlotGameId)?.Sort ?? 99999;
                   // 指定TpId排序參數 (TpId > 0的設定資料)
                   sgl.Sort = slotGameTopRanking.Find(x => x.NoneTpId != 0 && x.SlotGameId == sgl.SlotGameId)?.Sort ?? 99999;
                   return sgl;
               })
               .OrderBy(x => x.Top)
               .ThenBy(x => x.Sort)
               .ToList();
        }

        /// <summary>
        /// Slot遊戲優先排序列
        /// </summary>
        private async Task<IEnumerable<SlotGameTopRankingInfo>> GetSlotGameTopRankingList()
        {
            return await _cacheHelper.TryGetOrCreateAsync($"GetSlotGameTopRankingList",
                TimeSpan.FromHours(2),
                async () => await _casinoService.GetSlotGameTopRankingList());
        }

        /// <summary>
        /// 依條件取得Slot遊戲優先排序 (SiteId、CurrencyId)
        /// </summary>
        private async Task<List<SlotGameTopRankingInfo>> GetSlotGameTopRanking(int siteId, int currencyId)
        {
            var list = await GetSlotGameTopRankingList();
            return list.Where(x => x.SiteId == siteId && x.CurrencyId == currencyId).ToList();
        }
    }
}