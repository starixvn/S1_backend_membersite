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
        public IEnumerable<Prize> CheckDrawPrize(int eventId, int custId, int currencyId, out int prizeId, out int result)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            using var trans = conn.BeginTransaction();

            return CheckDrawPrize(eventId, custId, currencyId, out prizeId, out _, out _, out result, conn, trans);
        }
        private IEnumerable<Prize> CheckDrawPrize(int eventId, int custId, int currencyId, out int prizeId, out int quota, out DateTime checkDate, out int result, MySqlConnection conn, MySqlTransaction trans)
        {
            var p = new DynamicParameters(new { EventId = eventId, CustId = custId, CurrencyId = currencyId });
            p.Add("PrizeId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("Quota", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("CheckDate", dbType: DbType.Date, direction: ParameterDirection.Output);
            p.Add("result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            var rtn = conn.Query<Prize>("Member_CheckDrawPrize_Get", p, transaction: trans, commandType: CommandType.StoredProcedure);
            prizeId = p.Get<int>("PrizeId");
            quota = p.Get<int>("PrizeId");
            checkDate = p.Get<DateTime>("CheckDate");
            result = p.Get<int>("result");

            return rtn;
        }
        public int DrawPrize(int eventId, int custId, int currencyId, string userName, int siteId, out int result, out Prize drawPrize)
        {
            int res = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            using var trans = conn.BeginTransaction();
            drawPrize = null;

            var Prizes = CheckDrawPrize(eventId, custId, currencyId, out int prizeId, out int quota, out DateTime checkDate, out result, conn, trans);

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

                    if (LuckyNum < LuckyCnt)
                    {
                        break;
                    }
                }

                res = conn.Execute("Member_DrawPrize_Set", new { EventId = eventId, PrizeId = prizeId, CustId = custId, DrawDate = checkDate }, transaction: trans, commandType: CommandType.StoredProcedure);
                res = walletService.SetBalance(custId, siteId, userName, prizeAmount, 8, $"Prize: {prizeAmount.ToString("#,##0.##")}", eventId.ToString(), out _, out _, conn, trans);
                res = withdrawalLimitService.SetWithdrawalLimit(custId, userName, siteId, prizeAmount, null, eventId.ToString(), out decimal afterWithdrawalLimit, conn, trans);
                trans.Commit();
            }

            return result;
        }
        public IEnumerable<Prizeleft> GetPrizeleft(int siteId, int currencyId, out int leftTotal)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));

            var p = new DynamicParameters(new { SiteId = siteId, CurrencyId = currencyId });
            p.Add("LeftTotal", dbType: DbType.Int32, direction: ParameterDirection.Output);
            var rtn = conn.Query<Prizeleft>("Member_Prizeleft_Get", p, commandType: CommandType.StoredProcedure);
            leftTotal = p.Get<int?>("LeftTotal") ?? 0;

            return rtn;
        }
        public void GetPrizeCustGift(int custId, out int prizeId, out string giftName)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));

            var p = new DynamicParameters(new { CustId = custId });
            p.Add("PrizeId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("GiftName", dbType: DbType.String, direction: ParameterDirection.Output);
            var rtn = conn.Query<Prizeleft>("Member_PrizeCustGift_Get", p, commandType: CommandType.StoredProcedure);
            prizeId = p.Get<int?>("PrizeId") ?? 0;
            giftName = p.Get<string>("GiftName");
            return;
        }
        public IEnumerable<CustGiftList> GetPrizeCustGiftList(int custId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<CustGiftList>("Member_PrizeCustGiftList_Get", new { CustId = custId }, commandType: CommandType.StoredProcedure);

        }
        public int CheckPrizeLottery(int siteId, int currencyId, int custId, out int prizeId, out string giftName)
        {
            prizeId = 0;
            giftName = "";
            int eventId = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            // using var trans = conn.BeginTransaction();

            var p2 = new DynamicParameters(new { CustId = custId });
            p2.Add("PrizeId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p2.Add("GiftName", dbType: DbType.String, direction: ParameterDirection.Output);
            conn.Execute("Member_PrizeCustGift_Get", p2, commandType: CommandType.StoredProcedure);
            prizeId = p2.Get<int?>("PrizeId") ?? 0;
            giftName = p2.Get<string>("GiftName");

            if (prizeId != 0)
            {
                return 100;//Already been drawn
            }
            var p = new DynamicParameters(new { SiteId = siteId, CurrencyId = currencyId });
            p.Add("LeftTotal", dbType: DbType.Int32, direction: ParameterDirection.Output);
            var Prizes = conn.Query<Prizeleft>("Member_Prizeleft_Get", p, commandType: CommandType.StoredProcedure);
            int leftTotal = p.Get<int?>("LeftTotal") ?? 0;

            if (leftTotal <= 0)
                return 101;//The number of gifts is gone

            return 0;
        }
        public int SetPrizeLottery(int siteId, int currencyId, int custId, out int prizeId, out string giftName)
        {
            prizeId = 0;
            giftName = "";
            int eventId = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            var p2 = new DynamicParameters(new { CustId = custId });
            p2.Add("PrizeId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p2.Add("GiftName", dbType: DbType.String, direction: ParameterDirection.Output);
            conn.Execute("Member_PrizeCustGift_Get", p2, commandType: CommandType.StoredProcedure);
            prizeId = p2.Get<int?>("PrizeId") ?? 0;
            giftName = p2.Get<string>("GiftName");

            if (prizeId != 0)
            {
                return 100;//Already been drawn
            }

            using var trans = conn.BeginTransaction();

            var p = new DynamicParameters(new { SiteId = siteId, CurrencyId = currencyId });
            p.Add("LeftTotal", dbType: DbType.Int32, direction: ParameterDirection.Output);
            var Prizes = conn.Query<Prizeleft>("Member_Prizeleft_Get", p, commandType: CommandType.StoredProcedure);
            int leftTotal = p.Get<int?>("LeftTotal") ?? 0;

            if (leftTotal <= 0)
                return 101;//The number of gifts is gone



            Random r = new Random();
            int LuckyNUM = r.Next(leftTotal) + 1;

            foreach (var item in Prizes)
            {
                if (item.Cumsum >= LuckyNUM)
                {
                    prizeId = item.PrizeId;
                    giftName = item.GiftName;
                    eventId = item.EventId;
                    break;
                }
            }
            if (prizeId == 0)
            {
                return 102;//SOME DATA ERROR
            }

            int rtn = conn.Execute("Member_PrizeDelivery_Set", new { PrizeId = prizeId, CustId = custId, EventId = eventId }, commandType: CommandType.StoredProcedure);

            if (prizeId == 0)
            {
                return 103;//Delivery ERROR
            }
            if (rtn > 0)
                trans.Commit();
            return 0;
        }
    }
}
