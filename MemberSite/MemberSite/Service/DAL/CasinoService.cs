using Dapper;
using MemberSite.Service.Model;
using MemberSite.Service.Library;
using Newtonsoft.Json;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.DAL
{
    public class CasinoService
    {
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDb;
        public CasinoService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDb = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }

        public FavoriteGames GetFavoriteGames(int CustId)
        {
            return _reportDb.QueryFirst<FavoriteGames>("Member_Favorite_Get", new { CustId });
        }

        public void SetFavoriteGames(int CustId, string GameInfo, bool IsRemove)
        {
            _mainDbClient.Execute("Member_Favorite_Set",  new DynamicParameters(new { CustId, GameInfo, IsRemove}));
        }

        public IEnumerable<int> GetProviderIdByCurrency(int CurrencyId, int SiteId)
        {
            return _reportDb.Query<int>("Member_TpList_GetByCurrency", new { CurrencyId, SiteId});
        }
        public IEnumerable<GameCasionByCurrencyAndSite> GetGameCasinoByCurrencyAndSite(int CurrencyId, int SiteId)
        {
            return _reportDb.Query<GameCasionByCurrencyAndSite>("Member_GameCasino_Get", new { CurrencyId, SiteId });
        }

        public IEnumerable<DisableTpGame> GetTpDisableUserByCustId(int custId)
        {
            var p = new DynamicParameters();
            p.Add("_CustId", custId);
            var result = _reportDb.QueryFirst<string>("Member_TpDisableUserByCustId_Get", p);
            return (!string.IsNullOrEmpty(result) && result != "null") ? JsonConvert.DeserializeObject<IEnumerable<DisableTpGame>>(result) : new List<DisableTpGame>();
        }

        public Task<FavoriteGames> GetFavoriteGamesAsync(int CustId)
        {
            return _reportDb.QueryFirstAsync<FavoriteGames>("Member_Favorite_Get", new { CustId });
        }

        public async Task<IEnumerable<DisableTpGame>> GetTpDisableUserByCustIdAsync(int custId)
        {
            var p = new DynamicParameters();
            p.Add("_CustId", custId);
            var result = await _reportDb.QueryFirstAsync<string>("Member_TpDisableUserByCustId_Get", p);
            return (!string.IsNullOrEmpty(result) && result != "null") ? JsonConvert.DeserializeObject<IEnumerable<DisableTpGame>>(result) : new List<DisableTpGame>();
        }
    }
}
