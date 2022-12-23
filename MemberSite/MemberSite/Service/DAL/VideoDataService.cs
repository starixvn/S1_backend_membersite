using System.Data;
using Dapper;
using MemberSite.Service.Model;
using MySql.Data.MySqlClient;

namespace MemberSite.Service.DAL
{
    public class VideoDataService
    {
        private readonly IConfiguration configuration;
        public VideoDataService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        public VideoData GetVideoData(int SiteId,int CurrencyId, string LangId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.QueryFirstOrDefault<VideoData>("Member_VideoInfo_Get", new { SiteId,CurrencyId,LangId }, commandType: CommandType.StoredProcedure);
        }
    }
}
