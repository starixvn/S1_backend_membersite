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
    public class NewsService
    {
        private readonly IConfiguration configuration;

        public NewsService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public News GetNewsById(int SiteId, int NewsId)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.QueryFirstOrDefault<News>("Member_News_GetById", new { SiteId, NewsId }, commandType: CommandType.StoredProcedure);
        }

        public IEnumerable<News> GetNews(int SiteId, int pageSize, ref int pageNumber, out int totalPages, out int rowCount)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            var p = new DynamicParameters(new { SiteId, NewsId = 0, PageSize = pageSize, SelectAll =1 });
            p.Add("PageNumber", pageNumber, dbType: DbType.Int32, direction: ParameterDirection.InputOutput);
            p.Add("TotalPages", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("RowCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("StartIndex", dbType: DbType.Int32, direction: ParameterDirection.Output);

            var result = conn.Query<News>("Member_News_Get", p, commandType: CommandType.StoredProcedure);
            totalPages = p.Get<int?>("TotalPages") ?? 0;
            rowCount = p.Get<int?>("RowCount") ?? 0;
            return result;
        }
    }
}
