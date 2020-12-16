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
    public class EventsService
    {
        private readonly IConfiguration configuration;

        public EventsService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        public IEnumerable<Leaderboard> GetLeaderboardList(int lBCategoryId, int siteId,int currencyId, int topNum, int beforeDays)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<Leaderboard>("Member_Leaderboard_LastDateTop_Get", new { LBCategoryId = lBCategoryId, SiteId = siteId, CurrencyId = currencyId, TopNum = topNum, BeforeDays= beforeDays }, commandType: CommandType.StoredProcedure);
        }
        public void GetCustRank(int lBCategoryId, int custId, int beforeDays, out int custRank, out Decimal point)
        {
            custRank = 0;
            point = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            var p = new DynamicParameters(new { LBCategoryId = lBCategoryId, CustId = custId, BeforeDays = beforeDays });
            p.Add("CustRank", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("Point", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            conn.Execute("Member_Leaderboard_CustRank_Get", p, commandType: CommandType.StoredProcedure);

            custRank = p.Get<int?>("CustRank") ?? 0;
            point = p.Get<decimal?>("Point") ?? 0;
        }
    }
}
