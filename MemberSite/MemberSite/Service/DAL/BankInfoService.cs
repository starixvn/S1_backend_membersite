using Dapper;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using MySql.Data.MySqlClient;
using System.Data;

namespace MemberSite.Service.DAL
{
    public class BankInfoService
    {
        private readonly IConfiguration configuration;

        public BankInfoService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        public IEnumerable<BankInfoSys> GetBankInfoSysList(int siteId)
        {
            //siteId Still Useless
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<BankInfoSys>("BankInfoSysList_Get", new { SiteId = siteId }, commandType: CommandType.StoredProcedure);
        }

        public IEnumerable<BankInfoSys> GetBankInfoSysListForCtrl(int siteId)/*Filter Out BankInfoSys.Status != 0*/
        {
            //siteId Still Useless
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<BankInfoSys>("BankInfoSysListForCtrl_Get", new { SiteId = siteId }, commandType: CommandType.StoredProcedure);
        }
        public IEnumerable<BankInfoPay> GetBankInfoPayList(int siteId, DepositType depositType)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<BankInfoPay>("Common_BankInfoPay_Get_V2"
                , new { _SiteId = siteId, _DepositType = (int)depositType }, commandType: CommandType.StoredProcedure);
        }
        
        public IEnumerable<BankInfoName> GetBankInfoNameList(string lang)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<BankInfoName>("Member_BankInfoName_Get", new { _Lang = lang }, commandType: CommandType.StoredProcedure);
        }

        public IEnumerable<BlockAccount> GetBlockBankAccount(int sysId, string accountNum)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<BlockAccount>("Member_BlockAccount_Get", new { SysId = sysId, AccountNum = accountNum }, commandType: CommandType.StoredProcedure);
        }

        public IEnumerable<EWalletInfoSys> GetEWalletInfoSysList()
        {
            //siteId Still Useless
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<EWalletInfoSys>("Service_EWalletInfoSys_Get", commandType: CommandType.StoredProcedure);
        }

        public IEnumerable<BlockAccount> GetBlockEWalletAccount(int sysId, string accountNum)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<BlockAccount>("Member_BlockEWallet_Get", new { SysId = sysId, AccountNum = accountNum }, commandType: CommandType.StoredProcedure);
        }
    }
}
