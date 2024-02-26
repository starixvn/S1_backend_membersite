using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.DAL
{
    public class TelegramService
    {
        private readonly MySqlClient _reportDbClient;
        public TelegramService(IServiceProvider provider)
        {
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }

        public IEnumerable<ConfigObject> GetTpSiteConfig()
        {
            var parameter = new { ConfigGroup = "TP" };
            return _reportDbClient.Query<ConfigObject>("SiteConfig_Get", parameter);
        }
    }
}
