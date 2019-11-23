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
        public DispositInfo2 GetBankAccountOff(int sysId, int custId, decimal amount)
        {
            using (MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
            {
                return conn.QueryFirstOrDefault<DispositInfo2>("BankAccountOff_Get", new { SysId = sysId, CustId = custId, Amount = amount }, commandType: CommandType.StoredProcedure);
            }
        }
    }
}
