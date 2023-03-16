using Dapper;
using MemberSite.Service.Model;
using System.Data;
using MemberSite.Service.Library;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.DAL
{
    public class NewsService
    {
        private readonly MySqlClient _reportDbClient;

        public NewsService(IServiceProvider provider)
        {
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }

        public Task<News> GetNewsById(int SiteId, int NewsId)
        {
            return _reportDbClient.QueryFirstAsync<News>("Member_News_GetById", new { SiteId, NewsId });
        }

        public IEnumerable<News> GetNews(int SiteId, int pageSize, ref int pageNumber, out int totalPages, out int rowCount)
        {
            var p = new DynamicParameters(new { SiteId, PageSize = pageSize });
            p.Add("PageNumber", pageNumber, dbType: DbType.Int32, direction: ParameterDirection.InputOutput);
            p.Add("TotalPages", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("RowCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("StartIndex", dbType: DbType.Int32, direction: ParameterDirection.Output);

            var result = _reportDbClient.Query<News>("Member_News_Get", p);
            totalPages = p.Get<int?>("TotalPages") ?? 0;
            rowCount = p.Get<int?>("RowCount") ?? 0;
            pageNumber = p.Get<int?>("PageNumber") ?? 0;
            return result;
        }
    }
}
