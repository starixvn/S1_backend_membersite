using MemberSite.Service.Model;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using System.Data;

namespace MemberSite.Service.DAL
{
    public class ServerInfoService
    {
        private readonly IConfiguration configuration;

        public ServerInfoService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        public ServerInfo GetModel(int siteId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.QueryFirstOrDefault<ServerInfo>("ServerInfo_Get", new { SiteId = siteId }, commandType: CommandType.StoredProcedure);
        }
        public dynamic GetModel1()
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.QueryFirstOrDefault<dynamic>("TestDate", new { T = "2019-12-19T18:00:00" }, commandType: CommandType.StoredProcedure);
        }
        public IEnumerable<Country> GetCountryList()
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<Country>("Common_CountryList_Get", commandType: CommandType.StoredProcedure);
        }

        public int GetSite(string domain)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.QueryFirstOrDefault<int>("Member_SiteId_Get", new { Domain = domain }, commandType: CommandType.StoredProcedure);
        }
        public IEnumerable<Domainlist> GetSupportDomainList(int siteId, int currencyId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<Domainlist>("Member_DomainTable_Get", new { SiteId = siteId, CurrencyId = currencyId }, commandType: CommandType.StoredProcedure);
        }
    }
}
