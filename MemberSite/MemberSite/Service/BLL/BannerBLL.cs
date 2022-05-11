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

        public IEnumerable<BannerSettingItem> GetBannerFromDB(int CurrencyId, string langId)
        {
            var serverInfo = serverInfoBLL.GetServerInfo();
            var promotionList = promotionBLL.GetPromotionList(serverInfo.SiteId)?.Where(p => p.CurrencyId == CurrencyId).OrderBy(x => x.Sort);
            var bannerFromDB = new List<BannerSettingItem>();
            foreach (var promo in promotionList)
            {
                if (promo.Details != null)
                {
                    var details = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(promo.Details);
                    Dictionary<string, string> settings = null;
                    if (details.TryGetValue(langId, out settings))
                    {
                        var bannerUrl = settings["bannerUrl"];
                        var hideInHome = false;
                        if (settings.TryGetValue("hideInHome", out var strHideInHome))
                        {
                            hideInHome = string.Equals(strHideInHome, "TRUE", StringComparison.OrdinalIgnoreCase);
                        };

                        if (serverInfo.SiteId == 2)
                        {
                            bannerFromDB.Add(new BannerSettingItem() { actionLink = $"promotion/promotionInfo?BonusCode={promo.BonusCode}", imgURL = bannerUrl, fromDB = true, hideInHome = hideInHome });
                        }
                        else
                        {
                            bannerFromDB.Add(new BannerSettingItem() { actionLink = $"Promotion/Bonus?BonusCode={promo.BonusCode}", imgURL = bannerUrl, fromDB = true, hideInHome = hideInHome });
                        }
                    }
                }
            }

            return bannerFromDB;
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

        public IEnumerable<BannerV3> GetBannerV3(int SiteId, int BannerType, int ShowStatus, int CurrencyId, string LangId, ServerInfo serverInfo)
        {
            var banner = new List<BannerV3>();
            var bannerlist = bannerService.GetBannerV3(SiteId, BannerType, ShowStatus, CurrencyId);
            foreach (var bannerdetail in bannerlist)
            {
                if (bannerdetail.Details != null && bannerdetail.BannerType != 2)
                {
                    var details = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(bannerdetail.Details);
                    if (details.ContainsKey(LangId))
                    {
                        var settings = details[LangId];
                        banner.Add(new BannerV3()
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
                            BannerMobileUrl = settings["bannerMobileUrl"],
                            tpGameType = tpGameTypeHelper.GetTpGameType(bannerdetail, CurrencyId),
                        });
                    }
                }
                else if (bannerdetail.SportLeaguesId != null && bannerdetail.SportTeamsHome != null && bannerdetail.SportTeamsAway != null && bannerdetail.BannerType == 2)
                {
                    var sportLeaguesId = JsonConvert.DeserializeObject<Dictionary<string, string>>(bannerdetail.SportLeaguesId);
                    var sportTeamsHome = JsonConvert.DeserializeObject<Dictionary<string, string>>(bannerdetail.SportTeamsHome);
                    var sportTeamsAway = JsonConvert.DeserializeObject<Dictionary<string, string>>(bannerdetail.SportTeamsAway);
                    var logoPic = "";
                    var backgroundPic = "";
                    if (sportLeaguesId.ContainsKey(LangId) && sportTeamsHome.ContainsKey(LangId) && sportTeamsAway.ContainsKey(LangId))
                    {
                        //if (serverInfo.SiteId == 1 && (bannerdetail.LeaguesId == 1 || bannerdetail.LeaguesId == 2 || bannerdetail.LeaguesId == 3 || bannerdetail.LeaguesId == 4 || bannerdetail.LeaguesId == 5 ))
                        if (serverInfo.SiteId == 1 && bannerdetail.LeaguesId != 6)
                        {
                            logoPic = $"{siteConfig.Value[serverInfo.SiteName].ImageURL}/Sports/{sportLeaguesId["en"]}/{serverInfo.SiteName}/Logo.png";
                            backgroundPic = $"{siteConfig.Value[serverInfo.SiteName].ImageURL}/Sports/{sportLeaguesId["en"]}/{serverInfo.SiteName}/Background.jpg";
                        }
                        var leagues = sportLeaguesId[LangId];
                        var teamsHome = sportTeamsHome[LangId];
                        var teamsHomePic = $"{siteConfig.Value[serverInfo.SiteName].ImageURL}/Sports/{sportLeaguesId["en"]}/{sportTeamsHome["en"]}.png";
                        var teamsAway = sportTeamsAway[LangId];
                        var teamsAwayPic = $"{siteConfig.Value[serverInfo.SiteName].ImageURL}/Sports/{sportLeaguesId["en"]}/{sportTeamsAway["en"]}.png";
                        banner.Add(new BannerV3()
                        {
                            BannerName = bannerdetail.BannerName,
                            BannerType = bannerdetail.BannerType,
                            ActionMode = bannerdetail.ActionMode,
                            BonusCode = bannerdetail.BonusCode,
                            MatchId = bannerdetail.MatchId,
                            TpId = bannerdetail.TpId,
                            GameId = bannerdetail.GameId,
                            UrlRouter = bannerdetail.UrlRouter,
                            SportBattleDateTime = bannerdetail.SportBattleDateTime,
                            LeaguesId = bannerdetail.LeaguesId,
                            SportLeaguesId = leagues,
                            SportTeamsHome = teamsHome,
                            SportTeamsHomePic = teamsHomePic,
                            SportTeamsAway = teamsAway,
                            SportTeamsAwayPic = teamsAwayPic,
                            HomeOdds = bannerdetail.HomeOdds,
                            AwayOdds = bannerdetail.AwayOdds,
                            Handicap = bannerdetail.Handicap,
                            Logo = logoPic,
                            BackgroundPic = backgroundPic,
                            tpGameType = tpGameTypeHelper.GetTpGameType(bannerdetail, CurrencyId)
                        });
                    }
                }
            }
            if (BannerType == 2)
            {
                banner = banner.OrderBy(x => x.SportBattleDateTime).ToList();
            }
            return banner;
        }

        public async Task<IEnumerable<HotGame>> GetHotGames(int siteId, int currencyId, int deviceType)
        {
            var key = $"GetHotGames_{siteId}_{currencyId}";
            var hotGames = await _redisHelper.TryGetOrCreateAsync(key, null,
                async _ => await bannerService.GetHotGames(siteId, currencyId));
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
