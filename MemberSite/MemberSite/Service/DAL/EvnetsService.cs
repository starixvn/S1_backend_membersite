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
    public class EvnetsService
    {
        private readonly IConfiguration configuration;

        public EvnetsService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        public IEnumerable<Leaderboard> GetLeaderboardList(int lBCategoryId, int siteId, int topNum, int beforeDays)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<Leaderboard>("Member_Leaderboard_LastDateTop_Get", new { LBCategoryId = lBCategoryId, SiteId = siteId, TopNum = topNum, BeforeDays= beforeDays }, commandType: CommandType.StoredProcedure);
        }
        public bool GetCustRank(int lBCategoryId, int custId, int beforeDays,out int custRank)
        {
            custRank = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            var p = new DynamicParameters(new { LBCategoryId = lBCategoryId, CustId = custId, BeforeDays = beforeDays });
            p.Add("CustRank", dbType: DbType.Int32, direction: ParameterDirection.Output);
            bool rtn = conn.Execute("Member_Leaderboard_CustRank_Get", p, commandType: CommandType.StoredProcedure) > 0;
            if(rtn)
                custRank = p.Get<int>("CustRank");
            return rtn;
        }
    }
}
