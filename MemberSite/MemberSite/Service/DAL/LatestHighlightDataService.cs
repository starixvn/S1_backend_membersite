using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.DAL
{
    public class LatestHighlightDataService
    {
        private readonly MySqlClient _reportDbClient;
        
        public LatestHighlightDataService(IServiceProvider provider)
        {
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }
        
        public LatestHighlightData GetLatestHighlightData(int SiteId, string LangId)
        {
            return _reportDbClient.QueryFirst<LatestHighlightData>("Member_LatestHighlightVideo_Get", new { SiteId, LangId });
        }
    }
}
