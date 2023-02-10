using Dapper;
using MemberSite.Service.DAL.Interface;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.DAL
{
    public class HotGameService : IHotGameService
    {
        private readonly MySqlClient _reportDbClient;

        public HotGameService(IEnumerable<MySqlClient> mySqlClients)
        {
            _reportDbClient = mySqlClients.First(p => p.DataBase == DataBase.Report);
        }

        public Task<IEnumerable<HotGameDto>> GetHotGames(int siteId, int currencyId)
        {
            var p = new DynamicParameters();
            p.Add("_SiteId", siteId);
            p.Add("_CurrencyId", currencyId);
            return _reportDbClient.QueryAsync<HotGameDto>("Member_HotGames_Get_V2", p);
        }
    }
}
