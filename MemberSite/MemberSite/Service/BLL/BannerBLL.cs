using MemberSite.Service.DAL;
using MemberSite.Service.Model;
using Microsoft.AspNetCore.Hosting;
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
        private readonly IWebHostEnvironment environment;

        public BannerBLL(BannerService bannerService, ServerInfoBLL serverInfoBLL, IWebHostEnvironment environment)
        {
            this.bannerService = bannerService;
            this.serverInfoBLL = serverInfoBLL;
            this.environment = environment;
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
    }
}
