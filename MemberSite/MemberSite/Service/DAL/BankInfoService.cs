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
        public IEnumerable<BankInfoOff> GetBankAccountOff(int siteId)
        {
            using (MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
            {
                return conn.Query<BankInfoOff>("BankAccountOff_Get", new { SiteId = siteId }, commandType: CommandType.StoredProcedure);
            }
        }
    }
}
