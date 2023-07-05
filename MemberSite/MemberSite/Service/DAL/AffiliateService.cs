using MemberSite.Service.DAL.Interface;
using MemberSite.Service.Library;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.DAL
{
    public class AffiliateService : IAffiliateService
    {
        private readonly MySqlClient _reportDbClient;

        public AffiliateService(IEnumerable<MySqlClient> mySqlClients)
        {
            _reportDbClient = mySqlClients.First(p => p.DataBase == DataBase.Report);
        }

        public bool ExistsAffiliateCode(int siteId, string affCode) =>
            _reportDbClient.ExecuteScalar<int>("Member_ExistsAffiliateCode_Get",
                new { _SiteId = siteId, _AffCode = affCode }) == 1;
    }
}
