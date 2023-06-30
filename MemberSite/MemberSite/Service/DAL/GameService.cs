using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.DAL
{
    public class GameService
    {
        private readonly MySqlClient _reportDbClient;

        public GameService(IEnumerable<MySqlClient> mySqlClients)
        {
            _reportDbClient = mySqlClients.First(p => p.DataBase == DataBase.Report);
        }

        public Task<IEnumerable<TpGameIdListData>> GetTpGameIdList()
        {
            return _reportDbClient.QueryAsync<TpGameIdListData>("Member_TpGameId_Get", null);
        }
    }
}
