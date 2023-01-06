using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.DAL
{
    public class VideoDataService
    {
        private readonly MySqlClient _reportDbClient;
        
        public VideoDataService(IServiceProvider provider)
        {
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }
        
        public VideoData GetVideoData(int SiteId,int CurrencyId, string LangId)
        {
            return _reportDbClient.QueryFirst<VideoData>("Member_VideoInfo_Get", new { SiteId,CurrencyId,LangId });
        }
    }
}
