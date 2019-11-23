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
            using (MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
            {
                return conn.QueryFirstOrDefault<ServerInfo>("ServerInfo_Get", new { SiteId = siteId }, commandType: CommandType.StoredProcedure);
            }
        }
    }
}
