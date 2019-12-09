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

        public bool ExistsEmail(int custId, string email, int siteId)
        {
            using (MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
            {
                return conn.ExecuteScalar<int>("ExistsEmail_Get", new { CustId = custId, Email = email, SiteId = siteId }, commandType: CommandType.StoredProcedure) != 0;
            }
        }
        public int AddCustInfo(Customer customer, out int custId)
        {
            custId = 0;
            using (MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
            {
                var p = new DynamicParameters(new { customer.SiteId, customer.UserName, customer.Userpwd, customer.CurrencyId, customer.UserLang, customer.AgentId, customer.AllAgentId, customer.UserLevel });
                p.Add("CustId", dbType: DbType.Int32, direction: ParameterDirection.Output);

                var result = conn.Execute("CustInfo_Ins", p, commandType: CommandType.StoredProcedure);
                custId = p.Get<int?>("CustId") ?? 0;
                return result > 0 ? 0 : -1;
            }
        }
        public Customer GetCustInfo(int custId)
        {
            using (MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
            {
                return conn.QueryFirstOrDefault<Customer>("Customer_Get", new { CustId = custId }, commandType: CommandType.StoredProcedure);
            }
        }

        public Customer GetCustInfoByUserName(string userName, int siteId)
        {
            using (MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
            {
                return conn.QueryFirstOrDefault<Customer>("LoginCustInfo_Get", new { UserName = userName, SiteId = siteId }, commandType: CommandType.StoredProcedure);
            }
        }
        public bool SetLanguage(int custId, string langId)
        {
            using (MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
            {
                return conn.Execute("LangId_Set", new { CustId = custId, LangId = langId }, commandType: CommandType.StoredProcedure) > 0;
            }
        }
        public int GetVipLevel(int custId)
        {
            using (MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
            {
                return conn.ExecuteScalar<int>("VipLevel_Get", new { CustId = custId }, commandType: CommandType.StoredProcedure);
            }
        }
        public bool SetPassword(int custId, string md5OldPassword, string md5NewPassword, out string md5Userpwd)
        {
            int result = 0;
            md5Userpwd = null;
            using (MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
            {
                conn.Open();
                using (MySqlTransaction trans = conn.BeginTransaction())
                {
                    var p = new DynamicParameters(new { CustId = custId, Md5OldPassword = md5OldPassword, Md5NewPassword = md5NewPassword });
                    p.Add("Md5Userpwd", dbType: DbType.String, direction: ParameterDirection.Output);

                    result = conn.Execute("Password_Set", p, trans, commandType: CommandType.StoredProcedure);
                    md5Userpwd = p.Get<string>("Md5Userpwd");
                    trans.Commit();
                    return result > 0;
                }
            }
        }
        public bool SetProfile(int custId,int siteId, Dictionary<string, string> profile)
        {
            int result = 0;
            using (MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
            {
                string Email = profile[nameof(Email)];
                result = conn.Execute("CustProfile_Set", new { CustId = custId, SiteId = siteId, Email, Profile = Newtonsoft.Json.JsonConvert.SerializeObject(profile) }, commandType: CommandType.StoredProcedure);
                return result > 0;
            }
        }

        public bool SetCustBankInfo(int custId, int siteId, string accountName, IEnumerable<BankInfoDetail> bankInfo)
        {
            int result = 0;
            using (MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
            {
                result = conn.Execute("CustBankInfo_Set", new { CustId = custId, SiteId = siteId, AccountName = accountName, BankInfo = Newtonsoft.Json.JsonConvert.SerializeObject(bankInfo) }, commandType: CommandType.StoredProcedure);
                return result > 0;
            }
        }

        public CustBankInfo GetCustBankInfo(int custId, int siteId)
        {
            using (MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
            {
                return conn.QueryFirstOrDefault<CustBankInfo>("CustBankInfo_Get", new { CustId = custId, SiteId = siteId }, commandType: CommandType.StoredProcedure);
            }
        }
    }
}
