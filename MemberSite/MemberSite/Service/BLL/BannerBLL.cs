using MemberSite.Service.DAL;
using MemberSite.Service.Model;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MemberSite.Service.BLL
{
    public class BannerBLL
    {
        private readonly BannerService bannerService;
        private readonly ServerInfoBLL serverInfoBLL;
        private readonly PromotionBLL promotionBLL;
        private readonly IWebHostEnvironment environment;

        public BannerBLL(BannerService bannerService, ServerInfoBLL serverInfoBLL, IWebHostEnvironment environment, PromotionBLL promotionBLL)
        {
            this.bannerService = bannerService;
            this.serverInfoBLL = serverInfoBLL;
            this.environment = environment;
            this.promotionBLL = promotionBLL;
        }

        //public IEnumerable<Banner> GetBanner(int SiteId, string FolderName, string Lang, int CurrencyId)
        //{
        //    return bannerService.GetBanner(SiteId, FolderName, Lang, CurrencyId);
        //}

        public IEnumerable<BannerSettingItem> GetBanner(int perferCurr, string FolderName)
        {
            var serverInfo = serverInfoBLL.GetServerInfo();
            string dataPath = Path.Combine(environment.WebRootPath, $"datas/{serverInfo.SiteName}_bannerSetting.json");
            BannerSetting _bannerSetting = new BannerSetting();
            if (System.IO.File.Exists(dataPath))
            {
                _bannerSetting = JsonConvert.DeserializeObject<BannerSetting>(System.IO.File.ReadAllText(dataPath));
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
                        bool hideInHome = false;
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
        public IEnumerable<BannerV2> GetBannerV2(int SiteId, int BannerType, int ShowIndex, int CurrencyId, string LangId)
        {
            var banner = new List<BannerV2>();
            var bannerlist = bannerService.GetBannerV2(SiteId, BannerType, ShowIndex, CurrencyId);
            foreach (var bannerdetail in bannerlist)
            {
                if (bannerdetail.Details != null)
                {
                    var details = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(bannerdetail.Details);
                    Dictionary<string, string> settings = null;
                    if (details.TryGetValue(LangId, out settings))
                    {
                        banner.Add(new BannerV2()
                        {
                            BannerName = bannerdetail.BannerName,
                            BannerType = bannerdetail.BannerType,
                            ActionMode = bannerdetail.ActionMode,
                            BonusCode = bannerdetail.BonusCode,
                            MatchId = bannerdetail.MatchId,
                            TpId = bannerdetail.TpId,
                            GameId = bannerdetail.GameId,
                            BannerLink = settings["bannerLink"],
                            BannerPCUrl = settings["bannerPCUrl"],
                            BannerMobileUrl = settings["bannerMobileUrl"]
                        }); ;
                    }
                }
            }
            return banner;
        }
    }
}
