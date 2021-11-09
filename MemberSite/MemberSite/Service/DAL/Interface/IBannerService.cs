using System.Collections.Generic;
using System.Threading.Tasks;
using MemberSite.Service.Model;

namespace MemberSite.Service.DAL.Interface
{
    public interface IBannerService
    {
        IEnumerable<BannerV2> GetBannerV2(int SiteId, int BannerType, int ShowStatus, int CurrencyId);
        IEnumerable<BannerV3> GetBannerV3(int SiteId, int BannerType, int ShowStatus, int CurrencyId);
        Task<IEnumerable<HotGameData>> GetHotGames(int siteId, string langId);
    }
}