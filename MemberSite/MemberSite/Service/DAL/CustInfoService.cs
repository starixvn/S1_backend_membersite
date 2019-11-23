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
    public class CustInfoService
    {
        private readonly IConfiguration configuration;
        public CustInfoService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        public bool ExistsUserName(string userName, int siteId)
        {
            using (MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
            {
                return conn.ExecuteScalar<int>("ExistsUserName_Get", new { UserName = userName, SiteId = siteId }, commandType: CommandType.StoredProcedure) != 0;
            }
        }
        public int AddCustInfo(Customer customer, out int userId)
        {
            using (MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
            {
                userId = conn.ExecuteScalar<int>("CustInfo_Ins", customer, commandType: CommandType.StoredProcedure);
                return userId > 0 ? 0 : -1;
            }
        }
        public Customer GetCustInfoByUserName(string userName, int siteId)
        {
            using (MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
            {
                return conn.QueryFirstOrDefault<Customer>("LoginCustInfo_Get", new { UserName = userName, SiteId = siteId }, commandType: CommandType.StoredProcedure);
            }
        }
    }
}
