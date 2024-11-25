using MemberSite.Service.BLL.Interface;
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

        public HotGameBLL(IHotGameService hotGameService, ICacheHelper redisHelper, IServiceProvider provider)
        {
            this._hotGameService = hotGameService;
            this._redisHelper = redisHelper;
            var configBll = provider.GetRequiredService<ConfigBLL>();
            _siteCloudFrontImageInfo = configBll.GetConfigValue<SiteCloudFrontImageInfo>("SiteCloudFrontImageInfo");
            _slotGameBLL = provider.GetRequiredService<SlotGameBLL>();
            _gameBll = provider.GetRequiredService<GameBLL>();
            _serverInfoBll = provider.GetRequiredService<ServerInfoBll>();
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

        public async Task<Dictionary<string, IEnumerable<HotGameResponse>>> GetHotGames(int siteId, int currencyId, HotGameDeviceType deviceType, string langId, GetHotGameDataListRequest request)
        {
            var serverInfo = _serverInfoBll.GetServerInfo();
            var imgUrl = _siteCloudFrontImageInfo.OldImageUrl;
            var bannerUrl = _siteCloudFrontImageInfo.BannerImageUrl;
            var devicePath = deviceType == HotGameDeviceType.Pc ? "desktop" : "h5";
            var key = $"GetHotGamesV2-NewUI:{siteId}:{currencyId}";
            //var hotGames = await _redisHelper.TryGetOrCreateAsync(key, null,
            //    async () => await _hotGameService.GetHotGames(siteId, currencyId, request));

            var hotGames = await _hotGameService.GetHotGames(siteId, currencyId, request);

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
                            GameLogo = $"{r.TpNameKey}/{r.GameName?.ToLower().Replace(" ", "-")}-en",
                            TPNameKey = r.TpNameKey,
                            TpName = r.TpName,
                            TpLogo = GetTpLogo(r.TpId, r.GameId, r.TpGameType, r.TpCategory),
                            Sort = r.Sort
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
