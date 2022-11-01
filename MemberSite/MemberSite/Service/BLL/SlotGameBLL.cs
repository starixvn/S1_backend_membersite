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
        private readonly MySqlClient _reportDbClient;
        private readonly ICacheHelper _cacheHelper;
        private readonly IOptions<Dictionary<string, SiteConfig>> _siteConfig;

        private readonly AuthorizeBLL _authorizeBll;
        private readonly ServerInfoBll _serverInfoBll;
        private readonly CustInfoBll _custInfoBll;
        private readonly GameBLL _gameBll;
        private readonly SelLangBLL _selLangBll;
       
        private readonly CasinoService _casinoService;
        private readonly IConfiguration _configuration;

        private readonly List<int> _excludeTp;
        private readonly Dictionary<string, string> _changeGameImgName;

        public SlotGameBLL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<SlotGameBLL>>();
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
            _cacheHelper = provider.GetRequiredService<ICacheHelper>();
            _siteConfig = provider.GetRequiredService<IOptions<Dictionary<string, SiteConfig>>>();
            _authorizeBll = provider.GetRequiredService<AuthorizeBLL>();
            _serverInfoBll = provider.GetRequiredService<ServerInfoBll>();
            _custInfoBll = provider.GetRequiredService<CustInfoBll>();
            _gameBll = provider.GetRequiredService<GameBLL>();
            _selLangBll = provider.GetRequiredService<SelLangBLL>();
            _casinoService = provider.GetRequiredService<CasinoService>();
            _configuration = provider.GetRequiredService<IConfiguration>();
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
            };
        }

        public async Task<GetSlotGamePageResponse> GetSlotGamePage(GetSlotGamePageRequest request, ClaimsPrincipal user)
        {
            var serverInfo = _serverInfoBll.GetServerInfo();
            var (siteId, currencyId, custId) = GetUserInfo(request.CurrencyId, user, serverInfo);
            var imgUrl = _siteConfig.Value[serverInfo.SiteName].ImageURL;

            var providerList = await GetSlotGameProviderList(siteId, currencyId, TpCategory.Casino);
            var slotGameList = await GetSlotGameList(siteId, currencyId, request.DeviceType);

            //排除不在SlotGameList的provider
            var slotGameProviderList = slotGameList.GroupBy(i => i.TpId).ToDictionary(i => i.Key,i => i.Key);
            providerList = providerList.Where(
                i => slotGameProviderList.ContainsKey(i.TpId) ||
                i.TpId == 22 // 針對mega888特別顯示
            ).ToList();
            
            // 未登入狀態直接回傳
            if (custId == 0)
            {
                return new GetSlotGamePageResponse(imgUrl, providerList, slotGameList);
            }

            //取得User Slot Game相關資料         
            var disableGame = await GetDisableGameList(custId);
            var favoriteAndRecentlyPlayed = await GetFavoriteAndRecentlyPlayed(custId);

            return new GetSlotGamePageResponse(
                imgUrl: imgUrl, 
                providerList: providerList,
                slotGameList: slotGameList,
                favorite: favoriteAndRecentlyPlayed.FavoriteList,
                recentlyPlayed: favoriteAndRecentlyPlayed.RecentlyPlayedList,
                disableGame: disableGame);
        }

        public async Task<List<SlotGameInfo>> GetSlotGameList(int siteId, int currencyId, int deviceType)
        {
            var statusList = new List<int> { 1 };
            var IsStage = _configuration.GetValue<int>("IsStage");
            if (IsStage == 1)
            {
                statusList.Add(3);
            }

            var key = $"GetSlotGameList:{IsStage}:{currencyId}:{siteId}:{deviceType}";
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
        public async Task<List<GameProvider>> GetSlotGameProviderList(int siteId, int currencyId, TpCategory tpCategory)
        {
            var providerList = await _gameBll.GetGameProviderByCategory(siteId, currencyId, tpCategory);
            var result = providerList.Where(i => !_excludeTp.Contains(i.TpId)).ToList();
            foreach (var provider in result)
            {
                provider.GameImgDisplayText = _changeGameImgName.TryGetValue(provider.TpName, out var changeName) ? changeName : provider.TpName;
            }
            return result;
        }

        /// <summary>
        /// 根據登入狀態回傳SiteId與CurrencyId
        /// </summary>
        private (int SiteId, int CurrencyId, int CustId) GetUserInfo(int? inputCurrencyId, ClaimsPrincipal user, ServerInfo serverInfo)
        {
            if (user.Identity.IsAuthenticated)
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

            if (data == null )
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
    }
}