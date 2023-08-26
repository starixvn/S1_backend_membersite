using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.DAL
{
    public class DeepLinkService
    {
        private readonly MySqlClient _reportDbClient;

        public DeepLinkService(IServiceProvider provider)
        {
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }

        public List<DeepLink> GetDeepLink(int SiteId, int DeepLinkId)
        {
            return _reportDbClient.Query<DeepLink>("Service_DeepLinkTable_Get", new { SiteId, DeepLinkId }).ToList();
        }
    }
}
