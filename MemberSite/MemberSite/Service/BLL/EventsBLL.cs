using MemberSite.Service.DAL;
using MemberSite.Service.Model;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using MemberSite.Service.Library;
using MemberSite.Service.BLL.Interface;

namespace MemberSite.Service.BLL
{
    public class EventsBLL : IEventsBLL
    {
        private readonly EventsService _eventsService;
        private readonly CacheHelper _cacheHelper;
        public string MaskName(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var pattern = @"(?<=[\w]{2})[\w-\._\+%]*(?=[\w]{2})";
            if (input.Length < 6)
                pattern = @"(?<=[\w]{1})[\w-\._\+%]*(?=[\w]{1})";
            return Regex.Replace(input, pattern, m => new string('*', m.Length));
        }
        public EventsBLL(IServiceProvider provider)
        {
            _eventsService = provider.GetRequiredService<EventsService>();
            _cacheHelper = provider.GetRequiredService<CacheHelper>();
        }
        public IEnumerable<Leaderboard> GetLeaderboardList(int lBCategoryId, int siteId, int currencyId, int topNum, int beforeDays)
        {
            var leaderboard = _eventsService.GetLeaderboardList(lBCategoryId, siteId, currencyId, topNum, beforeDays);
            return leaderboard;

        }
        public LeaderboardCustRank GetCustRank(int lBCategoryId, int custId, int beforeDays)
        {
            _eventsService.GetCustRank(lBCategoryId, custId, beforeDays, out var custRank, out var point);
            return new LeaderboardCustRank
            {
                CustRank = custRank,
                point = point
            };
        }

        public IEnumerable<Leaderboard> GetLeaderboardListWithCache(int lBCategoryId, int siteId, int currencyId, int topNum, int beforeDays)
        {
            var key = $"Leaderboard_{lBCategoryId}_{siteId}_{currencyId}_{topNum}_{beforeDays}";
            return _cacheHelper.TryGetOrCreate(key,
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(10)),
                i => GetLeaderboardList(lBCategoryId, siteId, currencyId, topNum, beforeDays));
        }
        public void GetCustRankWithCache(int lBCategoryId, int custId, int beforeDays, out int custRank, out decimal point)
        {
            var key = $"Leaderboard_{lBCategoryId}_{custId}_{beforeDays}";
            var result = _cacheHelper.TryGetOrCreate(key,
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(10)),
                i => GetCustRank(lBCategoryId, custId, beforeDays));

