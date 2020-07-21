using MemberSite.Service.DAL;
using MemberSite.Service.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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
                        bannerFromDB.Add(new BannerSettingItem() { actionLink = $"Promotion/Bonus?BonusCode={promo.BonusCode}", imgURL = bannerUrl, fromDB = true });
                    }
                }
            }

            return bannerFromDB;
        }
    }
}
