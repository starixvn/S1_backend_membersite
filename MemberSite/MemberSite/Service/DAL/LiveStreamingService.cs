using System.Data;
using Dapper;
using MemberSite.Service.Model;
using MySql.Data.MySqlClient;

namespace MemberSite.Service.DAL
{
    public class LiveStreamingService
    {
        private readonly IConfiguration configuration;
        public LiveStreamingService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        public IEnumerable<LiveStreaming> GetLiveStreamingList()
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<LiveStreaming>("Member_LiveStreaming_Get", commandType: CommandType.StoredProcedure);
        }
    }
}
