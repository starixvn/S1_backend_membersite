using System.Data;

using Dapper;

using MemberSite.Service.Library;
using MemberSite.Service.Model;
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

        public async Task<PageData<News>> GetNews(int SiteId, int pageSize, int pageNumber)
        {
            var p = new DynamicParameters(new { SiteId, PageSize = pageSize });
            p.Add("PageNumber", pageNumber, dbType: DbType.Int32, direction: ParameterDirection.InputOutput);
            p.Add("TotalPages", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("RowCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("StartIndex", dbType: DbType.Int32, direction: ParameterDirection.Output);

            var news = await _reportDbClient.QueryAsync<News>("Member_News_Get", p);
            return new PageData<News>()
            {
                Data = news,
                TotalPages = p.Get<int?>("TotalPages") ?? 0,
                RowCount = p.Get<int?>("RowCount") ?? 0,
                PageNumber = p.Get<int?>("PageNumber") ?? 0
            };
        }
    }
}
