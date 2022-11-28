using MemberSite.Service.Library;
using MemberSite.Service.Model;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using MemberSite.Service.DAL.Interface;

namespace MemberSite.Service.BLL
{
    public class BannerBLL
    {
        private readonly IBannerService bannerService;
        private readonly ServerInfoBll serverInfoBLL;
        private readonly PromotionBll promotionBLL;
        private readonly IWebHostEnvironment environment;
        private readonly IOptions<Dictionary<string, SiteConfig>> siteConfig;
        private readonly TpGameTypeHelper tpGameTypeHelper;
        private readonly ICacheHelper _redisHelper;

        public BannerBLL(IServiceProvider provider)
        {
            bannerService = provider.GetRequiredService<IBannerService>();
            serverInfoBLL = provider.GetRequiredService<ServerInfoBll>();
            environment = provider.GetRequiredService<IWebHostEnvironment>();
            promotionBLL = provider.GetRequiredService<PromotionBll>();
            siteConfig = provider.GetRequiredService<IOptions<Dictionary<string, SiteConfig>>>();
            tpGameTypeHelper = provider.GetRequiredService<TpGameTypeHelper>();
            _redisHelper = provider.GetRequiredService<ICacheHelper>();
        }

        public IEnumerable<BannerSettingItem> GetBanner(int perferCurr, string FolderName)
        {
            var serverInfo = serverInfoBLL.GetServerInfo();
            var dataPath = Path.Combine(environment.WebRootPath, $"datas/{serverInfo.SiteName}_bannerSetting.json");
            var _bannerSetting = new BannerSetting();
            if (File.Exists(dataPath))
            {
                _bannerSetting = JsonConvert.DeserializeObject<BannerSetting>(File.ReadAllText(dataPath));
            }

            if (FolderName == "Promotion")
            {
                return _bannerSetting.Promotion.Where(x => x.currencyId == null || x.currencyId.Any(y => y == perferCurr));
            }
            else if (FolderName == "Casino")
            {
                return _bannerSetting.Casino.Where(x => x.currencyId == null || x.currencyId.Any(y => y == perferCurr));
            }
            else
            {
                return null;
            }
        }

        public IEnumerable<BannerV2> GetBannerV2(int SiteId, int BannerType, int ShowStatus, int CurrencyId, string LangId)
        {
            var banner = new List<BannerV2>();
            var bannerlist = bannerService.GetBannerV2(SiteId, BannerType, ShowStatus, CurrencyId);
            foreach (var bannerdetail in bannerlist)
            {
                if (bannerdetail.Details != null)
                {
                    var details = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(bannerdetail.Details);
                    if (details.ContainsKey(LangId))
                    {
                        var settings = details[LangId];
                        banner.Add(new BannerV2()
                        {
                            BannerName = bannerdetail.BannerName,
                            BannerType = bannerdetail.BannerType,
                            ActionMode = bannerdetail.ActionMode,
                            BonusCode = bannerdetail.BonusCode,
                            MatchId = bannerdetail.MatchId,
                            TpId = bannerdetail.TpId,
                            GameId = bannerdetail.GameId,
                            UrlRouter = bannerdetail.UrlRouter,
                            BannerLink = settings["bannerLink"],
                            BannerPCUrl = settings["bannerPCUrl"],
                            BannerMobileUrl = settings["bannerMobileUrl"]
                        });
                    }
                }
            }

            return banner;
        }

        public IEnumerable<BannerV3> GetBannerV3(int siteId, int bannerType, int showStatus, int currencyId, string langId, ServerInfo serverInfo)
        {
            var banner = new List<BannerV3>();
            var bannerList = bannerService.GetBannerV3(siteId, bannerType, showStatus, currencyId);
            foreach (var bannerDetail in bannerList)
            {
                if (bannerDetail.Details != null && bannerDetail.BannerType != 2)
                {
                    var details = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(bannerDetail.Details);
                    if (!details.ContainsKey(langId)) continue;
                    var settings = details[langId];
                    banner.Add(new BannerV3()
                    {
                        BannerName = bannerDetail.BannerName,
                        BannerType = bannerDetail.BannerType,
                        ActionMode = bannerDetail.ActionMode,
                        BonusCode = bannerDetail.BonusCode,
                        TpId = bannerDetail.TpId,
                        GameId = bannerDetail.GameId,
                        UrlRouter = bannerDetail.UrlRouter,
                        BannerLink = settings["bannerLink"],
                        BannerPCUrl = settings["bannerPCUrl"],
                        BannerMobileUrl = settings["bannerMobileUrl"],
                        tpGameType = tpGameTypeHelper.GetTpGameType(bannerDetail, currencyId),
                    });
                }
                else if (IsHotEventAndDataNotNull(bannerDetail))
                {
                    var sportLeaguesId = JsonConvert.DeserializeObject<Dictionary<string, string>>(bannerDetail.SportLeaguesId);
                    var sportTeamsHome = JsonConvert.DeserializeObject<Dictionary<string, string>>(bannerDetail.SportTeamsHome);
                    var sportTeamsAway = JsonConvert.DeserializeObject<Dictionary<string, string>>(bannerDetail.SportTeamsAway);

                    if (HotEventLangFullExist(langId, sportLeaguesId, sportTeamsHome, sportTeamsAway))
                    {
                        ComposeHotEventData(currencyId, langId, serverInfo, bannerDetail, sportLeaguesId, sportTeamsHome, sportTeamsAway, banner);
                    }
                    else if (langId != "en") // 非 en 語系沒資料，就抓 en 語系當預設語系
                    {
                        const string langIdEn = "en";
                        ComposeHotEventData(currencyId, langIdEn, serverInfo, bannerDetail, sportLeaguesId, sportTeamsHome, sportTeamsAway, banner);
                    }
                }
            }

            if (bannerType == 2)
            {
                banner = banner.OrderBy(x => x.SportBattleDateTime).ToList();
            }

            return banner;
        }

