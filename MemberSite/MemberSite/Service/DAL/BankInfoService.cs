using Dapper;
using MemberSite.Service.Model;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
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
        public IEnumerable<BankInfoPay> GetBankInfoPayList(int siteId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<BankInfoPay>("Common_BankInfoPay_Get", new { SiteId = siteId }, commandType: CommandType.StoredProcedure);
        }
    }
}
