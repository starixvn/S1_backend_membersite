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
    public class BankInfoOffService
    {
        private readonly IConfiguration configuration;

        public BankInfoOffService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        public IEnumerable<BankInfoOff> GetBankAccountOff(int siteId, int queryType = 0)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            if (queryType == 0)
                return conn.Query<BankInfoOff>("BankAccountOff_Get", new { SiteId = siteId }, commandType: CommandType.StoredProcedure);
            else
                return conn.Query<BankInfoOff>("Member_BankAccountOffAllStatus_Get", new { SiteId = siteId }, commandType: CommandType.StoredProcedure);
        }
    }
}