        #region private method for GetBannerV3 To HotEvent

        private void ComposeHotEventData(int currencyId, string langId, ServerInfo serverInfo,
            BannerV3 bannerDetail,
            Dictionary<string, string> sportLeaguesId,
            Dictionary<string, string> sportTeamsHome,
            Dictionary<string, string> sportTeamsAway,
            List<BannerV3> banner)
        {

            var logoPic = string.Empty;
            var backgroundPic = string.Empty;

            if (NeedGetLogoAndBackGroundImage(serverInfo, bannerDetail))
            {
                logoPic =
                    $"{siteConfig.Value[serverInfo.SiteName].ImageURL}/Sports/{sportLeaguesId["en"]}/{serverInfo.SiteName}/Logo.png";
                backgroundPic =
                    $"{siteConfig.Value[serverInfo.SiteName].ImageURL}/Sports/{sportLeaguesId["en"]}/{serverInfo.SiteName}/Background.jpg";
            }

            var leagues = sportLeaguesId[langId];
            var teamsHome = sportTeamsHome[langId];
            var teamsHomePic =
                $"{siteConfig.Value[serverInfo.SiteName].ImageURL}/Sports/{sportLeaguesId["en"]}/{sportTeamsHome["en"]}.png";
            var teamsAway = sportTeamsAway[langId];
            var teamsAwayPic =
                $"{siteConfig.Value[serverInfo.SiteName].ImageURL}/Sports/{sportLeaguesId["en"]}/{sportTeamsAway["en"]}.png";
            banner.Add(new BannerV3()
            {
                BannerName = bannerDetail.BannerName,
                BannerType = bannerDetail.BannerType,
                ActionMode = bannerDetail.ActionMode,
                BonusCode = bannerDetail.BonusCode,
                MatchId = bannerDetail.MatchId,
                TpId = bannerDetail.TpId,
                GameId = bannerDetail.GameId,
                UrlRouter = bannerDetail.UrlRouter,
                SportBattleDateTime = bannerDetail.SportBattleDateTime,
                LeaguesId = bannerDetail.LeaguesId,
                SportLeaguesId = leagues,
                SportTeamsHome = teamsHome,
                SportTeamsHomePic = teamsHomePic,
                SportTeamsAway = teamsAway,
                SportTeamsAwayPic = teamsAwayPic,
                HomeOdds = bannerDetail.HomeOdds,
                AwayOdds = bannerDetail.AwayOdds,
                Handicap = bannerDetail.Handicap,
                Logo = logoPic,
                BackgroundPic = backgroundPic,
                tpGameType = tpGameTypeHelper.GetTpGameType(bannerDetail, currencyId)
            });
        }

        private static bool NeedGetLogoAndBackGroundImage(ServerInfo serverInfo, BannerV3 bannerDetail)
        {
            return serverInfo.SiteId == 1 && bannerDetail.LeaguesId != 6;
        }

        private static bool IsHotEventAndDataNotNull(BannerV3 bannerDetail)
        {
            return bannerDetail.SportLeaguesId != null &&
                   bannerDetail.SportTeamsHome != null &&
                   bannerDetail.SportTeamsAway != null &&
                   bannerDetail.BannerType == 2;
        }

        private static bool HotEventLangFullExist(string langId,
            Dictionary<string, string> sportLeaguesId,
            Dictionary<string, string> sportTeamsHome,
            Dictionary<string, string> sportTeamsAway)
        {
            return sportLeaguesId.ContainsKey(langId) &&
                   sportTeamsHome.ContainsKey(langId) &&
                   sportTeamsAway.ContainsKey(langId);
        }

        #endregion

        public async Task<IEnumerable<HotGame>> GetHotGames(int siteId, int currencyId, int deviceType)
        {
            var key = $"GetHotGames:{siteId}:{currencyId}";
            var hotGames = await _redisHelper.TryGetOrCreateAsync(key, null,
                async () => await bannerService.GetHotGames(siteId, currencyId));
            return hotGames
                .Select(a => new HotGame()
                {
                    TpId = a.TpId,
                    GameId = a.GameId,
                    TpGameType = a.TpGameType,
                    TpCategory = a.TpCategory,
                    GameName = a.GameName,
                    GameLogo = DeserializeGameLogo(a.GameLogo, deviceType),
                    TpName = a.TpName,
                    TpLogo = a.TpLogo
                });
        }

        private string DeserializeGameLogo(string jsonGameLogo, int device)
        {
            var gameLogo = JsonConvert.DeserializeObject<HotGameLogoUrl>(jsonGameLogo);
            return device switch
            {
                1 => gameLogo.HotGamePcUrl,
                2 => gameLogo.HotGameMobileUrl,
                _ => ""
            };
        }
    }
}