            custRank = result.CustRank;
            point = result.point;
        }

        public IEnumerable<Leaderboard> GetLeaderboardListV3(int lBCategoryId, string eventCode, int siteId, int currencyId, int topNum, int beforeDays)
        {
            var leaderboard = _eventsService.GetLeaderboardListV3(lBCategoryId, eventCode, siteId, currencyId, topNum, beforeDays);
            return leaderboard;

        }

        public IEnumerable<Leaderboard> GetLeaderboardListV4(int lBCategoryId, string eventCode, int siteId, int currencyId, int topNum, int beforeDays)
        {
            var leaderboard = _eventsService.GetLeaderboardListV3(lBCategoryId, eventCode, siteId, currencyId, topNum, beforeDays);
            var leaderboardyesterday = _eventsService.GetLeaderboardListV3(lBCategoryId, eventCode, siteId, currencyId, topNum, beforeDays + 1).ToList();
            if (leaderboardyesterday.Any())
            {
                var count = 0;
                foreach (var data in leaderboard)
                {
                    count++;
                    var item = leaderboardyesterday.FirstOrDefault(x => x.CustId == data.CustId);
                    if (item == null)
                        data.Status = 1;
                    data.Status = -count.CompareTo(leaderboardyesterday.IndexOf(item) + 1);

                }
            }
            return leaderboard;

        }
        public IEnumerable<Leaderboard> GetLeaderboardListMonth(int lBCategoryId, string eventCode, int siteId, int currencyId, int topNum, DateTime selectDays)
        {
            var leaderboard = _eventsService.GetLeaderboardListMonth(lBCategoryId, eventCode, siteId, currencyId, topNum, selectDays);
            return leaderboard;

        }
        public LeaderboardCustRank GetCustRankV3(int lBCategoryId, string eventCode, int custId, int beforeDays)
        {
            _eventsService.GetCustRankV3(lBCategoryId, eventCode, custId, beforeDays, out var custRank, out var point);
            return new LeaderboardCustRank
            {
                CustRank = custRank,
                point = point
            };
        }
        public LeaderboardCustRank GetCustRankMonth(int lBCategoryId, string eventCode, int custId, DateTime selectDays)
        {
            _eventsService.GetCustRankMonth(lBCategoryId, eventCode, custId, selectDays, out var custRank, out var point);
            return new LeaderboardCustRank
            {
                CustRank = custRank,
                point = point
            };
        }
        public IEnumerable<Leaderboard> GetLeaderboardListWithCacheV2(string eventCode, int lBCategoryId, int siteId, int currencyId, int topNum, int beforeDays)
        {
            var key = $"Leaderboard_{lBCategoryId}_{eventCode}_{siteId}_{currencyId}_{topNum}_{beforeDays}";
            return _cacheHelper.TryGetOrCreate(key,
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(10)),
                i => GetLeaderboardListV3(lBCategoryId, eventCode, siteId, currencyId, topNum, beforeDays));
        }
        public IEnumerable<Leaderboard> GetLeaderboardListWithCacheV3(string eventCode, int lBCategoryId, int siteId, int currencyId, int topNum, int beforeDays)
        {
            var key = $"Leaderboard_{lBCategoryId}_{eventCode}_{siteId}_{currencyId}_{topNum}_{beforeDays}_v3";
            return _cacheHelper.TryGetOrCreate(key,
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(10)),
                i => GetLeaderboardListV4(lBCategoryId, eventCode, siteId, currencyId, topNum, beforeDays));
        }

        public IEnumerable<Leaderboard> GetLeaderboardListMonthWithCache(string eventCode, int lBCategoryId, int siteId, int currencyId, int topNum, DateTime selectDays)
        {
            var key = $"Leaderboard_{lBCategoryId}_{eventCode}_{siteId}_{currencyId}_{topNum}_{selectDays.Date}";
            return _cacheHelper.TryGetOrCreate(key,
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(10)),
                i => GetLeaderboardListMonth(lBCategoryId, eventCode, siteId, currencyId, topNum, selectDays));
        }
        public void GetLeaderboardEventCode(int siteId, out string eventCodeNow, out string eventCodeLast)
        {
            _eventsService.GetLeaderboardEventCode(siteId, out eventCodeNow, out eventCodeLast);
        }

        public LeaderboardInfo GetLeaderboardInfo(string eventCode, int siteId, int currencyId)
        {
            var events = new List<RewardInfo>();
            var eventslist = _eventsService.GetLeaderboardInfo(eventCode, siteId, currencyId);
            Dictionary<string, string> TitalList = null;
            if (eventslist.Tital != null)
                TitalList = JsonConvert.DeserializeObject<Dictionary<string, string>>(eventslist.Tital);
            Dictionary<string, Dictionary<string, string>> RewardDESCList = null;
            if (eventslist.RewardDESC != null)
                RewardDESCList = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(eventslist.RewardDESC);
            foreach (var eventsdetail in eventslist.RewardInfolist)
            {
                if (eventsdetail.DetailUrl != null && eventsdetail.Rewards != null)
                {
                    var detailUrlList = JsonConvert.DeserializeObject<Dictionary<string, string>>(eventsdetail.DetailUrl);
                    var rewardsList = JsonConvert.DeserializeObject<Dictionary<string, List<Dictionary<string, string>>>>(eventsdetail.Rewards);

                    events.Add(new RewardInfo()
                    {

                        CurrencyId = eventsdetail.CurrencyId,
                        CategoryId = eventsdetail.CategoryId,
                        RewardsTotal = eventsdetail.RewardsTotal,
                        DetailUrlList = detailUrlList,
                        RewardsList = rewardsList
                    });

                }
            }

            var newevents = new LeaderboardInfo()
            {
                EventCode = eventslist.EventCode,
                TitalList = TitalList,
                LobbyPic = eventslist.LobbyPic,
                RewardPic = eventslist.RewardPic,
                RewardDESCList = RewardDESCList,
                StartTime = eventslist.StartTime,
                EndTime = eventslist.EndTime,
                RewardInfolist = events
            };

            return newevents;
        }
        public LeaderboardInfo GetLeaderboardInfoWithCache(string eventCode, int siteId, int currencyId)
        {
            var key = $"tLeaderboardIInfo_{eventCode}_{siteId}_{currencyId}";
            return _cacheHelper.TryGetOrCreate(key,
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(60)),
                i => GetLeaderboardInfo(eventCode, siteId, currencyId));
        }
        public void GetCustRankWithCacheV2(int lBCategoryId, string eventCode, int custId, int beforeDays, out int custRank, out decimal point)
        {
            var key = $"Leaderboard_{lBCategoryId}_{eventCode}_{custId}_{beforeDays}";
            var result = _cacheHelper.TryGetOrCreate(key,
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(10)),
                i => GetCustRankV3(lBCategoryId, eventCode, custId, beforeDays));

            custRank = result.CustRank;
            point = result.point;
        }

        public void GetCustRankWithCacheMonth(int lBCategoryId, string eventCode, int custId, DateTime selectDays, out int custRank, out Decimal point)
        {
            var key = $"Leaderboard_{lBCategoryId}_{eventCode}_{custId}_{selectDays}";
            var result = _cacheHelper.TryGetOrCreate(key,
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(10)),
                i => GetCustRankMonth(lBCategoryId, eventCode, custId, selectDays));

            custRank = result.CustRank;
            point = result.point;
        }
        public IEnumerable<Prize> CheckDrawPrize(ref int eventId, int siteId, int custId, int currencyId, out int prizeId, out int result)
        {
            return _eventsService.CheckDrawPrize(ref eventId, siteId, custId, currencyId, out prizeId, out result);
        }



        public async Task<(int errorCode, int ticketCount)> CheckDrawPrize(int custId, int currencyId) 
        {
            var ticketCount = 0;
            var tickets = await _eventsService.GetTickets(custId, currencyId);
            if (!tickets.Any()) return (102, ticketCount);

            ticketCount = tickets.Count(a => a.IsUsed == false);
            if (ticketCount == 0) return (103, ticketCount);

            return (0, ticketCount);
        }
        public int DrawPrize(ref int eventId, int custId, int currencyId, string userName, int siteId, out int result, out Prize drawPrize)
        {

            return _eventsService.DrawPrize(ref eventId, custId, currencyId, userName, siteId, out result, out drawPrize);
        }

        public async Task<(int error, Prize prize)> DrawPrize(int eventId, int custId, int currencyId, string userName, int siteId)
        {
            var res = 0;
            var drawPrize = new Prize();
            //檢查今日有無票
            var tickets = await _eventsService.GetTickets(custId, currencyId);
            
            if (!tickets.Any()) return (102, null);

            var ticket = tickets.FirstOrDefault(a => a.IsUsed == false);
            if (ticket == null) return (103, null);

            var prizes = await _eventsService.GetPrizes(eventId, currencyId);
            var todayPrize = await _eventsService.GetCustPrizeOnToday(eventId, custId, currencyId);
            // 如果今日已中獎後面給空紅包
            if (todayPrize != null) 
            {
                drawPrize = prizes.SingleOrDefault(a => a.Amount == 0);
            }
            else
            {
                var quota = prizes.Sum(a => a.Quota);

                if (quota > 0)
                {
                    var r = new Random();
                    var LuckyNum = r.Next(quota) + 1;
                    var LuckyCnt = 0;

                    foreach (var prize in prizes.Where(x => x.Quota > 0))
                    {
                        drawPrize = prize;
                        LuckyCnt += prize.Quota;
                        if (LuckyNum <= LuckyCnt)
                        {
                            break;
                        }
                    }
                }
                //獎項抽完了,給最小獎
                if (drawPrize == null)
                {
                    var prize = prizes.Aggregate((min, next) => min.Amount > next.Amount ? next : min);
                    drawPrize = prize;
                }
            }
            res = await _eventsService.PayoutPrize(eventId, drawPrize.PrizeId, drawPrize.PrizeName, siteId, custId, userName, currencyId, drawPrize.Amount, ticket.TicketNo, drawPrize.PrizeType);
            return (res, drawPrize);
        }

        public IEnumerable<CustGiftList> GetCustDrawPrizeList(int eventId, int siteId, int custId)
        {
            return _eventsService.GetCustDrawPrizeList(eventId, siteId, custId);
        }

        public async Task<IEnumerable<CustGiftList>> GetTodayCustDrawPrizeList(int eventId, int custId, int currencyId)
        {
            return await _eventsService.GetTodayCustDrawPrizeList(eventId, custId, currencyId);
        }

        public IEnumerable<PrizeDisPlayList> GetDrawPrizeDisPlayList(int eventId, int currencyId)
        {
            var prizeList = _eventsService.GetDrawPrizeList(eventId, currencyId).ToList();
            var disPlaySetting = _eventsService.GetDrawPrizeDisPlaySetting(eventId).ToList();
            if (prizeList.Any() && disPlaySetting.Any())
            {
                disPlaySetting.ForEach(x =>
                {
                    x.PrizeId = prizeList.FirstOrDefault(y => y.PrizeIndex == x.PrizeIndex).PrizeId;
                    x.PrizeName = prizeList.FirstOrDefault(y => y.PrizeIndex == x.PrizeIndex).PrizeName;
                    var PrizeType = prizeList.FirstOrDefault(y => y.PrizeIndex == x.PrizeIndex).PrizeType;

                    //if (currencyId == 51 && PrizeType == 1)
                    //    x.PrizeName =$"VND {x.PrizeName}";
                });
            }

            return disPlaySetting;
        }
        public IEnumerable<PrizeDisPlayList> GetDrawPrizeDisPlayListWithCache(int eventId, int currencyId)
        {
            var key = $"DrawPrizeDisPlaySetting_{eventId}_{currencyId}";
            return _cacheHelper.TryGetOrCreate(key,
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(10)),
                i => GetDrawPrizeDisPlayList(eventId, currencyId));
        }

        public EventDrawInfo GetEventDrawInfo(int eventId)
        {
            return _eventsService.GetEventDrawInfo(eventId);
        }

        public EventDrawInfo GetEventDrawInfoWithCache(int eventId)
        {
            var key = $"EventDrawInfo_{eventId}";
            return _cacheHelper.TryGetOrCreate(key, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(10)), i => GetEventDrawInfo(eventId));
        }
        public bool CheckEventCondition(string conditionSp, int eventId, int custId, int currencyId)
        {
            return _eventsService.CheckEventCondition(conditionSp, eventId, custId, currencyId);
        }
        public async Task<IEnumerable<AngPowTicket>> GetTickets(int custId, int currencyId)
        {
            return await _eventsService.GetTickets(custId, currencyId);
        }
        public DrawPrizeMessage GetDrawPrizeMessage(int eventId, string langId)
        {
            var drawPrizeMessage = new DrawPrizeMessage();
            if (_eventsService.GetDrawPrizeMessage(eventId, langId, out var titalLan, out var messageLan) > 0)
            {
                drawPrizeMessage.Tital = titalLan;
                drawPrizeMessage.Message = messageLan;
            }
            else 
            {
                _eventsService.GetDrawPrizeMessage(eventId, "en", out var tital, out var message); //預設用英文
                drawPrizeMessage.Tital = tital;
                drawPrizeMessage.Message = message;
            }
            return drawPrizeMessage;
        }
        public DrawPrizeMessage GetDrawPrizeMessageWithCache(int eventId, string langId)
        {
            var key = $"GetDrawPrizeMessageWithCache_{eventId}_{langId}";
            return _cacheHelper.TryGetOrCreate(key,
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(10)),
                i => GetDrawPrizeMessage(eventId, langId));
        }
    }
}

