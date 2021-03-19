using System.Collections.Generic;
using System.Data;
using Dapper;
using MemberSite.Service.Model;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;

namespace MemberSite.Service.DAL
{
    public class LatestHighlightDataService
    {
        private readonly IConfiguration configuration;
        public LatestHighlightDataService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        public LatestHighlightData GetLatestHighlightData(int SiteId, string LangId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.QueryFirstOrDefault<LatestHighlightData>("Member_LatestHighlightVideo_Get", new { SiteId, LangId }, commandType: CommandType.StoredProcedure);
        }
    }
}
