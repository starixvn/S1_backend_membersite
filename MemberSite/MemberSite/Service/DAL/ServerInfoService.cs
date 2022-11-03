using MemberSite.Service.Model;
using Dapper;
using MemberSite.Service.DAL.Interface;
using MemberSite.Service.Library;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.DAL
{
    public class ServerInfoService : IServerInfoService
    {
        private readonly MySqlClient _reportDbClient;

        public ServerInfoService(IServiceProvider serviceProvider)
        {
            _reportDbClient = serviceProvider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }
        public ServerInfo GetModel(int siteId)
        {
            return _reportDbClient.QueryFirst<ServerInfo>("ServerInfo_Get", new { SiteId = siteId });
        }

        public IEnumerable<Country> GetCountryList()
        {
            return _reportDbClient.Query<Country>("Common_CountryList_Get");
        }

        public int GetSite(string domain)
        {
            return string.IsNullOrEmpty(domain) ? 0 : _reportDbClient.QueryFirst<int>("Member_SiteId_Get", new { Domain = domain });
        }
        
        public SeoModel GetSeo(string domain)
        {
            return string.IsNullOrEmpty(domain) ? null : _reportDbClient.QueryFirst<SeoModel>("Service_Site_Get", new { Domain = domain });
        }
        
        public IEnumerable<Domainlist> GetSupportDomainList(int siteId, int currencyId)
        {
            return _reportDbClient.Query<Domainlist>("Member_DomainTable_Get", new { SiteId = siteId, CurrencyId = currencyId });
        }

        public async Task<IEnumerable<MenuOfGameData>> GetMenuOfGames(int siteId, string langId)
        {
            var p = new DynamicParameters();
            p.Add("_SiteId", siteId);
            p.Add("_LangId", langId);
            var result = await _reportDbClient.QueryAsync<MenuOfGameData>("Member_SiteMenuOfGames_Get", p);
            return result;
        }

        public async Task<IEnumerable<SocialChannel>> GetSocialChannel(SocialChannelRequest request)
        {
            var p = new DynamicParameters();
            p.Add("_SiteId", request.SiteId);
            p.Add("_CurrencyId", request.CurrencyId);
            p.Add("_LangId", request.LangId);
            p.Add("DisplayPage", request.DisplayPage);
            return await _reportDbClient.QueryAsync<SocialChannel>("Member_SocialChannel_Get_V2", p);
        }
    }

    
}
