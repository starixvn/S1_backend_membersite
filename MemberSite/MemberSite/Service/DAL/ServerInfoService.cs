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
        public ServerInfo GetModel()
        {
            int siteId = 1;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.QueryFirstOrDefault<ServerInfo>("ServerInfo_Get", new { SiteId = siteId }, commandType: CommandType.StoredProcedure);
        }
        public dynamic GetModel1()
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.QueryFirstOrDefault<dynamic>("TestDate", new { T = "2019-12-19T18:00:00" }, commandType: CommandType.StoredProcedure);
        }
        public IEnumerable<Country> GetCountryList()
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Query<Country>("Common_CountryList_Get", commandType: CommandType.StoredProcedure);
        }
    }
}
