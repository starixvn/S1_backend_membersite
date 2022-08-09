using MemberSite.Service.Model;
using MySql.Data.MySqlClient;
using Dapper;
using System.Data;
using MemberSite.Service.DAL.Interface;
using MemberSite.Service.Library;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.DAL
{
    public class ServerInfoService : IServerInfoService
    {
        private readonly IConfiguration configuration;
        private readonly MySqlClient _reportDbClient;

        public ServerInfoService(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            this.configuration = configuration;
            _reportDbClient = serviceProvider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }
        public ServerInfo GetModel(int siteId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.QueryFirstOrDefault<ServerInfo>("ServerInfo_Get", new { SiteId = siteId }, commandType: CommandType.StoredProcedure);
        }

        public IEnumerable<Country> GetCountryList()
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<Country>("Common_CountryList_Get", commandType: CommandType.StoredProcedure);
        }

        public int GetSite(string domain)
        {
            if (string.IsNullOrEmpty(domain))
            {
                return 0;
            }
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.QueryFirstOrDefault<int>("Member_SiteId_Get", new { Domain = domain }, commandType: CommandType.StoredProcedure);
        }
        
        public SeoModel GetSeo(string domain)
        {
            if (string.IsNullOrEmpty(domain))
            {
                return null;
            }
            
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.QueryFirstOrDefault<SeoModel>("Service_Site_Get", new { Domain = domain }, commandType: CommandType.StoredProcedure);
        }
        
        public IEnumerable<Domainlist> GetSupportDomainList(int siteId, int currencyId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<Domainlist>("Member_DomainTable_Get", new { SiteId = siteId, CurrencyId = currencyId }, commandType: CommandType.StoredProcedure);
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
