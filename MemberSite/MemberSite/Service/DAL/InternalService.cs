using Dapper;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.DAL
{
    public class InternalService
    {
        private readonly MySqlClient _reportDbClient;

        public InternalService(IServiceProvider serviceProvider)
        {
            _reportDbClient = serviceProvider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }
        
        public Task<IEnumerable<HotEventInfo>> GetHotEventDataList(int siteId)
        {
            var p = new DynamicParameters();
            p.Add("_SiteId", siteId);
            return _reportDbClient.QueryAsync<HotEventInfo>("Service_HotEventInfo_Get", p);
        }
        
    }
}