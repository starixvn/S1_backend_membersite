using MemberSite.Service.DAL.Interface;
using MemberSite.Service.Library;
using MemberSite.Service.Model;

using Newtonsoft.Json;

namespace MemberSite.Service.BLL
{
    public class BannerBLL
    {
        private readonly IBannerService bannerService;
        private readonly ServerInfoBll serverInfoBLL;
        private readonly IWebHostEnvironment environment;
        private readonly TpGameTypeHelper tpGameTypeHelper;
        private readonly ICacheHelper _redisHelper;
        private readonly SelLangBLL selLangBLL;
        private readonly ILogger<BannerBLL> _logger;

        public BannerBLL(IServiceProvider provider)
        {
            bannerService = provider.GetRequiredService<IBannerService>();
            serverInfoBLL = provider.GetRequiredService<ServerInfoBll>();
            environment = provider.GetRequiredService<IWebHostEnvironment>();
            tpGameTypeHelper = provider.GetRequiredService<TpGameTypeHelper>();
            _redisHelper = provider.GetRequiredService<ICacheHelper>();
            selLangBLL = provider.GetRequiredService<SelLangBLL>();
            _logger = provider.GetRequiredService<ILogger<BannerBLL>>();
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

        public IEnumerable<BannerV3> GetBannerV3(int siteId, int bannerType, int showStatus, int currencyId, string langId)
        {
            var banner = new List<BannerV3>();
            var bannerList = bannerService.GetBannerV3(siteId, bannerType, showStatus, currencyId);
            foreach (var bannerDetail in bannerList)
            {
                if (bannerDetail.Details != null && bannerDetail.BannerType != 2)
                {
                    try
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
                            BannerLink = settings.TryGetValue("bannerLink", out var valueLink) ? valueLink : "",
                            BannerPCUrl = settings.TryGetValue("bannerPCUrl", out var valuePcUrl) ? valuePcUrl : "",
                            BannerMobileUrl = settings.TryGetValue("bannerMobileUrl", out var valueMobileUrl) ? valueMobileUrl : "",
                            tpGameType = tpGameTypeHelper.GetTpGameType(bannerDetail, currencyId),
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "GetBannerV3 bannerDetail Exception");
                        continue;
                    }
                }
            }

            return banner;
        }

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

        public VipBanner GetVipBanner(int siteId, int currencyId)
        {
            var lastUpdateTimeList = new List<DateTime>();
            var membershipRenewal = new Dictionary<int, string>();
            var depositPriority = new Dictionary<int, string>();
            var withdrawalPriority = new Dictionary<int, string>();
            var serviceSupport = new Dictionary<int, string>();
            var designatedAccountManager = new Dictionary<int, bool>();

            membershipRenewal.Add(1, "lbl_Vip_Normal");
            membershipRenewal.Add(2, "lbl_Vip_Lifetime");
            membershipRenewal.Add(3, "lbl_Vip_3Months");
            membershipRenewal.Add(4, "lbl_Vip_3Months");
            membershipRenewal.Add(5, "lbl_Vip_3Months");
            membershipRenewal.Add(6, "lbl_Vip_3Months");
            depositPriority.Add(3, "lbl_Vip_High");
            depositPriority.Add(4, "lbl_Vip_High");
            depositPriority.Add(5, "lbl_Vip_High");
            depositPriority.Add(6, "lbl_Vip_High");
            withdrawalPriority.Add(3, "lbl_Vip_High");
            withdrawalPriority.Add(4, "lbl_Vip_High");
            withdrawalPriority.Add(5, "lbl_Vip_High");
            withdrawalPriority.Add(6, "lbl_Vip_High");
            serviceSupport.Add(3, "lbl_Vip_High");
            serviceSupport.Add(4, "lbl_Vip_High");
            serviceSupport.Add(5, "lbl_Vip_High");
            serviceSupport.Add(6, "lbl_Vip_High");
            designatedAccountManager.Add(4, true);
            designatedAccountManager.Add(5, true);
            designatedAccountManager.Add(6, true);
            var vipBanner = bannerService.GetVipBanner(siteId, currencyId);
            var rebateCategory = bannerService.GetRebateCategory(siteId, currencyId).ToList();
            foreach (var vip in vipBanner)
            {
                vip.RebateCategory = rebateCategory.FindAll(x => x.Level == vip.Level);
                vip.RankingValidityMembershipRenewal = selLangBLL.GetResourceValue(membershipRenewal[vip.Level]);
                vip.DepositPriority = depositPriority.ContainsKey(vip.Level) ? selLangBLL.GetResourceValue(depositPriority[vip.Level]) : null;
                vip.WithdrawalPriority = withdrawalPriority.ContainsKey(vip.Level) ? selLangBLL.GetResourceValue(withdrawalPriority[vip.Level]) : null;
                vip.ServiceSupport = serviceSupport.ContainsKey(vip.Level) ? selLangBLL.GetResourceValue(serviceSupport[vip.Level]) : null;
                vip.DesignatedAccountManager = designatedAccountManager.ContainsKey(vip.Level);
                decimal tempWithdrawalMax;
                decimal.TryParse(vip.WithdrawalMax, out tempWithdrawalMax);
                int tempTimes;
                int.TryParse(vip.Times, out tempTimes);
                if (tempWithdrawalMax == 0 && vip.Level != 6)
                {
                    vip.WithdrawalMax = selLangBLL.GetResourceValue("lbl_VIP_unlimited");
                }
                if (tempTimes == 0 && vip.Level != 6)
                {
                    vip.Times = selLangBLL.GetResourceValue("lbl_VIP_unlimited");
                }
                if (vip.Level == 6)
                {
                    vip.WithdrawalMax = selLangBLL.GetResourceValue("lbl_VIP_unlimited");
                    vip.Times = selLangBLL.GetResourceValue("lbl_VIP_unlimited");
                    vip.UpBonus = selLangBLL.GetResourceValue("lbl_Vip_ByInvitation");
                    vip.UpDeposit = selLangBLL.GetResourceValue("lbl_Vip_ByInvitation");
                    vip.DownDeposit = selLangBLL.GetResourceValue("lbl_Vip_ByInvitation");
                    vip.DownTurnover = selLangBLL.GetResourceValue("lbl_Vip_ByInvitation");
                }
            }

            var lastUpdateTime = bannerService.GetVipBannerLastUpdateTime(siteId, currencyId);
            lastUpdateTimeList.Add(lastUpdateTime.RebateCategoryLastUpdateTime);
            lastUpdateTimeList.Add(lastUpdateTime.WithdrawalLimitInfoLastUpdateTime);
            lastUpdateTimeList.Add(lastUpdateTime.VipSystemLastUpdateTime);

            return new VipBanner()
            {
                VipBannerData = vipBanner,
                LastUpdateTime = lastUpdateTimeList.Max()
            };
        }
    }
}
