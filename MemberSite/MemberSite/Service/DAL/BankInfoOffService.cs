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
        
        public IEnumerable<OfflineBankAccountInfo> GetOfflineBankAccount(int siteId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            
            return conn.Query<OfflineBankAccountInfo>("Member_OfflineBankAccount_Get",
                new { _SiteId = siteId }, commandType: CommandType.StoredProcedure);
        }
        
        public IEnumerable<int> GetCustOffDepositSysId(int custId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
      
            return conn.Query<int>("Member_CustOffDepositSysId_Get", new { _CustId = custId }, commandType: CommandType.StoredProcedure);
        }
        
        public IEnumerable<OfflineBankAccountDailyDeposit> GetOfflineBankAccountDailyDeposit(
            int siteId, int currencyId, string bankIds)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            
            return conn.Query<OfflineBankAccountDailyDeposit>("Member_OfflineBankAccount_DailyDepositSummary_Get",
                new
                {
                    _SiteId = siteId,
                    _CurrencyId = currencyId,
                    _BankIds = bankIds
                }, commandType: CommandType.StoredProcedure);
        }
    }
}
