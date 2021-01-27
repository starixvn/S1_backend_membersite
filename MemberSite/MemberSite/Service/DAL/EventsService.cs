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
        private readonly WalletService walletService;
        private readonly WithdrawalLimitService withdrawalLimitService;

        public EventsService(IConfiguration configuration, WalletService walletService, WithdrawalLimitService withdrawalLimitService)
        {
            this.configuration = configuration;
            this.walletService = walletService;
            this.withdrawalLimitService = withdrawalLimitService;
        }
        public IEnumerable<Leaderboard> GetLeaderboardList(int lBCategoryId, int siteId, int currencyId, int topNum, int beforeDays)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<Leaderboard>("Member_Leaderboard_LastDateTop_Get", new { LBCategoryId = lBCategoryId, SiteId = siteId, CurrencyId = currencyId, TopNum = topNum, BeforeDays = beforeDays }, commandType: CommandType.StoredProcedure);
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
        public IEnumerable<Leaderboard> GetLeaderboardListV2(int lBCategoryId, string eventCode, int siteId, int currencyId, int topNum, int beforeDays)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<Leaderboard>("Member_Leaderboard_LastDateTopV2_Get", new { LBCategoryId = lBCategoryId, EventCode = eventCode, SiteId = siteId, CurrencyId = currencyId, TopNum = topNum, BeforeDays = beforeDays }, commandType: CommandType.StoredProcedure);
        }
        public void GetCustRankV2(int lBCategoryId, string eventCode, int custId, int beforeDays, out int custRank, out Decimal point)
        {
            custRank = 0;
            point = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            var p = new DynamicParameters(new { LBCategoryId = lBCategoryId, EventCode = eventCode, CustId = custId, BeforeDays = beforeDays });
            p.Add("CustRank", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("Point", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            conn.Execute("Member_Leaderboard_CustRankV2_Get", p, commandType: CommandType.StoredProcedure);

            custRank = p.Get<int?>("CustRank") ?? 0;
            point = p.Get<decimal?>("Point") ?? 0;
        }
        public IEnumerable<Prize> CheckDrawPrize(ref int eventId, int siteId ,int custId, int currencyId, out int prizeId, out int result)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();

            return CheckDrawPrize(ref eventId, siteId, custId, currencyId, out prizeId, out _, out _, out result, conn, trans);
        }
        private IEnumerable<Prize> CheckDrawPrize(ref int eventId, int siteId, int custId, int currencyId, out int prizeId, out int quota, out DateTime checkDate, out int result, MySqlConnection conn, MySqlTransaction trans)
        {
            var p = new DynamicParameters(new {SiteId = siteId, CustId = custId, CurrencyId = currencyId });
            p.Add("EventId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("PrizeId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("Quota", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("CheckDate", dbType: DbType.Date, direction: ParameterDirection.Output);
            p.Add("result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            var rtn = conn.Query<Prize>("Member_CheckDrawPrize_Get", p, transaction: trans, commandType: CommandType.StoredProcedure);
            eventId = p.Get<int>("EventId");
            prizeId = p.Get<int>("PrizeId");
            quota = p.Get<int>("Quota");
            checkDate = p.Get<DateTime>("CheckDate");
            result = p.Get<int>("result");

            return rtn;
        }
        public int DrawPrize(ref int eventId, int custId, int currencyId, string userName, int siteId, out int result, out Prize drawPrize)
        {
            int res = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            drawPrize = null;
            int PrizeType = 0;

            var Prizes = CheckDrawPrize(ref eventId, siteId, custId, currencyId, out int prizeId, out int quota, out DateTime checkDate, out result, conn, trans);

            if (result == 100)
            {
                drawPrize = Prizes.Where(x => x.PrizeId == prizeId).FirstOrDefault();
            }
            else if (result == 0)
            {
                Random r = new Random();
                int LuckyNum = r.Next(quota) + 1;
                int LuckyCnt = 0;
                decimal prizeAmount = 0;
                foreach (var prize in Prizes.Where(x => x.Quota > 0))
                {
                    drawPrize = prize;
                    prizeId = prize.PrizeId;
                    prizeAmount = prize.Amount;
                    LuckyCnt += prize.Quota;
                    PrizeType = prize.PrizeType;

                    if (LuckyNum < LuckyCnt)
                    {
                        break;
                    }
                }
                if(LuckyCnt == 0)
                {
                    var prize = Prizes.Aggregate((min, next) => min.Amount > next.Amount ? next : min);
                    drawPrize = prize;
                    prizeId = prize.PrizeId;
                    prizeAmount = prize.Amount;
                    PrizeType = prize.PrizeType;
                }

                res = conn.Execute("Member_DrawPrize_Set", new { EventId = eventId, PrizeId = prizeId, CustId = custId, DrawDate = checkDate }, transaction: trans, commandType: CommandType.StoredProcedure);
                if (PrizeType == 1)
                {
                    res = walletService.SetBalance(custId, siteId, userName, prizeAmount, 8, $"Prize: {prizeAmount.ToString("#,##0.##")}", eventId.ToString(), out _, out _, conn, trans);
                    res = withdrawalLimitService.SetWithdrawalLimit(custId, userName, siteId, prizeAmount, null, eventId.ToString(), out decimal afterWithdrawalLimit, conn, trans);
                }
                trans.Commit();
            }

            return result;
        }
        public IEnumerable<CustGiftList> GetCustDrawPrizeList(int eventId, int siteId, int custId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<CustGiftList>("Member_CustDrawPrizeList_Get", new { EventId = eventId, SiteId = siteId, CustId = custId  }, commandType: CommandType.StoredProcedure);
        }
        public void GetLeaderboardEventCode( int siteId,out string eventCodeNow,out string eventCodeLast)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            var p = new DynamicParameters(new { SiteId = siteId});
            p.Add("NowEventCode", dbType: DbType.String, direction: ParameterDirection.Output);
            p.Add("LastEventCode", dbType: DbType.String, direction: ParameterDirection.Output);
            var rtn = conn.Execute("Member_LeaderboardEvent_Get", p, commandType: CommandType.StoredProcedure);
            eventCodeNow = p.Get<string>("NowEventCode");
            eventCodeLast = p.Get<string>("LastEventCode");
        }
        public LeaderboardInfo GetLeaderboardInfo(string eventCode, int siteId, int currencyId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            var info = conn.QueryFirstOrDefault<LeaderboardInfo>("Member_LeaderboardEventInfo_Get", new { EventCode = eventCode, SiteId = siteId }, commandType: CommandType.StoredProcedure);
            info.RewardInfolist = conn.Query<RewardInfo>("Member_LeaderboardRewardInfo_Get", new { EventCode = eventCode, SiteId = siteId, CurrencyId = currencyId }, commandType: CommandType.StoredProcedure);
            return info;
        }
    }
}
