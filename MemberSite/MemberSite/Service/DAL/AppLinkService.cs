using Dapper;
using MemberSite.Service.Model;
using MySql.Data.MySqlClient;
using System.Data;

namespace MemberSite.Service.DAL
{
    public class AppLinkService
    {
        private readonly IConfiguration configuration;
        public AppLinkService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        public List<AppLink> GetAppLink(int SiteId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<AppLink>("Service_AppLinkTable_Get", new { SiteId }, commandType: CommandType.StoredProcedure).ToList();
        }
    }
}
