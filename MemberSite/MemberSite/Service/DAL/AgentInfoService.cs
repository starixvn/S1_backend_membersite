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
    }
}
