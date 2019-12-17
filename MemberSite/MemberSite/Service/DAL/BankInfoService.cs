using Dapper;
using MemberSite.Service.Model;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

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
            using (MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
            {
                return conn.Query<BankInfoSys>("BankInfoSysList_Get", new { SiteId = siteId }, commandType: CommandType.StoredProcedure);
            }
        }

        public IEnumerable<BankInfoSys> GetBankInfoSysListForCtrl(int siteId)
        {
            //siteId Still Useless
            using (MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
            {
                return conn.Query<BankInfoSys>("BankInfoSysListForCtrl_Get", new { SiteId = siteId }, commandType: CommandType.StoredProcedure);
            }
        }
    }
}
