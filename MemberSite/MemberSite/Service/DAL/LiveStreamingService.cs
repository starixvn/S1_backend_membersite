using Dapper;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.DAL
{
    public class LiveStreamingService
    {
        private readonly MySqlClient _reportDbClient;

        public LiveStreamingService(IServiceProvider provider)
        {
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }

        public IEnumerable<LiveStreaming> GetLiveStreamingList()
        {
            return _reportDbClient.Query<LiveStreaming>("Member_LiveStreaming_Get");
        }
        public Task<IEnumerable<SportLiveStreamingData>> GetSportLiveStreaming(int siteId, string langId)
        {
            var p = new DynamicParameters();
            p.Add("_SiteId", siteId);
            p.Add("_langId", langId);
            return _reportDbClient.QueryAsync<SportLiveStreamingData>("Member_SportLiveStreaming_Get", p);
        }
    }
}
