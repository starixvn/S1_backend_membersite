using MemberSite.Service.BLL.Interface;
using MemberSite.Service.DAL.Interface;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Drawing.Drawing2D;

namespace MemberSite.Service.BLL
{
    public class HotGameBLL : IHotGameBLL
    {
        private readonly IHotGameService _hotGameService;
        private readonly ICacheHelper _redisHelper;
        private readonly IOptions<Dictionary<string, SiteConfig>> _siteConfig;
        private readonly SlotGameBLL _slotGameBLL;
        private readonly GameBLL _gameBll;
        private readonly ServerInfoBll _serverInfoBll;
        private readonly Dictionary<int, int> _specialVenderLogoTp;

        public HotGameBLL(IHotGameService hotGameService, ICacheHelper redisHelper, IServiceProvider provider)
        {
            this._hotGameService = hotGameService;
            this._redisHelper = redisHelper;
            _siteConfig = provider.GetRequiredService<IOptions<Dictionary<string, SiteConfig>>>();
            _slotGameBLL = provider.GetRequiredService<SlotGameBLL>();
            _gameBll = provider.GetRequiredService<GameBLL>();
            _serverInfoBll = provider.GetRequiredService<ServerInfoBll>();
            _specialVenderLogoTp = new Dictionary<int, int>()
            {
                { 67, 30 },
                { 109, 52},
                { 110, 52},
            };
        }

        public async Task<Dictionary<string, IEnumerable<HotGameResponse>>> GetHotGames(int siteId, int currencyId, HotGameDeviceType deviceType, string langId)
        {
            var serverInfo = _serverInfoBll.GetServerInfo();
            var imgUrl = _siteConfig.Value[serverInfo.SiteName].OldImageUrl;
            var bannerUrl = _siteConfig.Value[serverInfo.SiteName].BannerImageUrl;
            var devicePath = deviceType == HotGameDeviceType.Pc ? "desktop" : "h5";
            var key = $"GetHotGamesV2:{siteId}:{currencyId}";
            var hotGames = await _redisHelper.TryGetOrCreateAsync(key, null,
                async () => await _hotGameService.GetHotGames(siteId, currencyId));
            //hotGames = hotGames.Where(x => x.TpId == 25);
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
                            GameLogo = GetGameLogo(r.TpId, r.GameId, r.TpGameType, r.TpCategory),
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

                    gameLogo = $"{bannerUrl}/{devicePath}/img/productimg/{tpGameIdListDetail.ImgTpName}/{tpGameIdListDetail.LobbyCategoryName}/{tpGameIdListDetail.ImgName}";
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
