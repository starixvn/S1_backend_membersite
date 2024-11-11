using Dapper;

using MemberSite.Service.DAL.Interface;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.DAL
{
    public class HotEventService : IHotEventService
    {
        private readonly MySqlClient _reportDbClient;

        public HotEventService(IServiceProvider serviceProvider)
        {
            _reportDbClient = serviceProvider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }

        public Task<IEnumerable<HotEventDto>> GetHotEventDataList(int siteId, string langId)
        {
            var p = new DynamicParameters();
            p.Add("_SiteId", siteId);
            p.Add("_langId", langId);
            return _reportDbClient.QueryAsync<HotEventDto>("Member_HotEventDataList_Get", p);
        }


        public Task<IEnumerable<HotMatchDto>> GetHotMatchDataList()
        {
            return _reportDbClient.QueryAsync<HotMatchDto>("Member_HotMatchDataList_Get", null);
        }

        public Task<IEnumerable<SportTeamsInfoDto>> GetSportTeamsInfoList()
        {
            return _reportDbClient.QueryAsync<SportTeamsInfoDto>("Member_SportTeamsInfo_Get", null);
        }

        public Task<IEnumerable<SportLeaguesInfoDto>> GetSportLeaguesInfoList()
        {
            return _reportDbClient.QueryAsync<SportLeaguesInfoDto>("Admin_SportLeaguesOption_Get", null);
        }
    }
}
