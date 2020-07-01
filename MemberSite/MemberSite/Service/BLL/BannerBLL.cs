using MemberSite.Service.DAL;
using MemberSite.Service.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemberSite.Service.BLL
{
    public class BannerBLL
    {
        private readonly BannerService bannerService;
        public BannerBLL(BannerService bannerService)
        {
            this.bannerService = bannerService;
        }

        public IEnumerable<Banner> GetBanner(int SiteId, string FolderName, string Lang, int CurrencyId)
        {
            return bannerService.GetBanner(SiteId, FolderName, Lang, CurrencyId);
        }
    }
}
