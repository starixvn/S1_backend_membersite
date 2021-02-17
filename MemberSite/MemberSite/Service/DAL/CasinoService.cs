using Dapper;
using MemberSite.Service.Model;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MemberSite.Service.DAL
{
    public class CasinoService
    {
        private readonly IConfiguration configuration;
        public CasinoService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public FavoriteGames GetFavoriteGames(int CustId)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Query<FavoriteGames>("Member_Favorite_Get", new { CustId }, commandType: CommandType.StoredProcedure).FirstOrDefault();
        }

        public void SetFavoriteGames(int CustId, string GameInfo, bool IsRemove)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Execute("Member_Favorite_Set", new { CustId, GameInfo, IsRemove}, commandType: CommandType.StoredProcedure);
        }

        public IEnumerable<int> GetProviderIdByCurrency(int CurrencyId, int SiteId)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Query<int>("Member_TpList_GetByCurrency", new { CurrencyId, SiteId}, commandType: CommandType.StoredProcedure);
        }
        public IEnumerable<GameCasionByCurrencyAndSite> GetGameCasinoByCurrencyAndSite(int CurrencyId, int SiteId)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Query<GameCasionByCurrencyAndSite>("Member_GameCasino_Get", new { CurrencyId, SiteId }, commandType: CommandType.StoredProcedure);
        }
    }
}
