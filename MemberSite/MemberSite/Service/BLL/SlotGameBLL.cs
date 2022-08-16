using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using MemberSite.Service.Model.Game;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Security.Claims;

namespace MemberSite.Service.BLL
{
    public class SlotGameBLL
    {
        private readonly ILogger<SlotGameBLL> _logger;
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;
        private readonly ICacheHelper _cacheHelper;
        private readonly IOptions<Dictionary<string, SiteConfig>> _siteConfig;
        private readonly Dictionary<string, string> _changeDisplayName;


        private readonly AuthorizeBLL _authorizeBll;
        private readonly ServerInfoBll _serverInfoBll;
        private readonly CustInfoBll _custInfoBll;
        private readonly GameBLL _gameBll;
        private readonly SelLangBLL _selLangBll;

        private readonly CasinoService _casinoService;

        public SlotGameBLL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<SlotGameBLL>>();
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
            _cacheHelper = provider.GetRequiredService<ICacheHelper>();
            _siteConfig = provider.GetRequiredService<IOptions<Dictionary<string, SiteConfig>>>();
            _changeDisplayName = new Dictionary<string, string>()
            {
              {"Iconic","AT" },
            };

            _authorizeBll = provider.GetRequiredService<AuthorizeBLL>();
            _serverInfoBll = provider.GetRequiredService<ServerInfoBll>();
            _custInfoBll = provider.GetRequiredService<CustInfoBll>();
            _gameBll = provider.GetRequiredService<GameBLL>();
            _selLangBll = provider.GetRequiredService<SelLangBLL>();

            _casinoService = provider.GetRequiredService<CasinoService>();
        }

        public async Task<GetSlotGamePageResponse> GetSlotGamePage(GetSlotGamePageRequest request, ClaimsPrincipal user)
        {
            var serverInfo = _serverInfoBll.GetServerInfo();
            var (siteId, currencyId, custId) = GetUserInfo(user, serverInfo);
            var imgUrl = _siteConfig.Value[serverInfo.SiteName].ImageURL;

            var providerList = GetProviderList(siteId, currencyId, TpCategory.Slots);
            var slotGameList = GetSlotGameList(siteId, currencyId, request.DeviceType);

            // 未登入狀態直接回傳
            if (custId == 0)
            {
                return new GetSlotGamePageResponse(imgUrl, await providerList, await slotGameList);
            }

            //取得User Slot Game相關資料         
            var disableGame = GetDisableGameList(custId);
            var favoriteAndRecentlyPlayed = await GetFavoriteAndRecentlyPlayed(custId);

            return new GetSlotGamePageResponse(
                imgUrl: imgUrl, 
                providerList: await providerList,
                slotGameList: await slotGameList,
                favorite: favoriteAndRecentlyPlayed.FavoriteList,
                recentlyPlayed: favoriteAndRecentlyPlayed.RecentlyPlayedList,
                disableGame: await disableGame);
        }

        public async Task<List<SlotGameInfo>> GetSlotGameList(int siteId, int currencyId, int deviceType)
        {
            var result = await _cacheHelper.TryGetOrCreateAsync($"GetSlotGameList:{currencyId}:{siteId}:{deviceType}",
                TimeSpan.FromMinutes(10),
                async () => 
                {
                    var result = await _reportDbClient.QueryAsync<SlotGameInfo>("Service_SlotGame_Get", new
                    {
                        CurrencyId = currencyId,
                        SiteId = siteId,
                        DeviceType = deviceType,
                    });
                    return result;
                });
            return result.ToList();
        }

        /// <summary>
        /// 透過共用方法取回Slot Game Provider
        /// </summary>
        private async Task<List<SlotGameProvider>> GetProviderList(int siteId, int currencyId, TpCategory tpCategory)
        {
            var providerList = await _gameBll.GetGameProviderByCategory(siteId, currencyId, tpCategory);
            return providerList
                .Select(i => new SlotGameProvider(i, _changeDisplayName.TryGetValue(i.TpName, out var changeName) ? changeName : i.TpName))
                .ToList();
        }

        /// <summary>
        /// 根據登入狀態回傳SiteId與CurrencyId
        /// </summary>
        private (int SiteId, int CurrencyId, int CustId) GetUserInfo(ClaimsPrincipal user, ServerInfo serverInfo)
        {
            if (user.Identity.IsAuthenticated)
            {
                var authData = _authorizeBll.GetAuthData();
                var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
                return (custInfo.SiteId, custInfo.CurrencyId, authData.CustId);
            }
            else
            {

                return (serverInfo.SiteId, _selLangBll.GetPreferCurrencyId(), 0);
            }
        }

        public async Task<IEnumerable<DisableTpGame>> GetDisableGameList(int custId)
        {
            var result = await _cacheHelper.TryGetOrCreateAsync($"GetDisableGameList:{custId}",
                 TimeSpan.FromMinutes(10),
                 async () =>
                 {
                     return await _casinoService.GetTpDisableUserByCustIdAsync(custId);
                 });
            return result;
        }

        public async Task<FavoriteAndRecentlyPlayed> GetFavoriteAndRecentlyPlayed(int custId)
        {         
            var result = await _cacheHelper.TryGetOrCreateAsync($"GetFavoriteAndRecentlyPlayed:{custId}",
                 TimeSpan.FromMinutes(10),
                 async () =>
                 {
                     var result = new FavoriteAndRecentlyPlayed();
                     var data = await _casinoService.GetFavoriteGamesAsync(custId);
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
                 });
            return result;
        }      
    }
}