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
    }
}
