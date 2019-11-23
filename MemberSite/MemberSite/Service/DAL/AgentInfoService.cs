using MemberSite.Service.Model;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;

namespace MemberSite.Service.DAL
{
    public class AgentInfoService
    {
        private const string SelectSuperAgent_By_SiteIdAndCurrency = "SELECT CustId,SiteId,UserName,Userpwd,CurrencyId,UserLang,ActStatus,AgentId,AllAgentId,UserLevel,IsTest,LastLoginTime,RegTime FROM Customer WHERE CurrencyId=@CurrencyId AND AgentId=0 AND SiteId=@SiteId";
        private readonly IConfiguration configuration;

        public AgentInfoService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        public Customer GetSuperAgentModel(int currencyId, int siteId)
        {
            using (MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
            {
                return conn.QueryFirst<Customer>(SelectSuperAgent_By_SiteIdAndCurrency, new { CurrencyId = currencyId, SiteId = siteId });
            }
        }
    }
}
