using Dapper;
using MemberSite.Service.DAL.Interface;
using MemberSite.Service.Model;
using System.Data;
using MemberSite.Service.Library;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.DAL
{
    public class EventsService : IEventsService
    {
        private readonly WalletService _walletService;
        private readonly WithdrawalLimitService _withdrawalLimitService;
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;
        
        public EventsService(IServiceProvider provider)
        {
            _walletService = provider.GetRequiredService<WalletService>();
            _withdrawalLimitService = provider.GetRequiredService<WithdrawalLimitService>();
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }
        
        public IEnumerable<Leaderboard> GetLeaderboardList(int lBCategoryId, int siteId, int currencyId, int topNum, int beforeDays)
        {
            return _reportDbClient.Query<Leaderboard>("Member_Leaderboard_LastDateTop_Get", new { LBCategoryId = lBCategoryId, SiteId = siteId, CurrencyId = currencyId, TopNum = topNum, BeforeDays = beforeDays });
        }
        
        public void GetCustRank(int lBCategoryId, int custId, int beforeDays, out int custRank, out Decimal point)
        {
            custRank = 0;
            point = 0;
            
            var p = new DynamicParameters(new { LBCategoryId = lBCategoryId, CustId = custId, BeforeDays = beforeDays });
            p.Add("CustRank", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("Point", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            _reportDbClient.Execute("Member_Leaderboard_CustRank_Get", p);

            custRank = p.Get<int?>("CustRank") ?? 0;
            point = p.Get<decimal?>("Point") ?? 0;
        }

        public IEnumerable<Leaderboard> GetLeaderboardListV3(int lBCategoryId, string eventCode, int siteId, int currencyId, int topNum, int beforeDays)
        {
            var p = new DynamicParameters(new { EventCode = eventCode });
            p.Add("CustRankSP", dbType: DbType.String, direction: ParameterDirection.Output);
            p.Add("LastDateTopSP", dbType: DbType.String, direction: ParameterDirection.Output);
            _reportDbClient.Execute("Member_LeaderboardEventSetting_Get", p);

            var custRankSp = p.Get<string>("CustRankSP");
            var lastDateTopSp = p.Get<string>("LastDateTopSP");
            
            if (lastDateTopSp != null)
                return _reportDbClient.Query<Leaderboard>(lastDateTopSp, new { LBCategoryId = lBCategoryId, EventCode = eventCode, SiteId = siteId, CurrencyId = currencyId, TopNum = topNum, BeforeDays = beforeDays });
            else
                return _reportDbClient.Query<Leaderboard>("Member_Leaderboard_LastDateTopV2_Get", new { LBCategoryId = lBCategoryId, EventCode = eventCode, SiteId = siteId, CurrencyId = currencyId, TopNum = topNum, BeforeDays = beforeDays });
        }


        public IEnumerable<Leaderboard> GetLeaderboardListMonth(int lBCategoryId, string eventCode, int siteId, int currencyId, int topNum, DateTime selectDays)
        {
            return _reportDbClient.Query<Leaderboard>("Member_Leaderboard_TopMonth_Get", new { LBCategoryId = lBCategoryId, EventCode = eventCode, SiteId = siteId, CurrencyId = currencyId, TopNum = topNum, SeletDay = selectDays });
        }
        
        public void GetCustRankV3(int lBCategoryId, string eventCode, int custId, int beforeDays, out int custRank, out Decimal point)
        {
            custRank = 0;
            point = 0;
            var p = new DynamicParameters(new { EventCode = eventCode });
            p.Add("CustRankSP", dbType: DbType.String, direction: ParameterDirection.Output);
            p.Add("LastDateTopSP", dbType: DbType.String, direction: ParameterDirection.Output);
            _reportDbClient.Execute("Member_LeaderboardEventSetting_Get", p);

            var custRankSp = p.Get<string>("CustRankSP");
            var lastDateTopSp = p.Get<string>("LastDateTopSP");

            var p1 = new DynamicParameters(new { LBCategoryId = lBCategoryId, EventCode = eventCode, CustId = custId, BeforeDays = beforeDays });
            p1.Add("CustRank", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p1.Add("Point", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            if (custRankSp != null)
            {
                _reportDbClient.Execute(custRankSp, p1);
            }
            else
            {
                _reportDbClient.Execute("Member_Leaderboard_CustRankV2_Get", p);
            }
            custRank = p1.Get<int?>("CustRank") ?? 0;
            point = p1.Get<decimal?>("Point") ?? 0;
        }

        public void GetCustRankMonth(int lBCategoryId, string eventCode, int custId, DateTime selectDays, out int custRank, out Decimal point)
        {
            custRank = 0;
            point = 0;

            var p1 = new DynamicParameters(new { LBCategoryId = lBCategoryId, EventCode = eventCode, CustId = custId, SeletDay = selectDays });
            p1.Add("CustRank", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p1.Add("Point", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            {
                _reportDbClient.Execute("Member_Leaderboard_CustRank_Month_Get", p1);
            }
            custRank = p1.Get<int?>("CustRank") ?? 0;
            point = p1.Get<decimal?>("Point") ?? 0;

        }

        public IEnumerable<Prize> CheckDrawPrize(ref int eventId, int custId, int currencyId, out int prizeId, out int result)
        {
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            var check = CheckDrawPrize(ref eventId, custId, currencyId, out prizeId, out _, out _, out result, trans);
            trans.Commit();
            conn.Close();
            conn.Dispose();
            return check;
        }
        
        public IEnumerable<Prize> CheckDrawPrize(ref int eventId, int custId, int currencyId, out int prizeId,
            out int quota, out DateTime checkDate, out int result, IDbTransaction trans)
        {
            var p = new DynamicParameters(new { CustId = custId, CurrencyId = currencyId });
            p.Add("EventId", eventId, dbType: DbType.Int32, direction: ParameterDirection.InputOutput);
            p.Add("PrizeId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("Quota", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("CheckDate", dbType: DbType.Date, direction: ParameterDirection.Output);
            p.Add("result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            var rtn = _mainDbClient.Query<Prize>("Member_CheckDrawPrize_Set", trans, p);
            eventId = p.Get<int>("EventId");
            prizeId = p.Get<int>("PrizeId");
            quota = p.Get<int>("Quota");
            checkDate = p.Get<DateTime>("CheckDate");
            result = p.Get<int>("result");

            return rtn;
        }

        public async Task<IEnumerable<Prize>> GetPrizes(int eventId, int currencyId)
        {
            return await _reportDbClient.QueryAsync<Prize>("Member_DrawPrize_Get", new { _EventId = eventId, _CurrencyId = currencyId });
        }

        public async Task ResetPrize(int eventId, int currencyId) 
        {
            await _mainDbClient.ExecuteAsync("Member_DrawPrize_Reset", new { _EventId = eventId, _CurrencyId = currencyId });
        }

        public int DrawPrize(ref int eventId, int custId, int currencyId, string userName, int siteId, out int result, out Prize drawPrize)
        {

            var res = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            
            drawPrize = null;
            decimal prizeAmount = 0;

            var prizes = CheckDrawPrize(ref eventId, custId, currencyId, out var prizeId, out var quota, out var checkDate, out result, trans);

            if (result == 100)
            {
                drawPrize = prizes.FirstOrDefault(x => x.PrizeId == prizeId);
            }
            else if (result == 0)
            {
                if (quota > 0)
                {
                    //指定大獎 Jackpot == 1
                    var bigPrize = prizes.FirstOrDefault(x => x.Jackpot == 1 && x.Quota > 0);
                    //重新分配Quota個數來改變機率
                    if (quota > 1 && bigPrize != null && bigPrize.Quota > 0 && DateTime.Now > (bigPrize.StartTime.AddHours(((float)new TimeSpan(bigPrize.EndTime.Ticks - bigPrize.StartTime.Ticks).TotalHours * ((float)3 / (float)4)))))
                    {

                        var totalTime = (int)new TimeSpan(bigPrize.EndTime.Ticks - bigPrize.StartTime.Ticks).TotalHours;
                        var elapsedTime = (int)new TimeSpan((DateTime.Now.Ticks - bigPrize.StartTime.Ticks)).TotalHours;
                        var totalQuota = 1000;

                        bigPrize.Quota = (int)((float)totalQuota * ((float)elapsedTime / (float)totalTime));
                        if (quota - 1 > 0)
                        {
                            foreach (var prize in prizes.Where(x => x.Quota > 0 && x.Jackpot != 1))
                            {
                                prize.Quota = (int)((float)(totalQuota - bigPrize.Quota) * ((float)prize.Quota / (float)(quota - 1)));
                            }
                        }
                        quota = totalQuota;
                    }
                    var r = new Random();
                    var LuckyNum = r.Next(quota) + 1;
                    var LuckyCnt = 0;

                    foreach (var prize in prizes.Where(x => x.Quota > 0))
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
                    var prize = prizes.Aggregate((min, next) => min.Amount > next.Amount ? next : min);
                    drawPrize = prize;
                    prizeId = prize.PrizeId;
                    prizeAmount = prize.Amount;

                }

                _mainDbClient.Execute("Member_DrawPrize_Set", trans, new { EventId = eventId, PrizeId = prizeId, CustId = custId, DrawDate = checkDate });
                if (drawPrize.PrizeType == 1) //money
                {
                    _walletService.SetBalance(custId, siteId, userName, prizeAmount, 8, $"Prize: {prizeAmount.ToString("#,##0.##")}", eventId.ToString(), out _, out _, trans);
                    _withdrawalLimitService.SetWithdrawalLimit(custId, userName, siteId, prizeAmount, 1  ,null, eventId.ToString(), (int)WithdrawalLimitLogCreateFromEnum.DrawPrize , "System", out var afterWithdrawalLimit, trans);
                }
                trans.Commit();
                conn.Close();
                conn.Dispose();
            }

            return result;
        }

        public async Task<int> PayoutPrize(int eventId, int prizeId, string prizeName, int siteId, int custId, string userName, int currencyId, decimal prizeAmount, int ticketNo, int prizeType, bool isUsed)
        {
            await using var conn = _mainDbClient.GetMySqlConnection();
            await conn.OpenAsync();
            await using var trans = await conn.BeginTransactionAsync();

            var res = await _mainDbClient.ExecuteAsync("Member_DrawPrize_Set_V2", trans, new { _EventId = eventId, _PrizeId = prizeId, _PrizeName = prizeName, _Amount = prizeAmount, _CustId = custId,  _CurrencyId = currencyId, _TicketNo = ticketNo, _IsUsed = isUsed });
            if (prizeType == 1 && prizeAmount > 0)
            {
                var memo = $"Prize: {prizeAmount.ToString("#,##0.##")}";
                res = _walletService.SetBalance(custId, siteId, userName, prizeAmount, 8, memo, eventId.ToString(), out _, out _, trans);
                res = _withdrawalLimitService.SetWithdrawalLimit(custId, userName, siteId, prizeAmount, 1 , memo, eventId.ToString(), (int)WithdrawalLimitLogCreateFromEnum.DrawPrize , "System", out var afterWithdrawalLimit, trans);
            }
            if (res > 0) 
            {
                trans.Commit();
            }
            await conn.CloseAsync();
            await conn.DisposeAsync();

            return res;
        }

        public IEnumerable<CustGiftList> GetCustDrawPrizeList(int eventId, int siteId, int custId)
        {
            return _reportDbClient.Query<CustGiftList>("Member_CustDrawPrizeList_Get", new { EventId = eventId, SiteId = siteId, CustId = custId });
        }
        public async Task<IEnumerable<CustGiftList>> GetTodayCustDrawPrizeList(int eventId, int custId, int currencyId)
        {
            return await _reportDbClient.QueryAsync<CustGiftList>("Member_CustDrawPrizeList_Get_V2", new { _EventId = eventId, _CustId = custId, _CurrencyId = currencyId });
        }
        public IEnumerable<PrizeList> GetDrawPrizeList(int eventId, int currencyId)
        {
            return _reportDbClient.Query<PrizeList>("Member_DrawPrizeList_Get", new { EventId = eventId, CurrencyId = currencyId }, commandType: CommandType.StoredProcedure);
        }
        
        public IEnumerable<PrizeDisPlayList> GetDrawPrizeDisPlaySetting(int eventId)
        {
            return _reportDbClient.Query<PrizeDisPlayList>("Member_DrawPrizeDisPlaySetting_Get", new { EventId = eventId }, commandType: CommandType.StoredProcedure);
        }
        
        public void GetLeaderboardEventCode( int siteId,out string eventCodeNow,out string eventCodeLast)
        {
            var p = new DynamicParameters(new { SiteId = siteId});
            p.Add("NowEventCode", dbType: DbType.String, direction: ParameterDirection.Output);
            p.Add("LastEventCode", dbType: DbType.String, direction: ParameterDirection.Output);
            _reportDbClient.Execute("Member_LeaderboardEvent_Get", p);
            
            eventCodeNow = p.Get<string>("NowEventCode");
            eventCodeLast = p.Get<string>("LastEventCode");
        }
        public LeaderboardInfo GetLeaderboardInfo(string eventCode, int siteId, int currencyId)
        {
            var info = _reportDbClient.QueryFirst<LeaderboardInfo>("Member_LeaderboardEventInfo_Get", new { EventCode = eventCode, SiteId = siteId });
            info.RewardInfolist = _reportDbClient.Query<RewardInfo>("Member_LeaderboardRewardInfo_Get", new { EventCode = eventCode, SiteId = siteId, CurrencyId = currencyId });
            return info;
        }
        public EventDrawInfo GetEventDrawInfo(int eventId)
        {
            var p = new DynamicParameters(new { EventId = eventId });
            p.Add("EventName", dbType: DbType.String, direction: ParameterDirection.Output);
            p.Add("ConditionSP", dbType: DbType.String, direction: ParameterDirection.Output);
            _reportDbClient.Execute("Member_EventDraw_Get", p);
            return new EventDrawInfo
            {
                EventName = p.Get<string>("EventName"),
                ConditionSp = p.Get<string>("ConditionSP")
            };
        }

        public bool CheckEventCondition(string conditionSp, int eventId, int custId, int currencyId)
        {
            var p = new DynamicParameters(new { EventId = eventId, CustId = custId, CurrencyId = currencyId });
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            _reportDbClient.Execute(conditionSp, p);

            var Result = p.Get<int?>("Result") ?? 0;
            return Result > 0;
        }
        
        public async Task<IEnumerable<AngPowTicket>> GetTickets(int custId, int currencyId)
        {
            return await _reportDbClient.QueryAsync<AngPowTicket>("Member_DrawTicket_Get", new { _CustId = custId, _CurrencyId = currencyId });
        }
        
        public async Task<Prize> GetCustPrizeOnToday(int eventId, int custId, int currencyId)
        {
            return await _reportDbClient.QueryFirstAsync<Prize>("Member_DailyDrawPrize_Get", new { _EventId = eventId, _CustId = custId, _CurrencyId = currencyId });
        }
        public int GetDrawPrizeMessage(int eventId, string langId, out string tital, out string message)
        {
            var p = new DynamicParameters(new { EventId = eventId, LangId = langId });
            p.Add("Tital", dbType: DbType.String, direction: ParameterDirection.Output);
            p.Add("Message", dbType: DbType.String, direction: ParameterDirection.Output);
            var rtn = _reportDbClient.Execute("Member_DrawPrizeMessage_Get", p);
            tital = p.Get<string>("Tital");
            message = p.Get<string>("Message");
            return rtn;
        }
    }
}
