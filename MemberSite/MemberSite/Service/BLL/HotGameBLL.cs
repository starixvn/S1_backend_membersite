using System.Security.Claims;
using MemberSite.Service.BLL.Interface;
using MemberSite.Service.DAL;
using MemberSite.Service.DAL.Interface;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.BLL
{
    public class HotGameBLL : IHotGameBLL
    {
        private readonly IHotGameService _hotGameService;
        private readonly ICacheHelper _redisHelper;
        private readonly SiteCloudFrontImageInfo _siteCloudFrontImageInfo;
        private readonly SlotGameBLL _slotGameBLL;
        private readonly GameBLL _gameBll;
        private readonly ServerInfoBll _serverInfoBll;
        private readonly Dictionary<int, int> _specialVenderLogoTp;
        private readonly Dictionary<string, string> _changeGameImgName;
        private readonly CasinoService _casinoService;
        private readonly AuthorizeBLL _authorizeBll;
        private readonly CustInfoBll _custInfoBll;
        private readonly SelLangBLL _selLangBll;

        public HotGameBLL(IHotGameService hotGameService, ICacheHelper redisHelper, IServiceProvider provider)
        {
            this._hotGameService = hotGameService;
            this._redisHelper = redisHelper;
            var configBll = provider.GetRequiredService<ConfigBLL>();
            _siteCloudFrontImageInfo = configBll.GetConfigValue<SiteCloudFrontImageInfo>("SiteCloudFrontImageInfo");
            _slotGameBLL = provider.GetRequiredService<SlotGameBLL>();
            _gameBll = provider.GetRequiredService<GameBLL>();
            _serverInfoBll = provider.GetRequiredService<ServerInfoBll>();
            _casinoService = provider.GetRequiredService<CasinoService>();
            _authorizeBll = provider.GetRequiredService<AuthorizeBLL>();
            _custInfoBll = provider.GetRequiredService<CustInfoBll>();
            _selLangBll = provider.GetRequiredService<SelLangBLL>();
            _specialVenderLogoTp = new Dictionary<int, int>()
            {
                { 67, 30 },
                { 109, 52},
                { 110, 52},
            };
            _changeGameImgName = new Dictionary<string, string>()
            {
              {"Iconic","AT" },
              {"Kiss918","918Kiss" },
              {"END","ES" },
              {"EBet","WE" },
            };
        }

        public async Task<IEnumerable<ProviderResponse>> GetProvider()
        {
            var providers = await _hotGameService.GetProvider();
            return providers.Select(r => new ProviderResponse()
            {
                Id = r.Id,
                TpName = r.TpName,
                DisplayName = r.DisplayName
            });
        }

        private static string OrgFileNameFilter(string fileName)
        {
            return fileName.Replace(" ", "-")
                                       .Replace("&", "-")
                                       .Replace(":", "-")
                                       .Replace("#3258", "")
                                       .Replace("'", "")
                                       .Replace(":", "")
                                       .Replace("’", "")
                                       .Replace(".", "")
                                       .Replace("™", "")
                                       .Replace("---", "-")
                                       .Replace("--", "-")
                                       .Replace("\u00A0", "-")
                                       .Replace("\u200B", "")
                                       .Replace(",", "")
                                       .Replace("?", "")
                                       .Replace("!", "")
                                       .ToLower();
        }

        /// <summary>
        /// 根據登入狀態回傳SiteId、CurrencyId、CustId
        /// </summary>
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

        public async Task<Dictionary<string, IEnumerable<HotGameResponse>>> GetHotGames(int siteId, int currencyId, HotGameDeviceType deviceType, string langId, GetHotGameDataListRequest request, ClaimsPrincipal user)
        {
            var serverInfo = _serverInfoBll.GetServerInfo();
            var (defsiteId, defcurrencyId, custId) = GetUserInfo(request.CurrencyId, user, serverInfo);
            var imgUrl = _siteCloudFrontImageInfo.OldImageUrl;
            var bannerUrl = _siteCloudFrontImageInfo.BannerImageUrl;
            var devicePath = deviceType == HotGameDeviceType.Pc ? "desktop" : "h5";
            var hotGames = await _hotGameService.GetHotGames(siteId, currencyId, request);
            var providers = await _hotGameService.GetProvider();
            var disableGame = await _casinoService.GetTpDisableUserByCustIdAsync(custId);

            if (custId > 0)
            {
                hotGames = hotGames.Where(x => !disableGame.Any(y => y.GameList.Exists(z => z.Equals(x.GameId)) && y.TpId == x.TpId));
            }

            // 取出Slot的遊戲資訊
            var slotGameList = await _slotGameBLL.GetSlotGameList(siteId, currencyId, (int)deviceType);
            // 取出其他大廳的遊戲資訊
            var tpGameIdList = await _gameBll.GetTpGameIdList();

            var result = hotGames.GroupBy(r => r.TopCategory.ToString())
                .ToDictionary(item => item.Key,
                    item => item.OrderBy(x => x.Sort)
                        .Select(r => new HotGameResponse()
                        {
                            TpId = r.TpId,
                            GameId = r.GameId,
                            TpGameType = r.TpGameType,
                            TpCategory = r.TpCategory,
                            GameName = r.GameName,
                            GameLogo = $"{r.TpNameKey}/{OrgFileNameFilter(r.GameName?.ToLower())}-en",
                            TpNameKey = r.TpNameKey,
                            TpName = r.TpName,
                            TpLogo = GetTpLogo(r.TpId, r.GameId, r.TpGameType, r.TpCategory),
                            Sort = r.Sort
                        }));

            result.Add("Provider", providers.Select(r => new HotGameResponse()
            {
                TpId = r.Id,
                TpNameKey = r.TpName,
                TpName = r.DisplayName,
                TpLogo = r.DisplayName?.ToLower().Replace("\u00A0", "-").Replace(" ", "-")
            }));

            return result;

            string GetGameLogo(int tpId, string gameId, int tpGameType, int tpCategory)
            {
                string gameLogo = null;

                if (tpCategory == (int)TpCategory.Casino)
                {
                    var gameSlotDetail = slotGameList.FirstOrDefault(i => i.TpId == tpId && (i.GameId == gameId || i.AppId == gameId));
                    if (gameSlotDetail == null)
                    {
                        return gameLogo;
                    }

                    var imgTpName = tpGameIdList.FirstOrDefault(r => r.TpId == tpId)?.ImgTpName;
                    imgTpName = _changeGameImgName.TryGetValue(imgTpName, out var changeName) ? changeName : imgTpName;
                    //沒有符合的langId就用en
                    var imgName = gameSlotDetail.ImgName.ContainsKey(langId) ? gameSlotDetail.ImgName[langId] : gameSlotDetail.ImgName["en"];
                    gameLogo = $"{imgUrl}/{imgTpName}/{imgName}";
                }
                else
                {
                    var tpGameIdListDetail = tpGameIdList.FirstOrDefault(i => i.TpId == tpId && i.TpGameType == tpGameType && i.GameId == gameId && i.BetType == "");
                    if (tpGameIdListDetail == null)
                    {
                        return gameLogo;
                    }
                    var imgTpName = _changeGameImgName.TryGetValue(tpGameIdListDetail.ImgTpName, out var changeName) ? changeName : tpGameIdListDetail.ImgTpName;
                    gameLogo = $"{bannerUrl}/{devicePath}/img/productimg/{imgTpName}/{tpGameIdListDetail.LobbyCategoryName}/{tpGameIdListDetail.ImgName}";
                }

                return gameLogo;
            }

            string GetTpLogo(int tpId, string gameId, int tpGameType, int tpCategory)
            {
                string tpLogo = null;
                string venderLogoTpId = null;
                if (tpCategory == (int)TpCategory.Casino)
                {
                    var tpGameIdListDetail = tpGameIdList.FirstOrDefault(i => i.TpId == tpId && i.TpGameType == tpGameType);
                    if (tpGameIdListDetail == null)
                    {
                        return tpLogo;
                    }
                    var tpGameIdNo = (int)(tpGameIdListDetail?.TpGameIdNo);
                    venderLogoTpId = _specialVenderLogoTp.ContainsKey(tpGameIdNo) && _specialVenderLogoTp.ContainsValue(tpId) ? $"{tpId}-{tpGameIdNo}" : tpId.ToString();
                }
                else
                {
                    var tpGameIdListDetail = tpGameIdList.FirstOrDefault(i => i.TpId == tpId && i.TpGameType == tpGameType && i.GameId == gameId && i.BetType == "");
                    if (tpGameIdListDetail == null)
                    {
                        return tpLogo;
                    }
                    var tpGameIdNo = tpGameIdListDetail.TpGameIdNo;
                    venderLogoTpId = _specialVenderLogoTp.ContainsKey(tpGameIdNo) && _specialVenderLogoTp.ContainsValue(tpId) ? $"{tpId}-{tpGameIdNo}" : tpId.ToString();
                }

                if (venderLogoTpId != null)
                {
                    tpLogo = $"{bannerUrl}/{devicePath}/img/venderlogo/{venderLogoTpId}.png";
                }

                return tpLogo;
            }
        }
    }
}
