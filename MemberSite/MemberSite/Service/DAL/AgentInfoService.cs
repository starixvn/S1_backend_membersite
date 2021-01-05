using MemberSite.Service.Model;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using System.Data;

namespace MemberSite.Service.DAL
{
    public class AgentInfoService
    {
        private readonly IConfiguration configuration;

        public AgentInfoService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        public Customer GetSuperAgent(int siteId, int currencyId)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.QueryFirst<Customer>("SuperAgent_Get", new { CurrencyId = currencyId, SiteId = siteId }, commandType: CommandType.StoredProcedure);
        }
        public Customer GetAffiliateAgent(string affiliateId, int siteId)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.QueryFirstOrDefault<Customer>("AffiliateAgent_Get", new { AffiliateId = affiliateId, SiteId = siteId }, commandType: CommandType.StoredProcedure);
        }
        public int AddAffiliateId(string affiliateId, int custId, int siteId)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Execute("Member_Affiliate_Ins", new { AffiliateId = affiliateId, CustId = custId, SiteId = siteId }, commandType: CommandType.StoredProcedure);
        }
        public string GetAffiliateId(int custId, int siteId)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.QueryFirstOrDefault<string>("Member_AffiliateId_Get", new { CustId = custId, SiteId = siteId }, commandType: CommandType.StoredProcedure);
        }
        public IEnumerable<ReferralSetting> GetReferralSetting(int siteId, int currencyId)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<ReferralSetting>("Member_ReferralSetting_Get", new { SiteId = siteId, CurrencyId = currencyId }, commandType: CommandType.StoredProcedure);
           
        }
        public void GetReferralSummary(int custId, out int referralCount, out int amountCount, out int amountSum)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            var p = new DynamicParameters(new { CustId = custId});
            p.Add("ReferralCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("AmountCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("AmountSum", dbType: DbType.Int32, direction: ParameterDirection.Output);
            conn.Execute("Member_ReferralSummary_Get", p, commandType: CommandType.StoredProcedure);

            referralCount = p.Get<int?>("ReferralCount") ?? 0;
            amountCount = p.Get<int?>("AmountCount") ?? 0;
            amountSum = p.Get<int?>("AmountSum") ?? 0;
        }
    }
}
