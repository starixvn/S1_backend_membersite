using MemberSite.Service.Model;
using MySql.Data.MySqlClient;
using System.Data;
using Dapper;

namespace MemberSite.Service.DAL
{
    public class DeepLinkService
    {
        private readonly IConfiguration configuration;
        public DeepLinkService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        public List<DeepLink> GetDeepLink(int SiteId,int DeepLinkId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<DeepLink>("Service_DeepLinkTable_Get", new { SiteId, DeepLinkId }, commandType: CommandType.StoredProcedure).ToList();
        }
    }
}
