using Dapper;
using MemberSite.Service.DAL.Interface;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using MySql.Data.MySqlClient;
using System.Data;

namespace MemberSite.Service.DAL
{
    public class EventsService : IEventsService
    {
        private readonly IConfiguration configuration;
        private readonly WalletService walletService;
        private readonly WithdrawalLimitService withdrawalLimitService;
        private readonly MySqlClient _reportDb;

        public EventsService(IConfiguration configuration, WalletService walletService, WithdrawalLimitService withdrawalLimitService, IEnumerable<MySqlClient> sqlClients)
        {
            this.configuration = configuration;
            this.walletService = walletService;
            this.withdrawalLimitService = withdrawalLimitService;
            _reportDb = sqlClients.FirstOrDefault(a => a.DataBase == DataBase.Report);
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

        public IEnumerable<Leaderboard> GetLeaderboardListV3(int lBCategoryId, string eventCode, int siteId, int currencyId, int topNum, int beforeDays)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));

            var p = new DynamicParameters(new { EventCode = eventCode });
            p.Add("CustRankSP", dbType: DbType.String, direction: ParameterDirection.Output);
            p.Add("LastDateTopSP", dbType: DbType.String, direction: ParameterDirection.Output);
            conn.Execute("Member_LeaderboardEventSetting_Get", p, commandType: CommandType.StoredProcedure);

