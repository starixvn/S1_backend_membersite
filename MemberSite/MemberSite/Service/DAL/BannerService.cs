using Dapper;

using MemberSite.Service.DAL.Interface;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.DAL
{
    public class BannerService : IBannerService
    {
        private readonly MySqlClient _reportDbClient;
        public BannerService(IServiceProvider serviceProvider)
        {
            _reportDbClient = serviceProvider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }

        public IEnumerable<BannerV2> GetBannerV2(int SiteId, int BannerType, int ShowStatus, int CurrencyId)
        {
            return _reportDbClient.Query<BannerV2>("Member_Banner_Get", new { SiteId, BannerType, ShowStatus, CurrencyId });
        }

        public IEnumerable<BannerV3> GetBannerV3(int SiteId, int BannerType, int ShowStatus, int CurrencyId)
        {
            return _reportDbClient.Query<BannerV3>("Member_Banner_Get_V3", new { SiteId, BannerType, ShowStatus, CurrencyId });
        }

        public async Task<IEnumerable<HotGameData>> GetHotGames(int siteId, int currencyId)
        {
            var p = new DynamicParameters();
            p.Add("_SiteId", siteId);
            p.Add("_CurrencyId", currencyId);
            var result = await _reportDbClient.QueryAsync<HotGameData>("Member_HotGames_Get", p);
            return result;
        }

        public IEnumerable<RebateCategory> GetRebateCategory(int SiteId, int CurrencyId)
        {
            return _reportDbClient.Query<RebateCategory>("Member_RebateCategory_Get", new { SiteId, CurrencyId });
        }
        public IEnumerable<VipBannerData> GetVipBanner(int SiteId, int CurrencyId)
        {
            return _reportDbClient.Query<VipBannerData>("Member_VipBanner_Get", new { SiteId, CurrencyId });
        }
        public LastUpdateTime GetVipBannerLastUpdateTime(int SiteId, int CurrencyId)
        {
            return _reportDbClient.Query<LastUpdateTime>("Member_VipBannerLastUpdateTime_Get", new { SiteId, CurrencyId }).FirstOrDefault();
        }
    }
}
