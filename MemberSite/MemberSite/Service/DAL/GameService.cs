using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using MemberSite.Service.Model.Game;
using Newtonsoft.Json;

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

        public Task<IEnumerable<LobbyHeaderData>> GetLobbyHeader(GetLobbyHeaderRequest req, List<int> statusList)
        {
            return _reportDbClient.QueryAsync<LobbyHeaderData>("Member_HeaderMenu_Get_xtest",
                new
                {
                    _SiteId = req.SiteId,
                    _CurrencyId = req.CurrencyId,
                    StatusJson = JsonConvert.SerializeObject(statusList)
                });
        }

        public Task<IEnumerable<LobbyHeaderData>> GetLobbyGame(GetLobbyGameRequest req)
        {
            return _reportDbClient.QueryAsync<LobbyHeaderData>("Member_LobbyGames_Get_xtest",
                new
                {
                    _SiteId = req.SiteId,
                    _CurrencyId = req.CurrencyId,
                    _LobbyCategoryIds = req.LobbyCategoryIds,
                    _LangId = req.LangId
                });
        }

        public Task<LobbyHeaderSequenceData> GetLobbyHeaderSequence(GetLobbyHeaderRequest req)
        {
            return _reportDbClient.QueryFirstAsync<LobbyHeaderSequenceData>("Member_LobbyHeaderSequence_Get", new { _SiteId = req.SiteId, _CurrencyId = req.CurrencyId });
        }
    }
}