            var custRankSP = p.Get<string>("CustRankSP");
            var lastDateTopSP = p.Get<string>("LastDateTopSP");
            if (lastDateTopSP != null)
                return conn.Query<Leaderboard>(lastDateTopSP, new { LBCategoryId = lBCategoryId, EventCode = eventCode, SiteId = siteId, CurrencyId = currencyId, TopNum = topNum, BeforeDays = beforeDays }, commandType: CommandType.StoredProcedure);
            else
                return conn.Query<Leaderboard>("Member_Leaderboard_LastDateTopV2_Get", new { LBCategoryId = lBCategoryId, EventCode = eventCode, SiteId = siteId, CurrencyId = currencyId, TopNum = topNum, BeforeDays = beforeDays }, commandType: CommandType.StoredProcedure);
        }


        public IEnumerable<Leaderboard> GetLeaderboardListMonth(int lBCategoryId, string eventCode, int siteId, int currencyId, int topNum, DateTime selectDays)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<Leaderboard>("Member_Leaderboard_TopMonth_Get", new { LBCategoryId = lBCategoryId, EventCode = eventCode, SiteId = siteId, CurrencyId = currencyId, TopNum = topNum, SeletDay = selectDays }, commandType: CommandType.StoredProcedure);
        }
        public void GetCustRankV3(int lBCategoryId, string eventCode, int custId, int beforeDays, out int custRank, out Decimal point)
        {
            custRank = 0;
            point = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));

            var p = new DynamicParameters(new { EventCode = eventCode });
            p.Add("CustRankSP", dbType: DbType.String, direction: ParameterDirection.Output);
            p.Add("LastDateTopSP", dbType: DbType.String, direction: ParameterDirection.Output);
            conn.Execute("Member_LeaderboardEventSetting_Get", p, commandType: CommandType.StoredProcedure);

            var custRankSP = p.Get<string>("CustRankSP");
            var lastDateTopSP = p.Get<string>("LastDateTopSP");

            var p1 = new DynamicParameters(new { LBCategoryId = lBCategoryId, EventCode = eventCode, CustId = custId, BeforeDays = beforeDays });
            p1.Add("CustRank", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p1.Add("Point", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            if (custRankSP != null)
            {
                conn.Execute(custRankSP, p1, commandType: CommandType.StoredProcedure);
            }
            else
            {
                conn.Execute("Member_Leaderboard_CustRankV2_Get", p, commandType: CommandType.StoredProcedure);
            }
            custRank = p1.Get<int?>("CustRank") ?? 0;
            point = p1.Get<decimal?>("Point") ?? 0;

        }
        public void GetCustRankMonth(int lBCategoryId, string eventCode, int custId, DateTime selectDays, out int custRank, out Decimal point)
        {
            custRank = 0;
            point = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));

            var p1 = new DynamicParameters(new { LBCategoryId = lBCategoryId, EventCode = eventCode, CustId = custId, SeletDay = selectDays });
            p1.Add("CustRank", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p1.Add("Point", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            {
                conn.Execute("Member_Leaderboard_CustRank_Month_Get", p1, commandType: CommandType.StoredProcedure);
            }
            custRank = p1.Get<int?>("CustRank") ?? 0;
            point = p1.Get<decimal?>("Point") ?? 0;

        }

        public IEnumerable<Prize> CheckDrawPrize(ref int eventId, int siteId, int custId, int currencyId, out int prizeId, out int result)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();

            return CheckDrawPrize(ref eventId, siteId, custId, currencyId, out prizeId, out _, out _, out result, conn, trans);
        }
        private IEnumerable<Prize> CheckDrawPrize(ref int eventId, int siteId, int custId, int currencyId, out int prizeId, out int quota, out DateTime checkDate, out int result, MySqlConnection conn, MySqlTransaction trans)
        {
            var p = new DynamicParameters(new { SiteId = siteId, CustId = custId, CurrencyId = currencyId });
            p.Add("EventId", eventId, dbType: DbType.Int32, direction: ParameterDirection.InputOutput);
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

            var res = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            drawPrize = null;
            decimal prizeAmount = 0;

            var Prizes = CheckDrawPrize(ref eventId, siteId, custId, currencyId, out var prizeId, out var quota, out var checkDate, out result, conn, trans);

            if (result == 100)
            {
                drawPrize = Prizes.FirstOrDefault(x => x.PrizeId == prizeId);
            }
            else if (result == 0)
            {
                if (quota > 0)
                {
                    //指定大獎 Jackpot == 1
                    var bigPrize = Prizes.FirstOrDefault(x => x.Jackpot == 1 && x.Quota > 0);
                    //重新分配Quota個數來改變機率
                    if (quota > 1 && bigPrize != null && bigPrize.Quota > 0 && DateTime.Now > (bigPrize.StartTime.AddHours(((float)new TimeSpan(bigPrize.EndTime.Ticks - bigPrize.StartTime.Ticks).TotalHours * ((float)3 / (float)4)))))
                    {

                        var totalTime = (int)new TimeSpan(bigPrize.EndTime.Ticks - bigPrize.StartTime.Ticks).TotalHours;
                        var elapsedTime = (int)new TimeSpan((DateTime.Now.Ticks - bigPrize.StartTime.Ticks)).TotalHours;
                        var totalQuota = 1000;

                        bigPrize.Quota = (int)((float)totalQuota * ((float)elapsedTime / (float)totalTime));
                        if (quota - 1 > 0)
                        {
                            foreach (var prize in Prizes.Where(x => x.Quota > 0 && x.Jackpot != 1))
                            {
                                prize.Quota = (int)((float)(totalQuota - bigPrize.Quota) * ((float)prize.Quota / (float)(quota - 1)));
                            }
                        }
                        quota = totalQuota;
                    }
                    var r = new Random();
                    var LuckyNum = r.Next(quota) + 1;
                    var LuckyCnt = 0;

                    foreach (var prize in Prizes.Where(x => x.Quota > 0))
                    {
                        drawPrize = prize;
                        prizeId = prize.PrizeId;
                        prizeAmount = prize.Amount;
                        LuckyCnt += prize.Quota;

                        if (LuckyNum <= LuckyCnt)
                        {
                            break;
                        }
                    }
                }
                if (drawPrize == null)
                {
                    var prize = Prizes.Aggregate((min, next) => min.Amount > next.Amount ? next : min);
                    drawPrize = prize;
                    prizeId = prize.PrizeId;
                    prizeAmount = prize.Amount;

                }

                res = conn.Execute("Member_DrawPrize_Set", new { EventId = eventId, PrizeId = prizeId, CustId = custId, DrawDate = checkDate }, transaction: trans, commandType: CommandType.StoredProcedure);
                if (drawPrize.PrizeType == 1) //money
                {
                    res = walletService.SetBalance(custId, siteId, userName, prizeAmount, 8, $"Prize: {prizeAmount.ToString("#,##0.##")}", eventId.ToString(), out _, out _, conn, trans);
                    res = withdrawalLimitService.SetWithdrawalLimit(custId, userName, siteId, prizeAmount, null, eventId.ToString(), out var afterWithdrawalLimit, conn, trans);
                }
                trans.Commit();
            }

            return result;
        }

        public IEnumerable<CustGiftList> GetCustDrawPrizeList(int eventId, int siteId, int custId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<CustGiftList>("Member_CustDrawPrizeList_Get", new { EventId = eventId, SiteId = siteId, CustId = custId }, commandType: CommandType.StoredProcedure);
        }
        public IEnumerable<PrizeList> GetDrawPrizeList(int eventId, int currencyId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<PrizeList>("Member_DrawPrizeList_Get", new { EventId = eventId, CurrencyId = currencyId }, commandType: CommandType.StoredProcedure);
        }
        public IEnumerable<PrizeDisPlayList> GetDrawPrizeDisPlaySetting(int eventId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<PrizeDisPlayList>("Member_DrawPrizeDisPlaySetting_Get", new { EventId = eventId }, commandType: CommandType.StoredProcedure);
        }
        public void GetLeaderboardEventCode(int siteId, out string eventCodeNow, out string eventCodeLast)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            var p = new DynamicParameters(new { SiteId = siteId });
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
        public EventDrawInfo GetEventDrawInfo(int eventId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            var p = new DynamicParameters(new { EventId = eventId });
            p.Add("EventName", dbType: DbType.String, direction: ParameterDirection.Output);
            p.Add("ConditionSP", dbType: DbType.String, direction: ParameterDirection.Output);
            conn.Execute("Member_EventDraw_Get", p, commandType: CommandType.StoredProcedure);
            return new EventDrawInfo
            {
                EventName = p.Get<string>("EventName"),
                ConditionSp = p.Get<string>("ConditionSP")
            };
        }

        public bool CheckEventCondition(string conditionSP, int eventId, int custId, int currencyId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            var p = new DynamicParameters(new { EventId = eventId, CustId = custId, CurrencyId = currencyId });
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            conn.Execute(conditionSP, p, commandType: CommandType.StoredProcedure);
            var Result = p.Get<int?>("Result") ?? 0;
            return Result > 0;
        }
        public async Task<IEnumerable<AngPowTicket>> GetTickets(int custId, int currencyId)
        {
            var p = new DynamicParameters(new { _CustId = custId, _CurrencyId = currencyId });
            var tickets = await _reportDb.QueryAsync<AngPowTicket>("Member_EventsTickets_Get", p);
            return tickets;
        }
        public int GetDrawPrizeMessage(int eventId, string langId, out string tital, out string message)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            var p = new DynamicParameters(new { EventId = eventId, LangId = langId });
            p.Add("Tital", dbType: DbType.String, direction: ParameterDirection.Output);
            p.Add("Message", dbType: DbType.String, direction: ParameterDirection.Output);
            var rtn = conn.Execute("Member_DrawPrizeMessage_Get", p, commandType: CommandType.StoredProcedure);
            tital = p.Get<String>("Tital");
            message = p.Get<String>("Message");
            return rtn;
        }
    }
}
