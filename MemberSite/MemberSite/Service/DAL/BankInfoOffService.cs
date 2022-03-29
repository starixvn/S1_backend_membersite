using Dapper;
using MemberSite.Service.Model;
using MySql.Data.MySqlClient;
using System.Data;

namespace MemberSite.Service.DAL
{
    public class BankInfoOffService
    {
        private readonly IConfiguration configuration;

        public BankInfoOffService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        public IEnumerable<BankInfoOff> GetBankAccountOff(int siteId, int queryType = 1)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            if (queryType == 0)
                return conn.Query<BankInfoOff>("BankAccountOff_Get", new { SiteId = siteId }, commandType: CommandType.StoredProcedure);
            else
                return conn.Query<BankInfoOff>("Member_BankAccountOffAllStatus_Get", new { SiteId = siteId }, commandType: CommandType.StoredProcedure);
        }
    }
}
