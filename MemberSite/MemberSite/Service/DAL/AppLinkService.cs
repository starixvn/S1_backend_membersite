using MemberSite.Service.Model;
using MemberSite.Service.Library;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.DAL
{
    public class AppLinkService
    {
        private readonly MySqlClient _reportDb;
        
        public AppLinkService(IServiceProvider provider)
        {
            _reportDb = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }
        public List<AppLink> GetAppLink(int SiteId)
        {
            return _reportDb.Query<AppLink>("Service_AppLinkTable_Get", new { SiteId }).ToList();
        }
    }
}
