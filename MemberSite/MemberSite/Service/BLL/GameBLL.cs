using System.Security.Claims;
using System.Web;
using Dapper;
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
        private readonly AuthorizeBLL _authorizeBll;
        private readonly MySqlClient _reportDbClient;
        private readonly CasinoService _casinoService;
        private readonly CustInfoBll _custInfoBll;
        private readonly SelLangBLL _selLangBll;
        private readonly ICacheHelper _cacheHelper;
        private readonly Dictionary<int, string> _openPathList;
        private readonly StatementService _statementService;
        private readonly GameService _gameService;
        private readonly ThirdPartyBll _thirdPartyBll;
        private readonly SiteCloudFrontImageInfo _siteCloudFrontImageInfo;
        private readonly Dictionary<string, string> _changeTpName;
        private readonly int _isStage;
        private readonly Tools _tools;

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
            _custInfoBll = provider.GetRequiredService<CustInfoBll>();
            _selLangBll = provider.GetRequiredService<SelLangBLL>();
            _thirdPartyBll = provider.GetRequiredService<ThirdPartyBll>();
            _authorizeBll = provider.GetRequiredService<AuthorizeBLL>();
            _casinoService = provider.GetRequiredService<CasinoService>();
            var configBll = provider.GetRequiredService<ConfigBLL>();
            _siteCloudFrontImageInfo = configBll.GetConfigValue<SiteCloudFrontImageInfo>("SiteCloudFrontImageInfo");
            _changeTpName = new Dictionary<string, string>()
            {
                {"END","ES" },
                {"EBet","WE" },
            };
            _isStage = configBll.GetConfigValueByEnvironment<int>("IsStage");
            _tools = provider.GetRequiredService<Tools>();
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

        public async Task<IEnumerable<GameProvider>> GetGameProviderByCategory(int siteId, int currencyId, TpCategory tpCategory, Dictionary<string, string> changeGameImgName)
        {
            return await _cacheHelper.TryGetOrCreateAsync($"GetGameProviderByCategory:{currencyId}:{siteId}:{tpCategory}",
                TimeSpan.FromMinutes(10),
                async () =>
                {
                    var providerDto = await _reportDbClient.QueryAsync<ProviderDto>("Service_Provider_Get", new DynamicParameters());
                    var providers = providerDto.Select(r => new GameProvider()
                    {
                        TpId = r.Id,
                        TpName = r.TpName,
                        DisplayText = r.DisplayName
                    }).ToList();
                    foreach (var provider in providers)
                    {
                        var tpName = changeGameImgName.TryGetValue(provider.TpName, out var changeName) ? changeName : provider.TpName;
                        provider.ImgName = $"{tpName}".ToLower();
                        provider.OpenPath = _openPathList.TryGetValue(provider.TpId, out var openPath) ? openPath : null;
                    }
                    return providers;
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

        public async Task<IEnumerable<LobbyHeaderResponse>> GetLobbyHeader(GetLobbyHeaderRequest req, ServerInfo serverInfo)
        {
            var deviceType = _tools.GetDeviceId(_tools.GetFromUrl());

            var key = $"GetLobbyHeader:{_isStage}:{deviceType}:{req.SiteId}:{req.CurrencyId}";
            var result = await _cacheHelper.TryGetOrCreateAsync(key,
                TimeSpan.FromMinutes(5),
                async () =>
                {
                    var result = await GetLobbyHeaderDetail(req, serverInfo, deviceType);
                    return result;
                });
            return result;
        }

        public async Task<LobbyGameResponse> GetLobbyGame(GetLobbyGameRequest req, ServerInfo serverInfo, ClaimsPrincipal user)
        {
            var deviceType = _tools.GetDeviceId(_tools.GetFromUrl());

            var key = $"GetLobbyGame:{_isStage}:{deviceType}:{req.SiteId}:{req.CurrencyId}:{req.LobbyCategoryIds}";
            var result = await _cacheHelper.TryGetOrCreateAsync(key,
                TimeSpan.FromMinutes(5),
                async () =>
                {
                    var result = await _gameService.GetLobbyGame(req);
                    var (siteId, currencyId, custId) = GetUserInfo(req.CurrencyId, user, serverInfo);
                    var resultVendor = result.GroupBy(test => test.TpId).Select(grp => grp.First());
                    var providerList = resultVendor.Select(item => new GameProvider
                    {
                        TpId = item.TpId,
                        TpName = item.TpName,
                        TpGameType = item.TpGameType,
                        DisplayText = item.DisplayText,
                        OpenPath = _openPathList.TryGetValue(item.TpId, out var openPath) ? openPath : null,
                        ImgName = $"{item.TpName}".ToLower()
                    });
                    var favoriteAndRecentlyPlayedTask = await GetFavoriteAndRecentlyPlayed(custId);
                    var lobbyGameResponse = new LobbyGameResponse
                    {
                        ProviderList = providerList.ToList()
                    };
                    lobbyGameResponse.SetLobbyCategory(result.ToList(), favoriteAndRecentlyPlayedTask.FavoriteList, favoriteAndRecentlyPlayedTask.RecentlyPlayedList);
                    return lobbyGameResponse;
                });
            return result;
        }

        private (int SiteId, int CurrencyId, int CustId) GetUserInfo(int? inputCurrencyId, ClaimsPrincipal user, ServerInfo serverInfo)
        {

            if (user?.Identity is { IsAuthenticated: true })
            {

                var authData = _authorizeBll.GetAuthData();
                if (authData != null && authData.CustId != 0)
                {
                    var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
                    return (custInfo.SiteId, custInfo.CurrencyId, authData.CustId);
                }
            }
            var currencyId = _selLangBll.GetPreferCurrencyId();
            if (inputCurrencyId.HasValue)
            {
                currencyId = inputCurrencyId.Value;
            }
            return (serverInfo.SiteId, currencyId, 0);

        }

        private async Task<FavoriteAndRecentlyPlayed> GetFavoriteAndRecentlyPlayed(int custId)
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

        private async Task<IEnumerable<LobbyHeaderResponse>> GetLobbyHeaderDetail(GetLobbyHeaderRequest req, ServerInfo serverInfo, DeviceId deviceType)
        {
            var statusList = new List<int> { 1 };
            if (_isStage == 1)
            {
                statusList.Add(3);
            }

            var resultData = await _gameService.GetLobbyHeader(req, statusList);

            // app的wrapper無法開啟PlayTech，後端寫死移除PlayTech的Casino
            if (deviceType == DeviceId.APP)
            {
                resultData = resultData.Where(i => !(i.TpId == 51 && i.LobbyCategoryId == 3)).ToList();
            }

            var sequence = new List<int>();
            var headerSequence = await _gameService.GetLobbyHeaderSequence(req);
            if (headerSequence is not null)
            {
                sequence = JsonConvert.DeserializeObject<List<int>>(headerSequence.LobbyCategoryList);
            }

            var imgDomain = _siteCloudFrontImageInfo.BannerImageUrl;

            var groupedData = resultData.GroupBy(item => item.LobbyCategoryId).ToList();
            var result = groupedData.Select(group =>
            {
                var response = new LobbyHeaderResponse
                {
                    LobbyCategoryId = group.Key,
                    Data = group.Select(item =>
                    {
                        item.TpName = _changeTpName.TryGetValue(item.TpName, out var changeName) ? changeName : item.TpName;
                        var responseDetail = new LobbyHeaderDetail
                        {
                            LobbyCategory = item.LobbyCategory,
                            TpId = item.TpId,
                            TpName = item.TpName,
                            GameName = item.GameName,
                            TpGameIdNo = item.TpGameIdNo,
                            GameId = item.GameId,
                            TpGameType = item.TpGameType,
                            BetType = item.BetType,
                            IconType = item.IconType,
                            IconStartTime = item.IconStartTime,
                            IconEndTime = item.IconEndTime,
                            CurrencyGameName = string.IsNullOrEmpty(item.CurrencyGameName) ? null : JsonConvert.DeserializeObject<Dictionary<string, string>>(item.CurrencyGameName),
                            RwdHeaderPicUrl = JsonConvert.SerializeObject(new List<string> { $"{imgDomain}/img/Header/{item.TpName}/{item.LobbyCategory}/{item.ImgGameName.Replace(":", "")}.webp" }),
                            RwdLobbyPicUrl = JsonConvert.SerializeObject(new List<string> { $"{imgDomain}/img/LobbyHeader/{item.TpName}/{item.LobbyCategory}/{item.ImgGameName.Replace(":", "")}.webp" }),
                            PcHeaderPicUrl = JsonConvert.SerializeObject(new List<string> { $"{imgDomain}/desktop/img/Header/{item.TpName}/{item.LobbyCategory}/{item.ImgGameName.Replace(":", "")}.webp" }),
                            PcLobbyPicUrl = JsonConvert.SerializeObject(new List<string> { $"{imgDomain}/desktop/img/LobbyHeader/{item.TpName}/{item.LobbyCategory}/{item.ImgGameName.Replace(":", "")}.webp" }),
                            MobileHeaderPicUrl = JsonConvert.SerializeObject(new List<string> { $"{imgDomain}/h5/img/Header/{item.TpName}/{item.LobbyCategory}/{item.ImgGameName.Replace(":", "")}.webp" }),
                            MobileLobbyPicUrl = JsonConvert.SerializeObject(new List<string> { $"{imgDomain}/h5/img/LobbyHeader/{item.TpName}/{item.LobbyCategory}/{item.ImgGameName.Replace(":", "")}.webp" }),
                            Sort = item.Sort
                        };
                        return responseDetail;
                    }).OrderBy(x => x.Sort) // 排序遊戲清單
                };
                return response;
            }).OrderBy(r => sequence.IndexOf(r.LobbyCategoryId));  // 排序header Category

            //Hard code menu
            var menus = result.ToList();
            menus.AddRange(new List<LobbyHeaderResponse>
            {
                new() { LobbyCategoryId = (int)LobbyCategory.Casino, Data = new List<LobbyHeaderDetail>() },
                new() { LobbyCategoryId = (int)LobbyCategory.Slots, Data = new List<LobbyHeaderDetail>() },
                new() { LobbyCategoryId = (int)LobbyCategory.Games, Data = new List<LobbyHeaderDetail>() },
                new() { LobbyCategoryId = (int)LobbyCategory.Fishing, Data = new List<LobbyHeaderDetail>() }
            });

            return menus;
        }
    }
}