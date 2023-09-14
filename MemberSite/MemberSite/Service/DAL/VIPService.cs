using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.DAL
{
    public class VIPService
    {
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;
        private readonly ILogger<VIPService> _logger;

        public VIPService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
            _logger = provider.GetRequiredService<ILogger<VIPService>>();
        }

        public IEnumerable<VipSystem> GetVipSystem(int siteId)
        {
            return _mainDbClient.Query<VipSystem>("Service_VipSystem_GetV2", new { SiteId = siteId });
        }
    }
}
