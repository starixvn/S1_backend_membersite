using MemberSite.Service.DAL;
using MemberSite.Service.Model;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using MemberSite.Service.Library;

namespace MemberSite.Service.BLL
{
    public class EventsBLL
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
 
        public LeaderboardCustRank GetCustRank(int lBCategoryId, int custId, int beforeDays)
        {
            _eventsService.GetCustRank(lBCategoryId, custId, beforeDays, out var custRank, out var point);
            return new LeaderboardCustRank
            {
                CustRank = custRank,
                point = point
            };
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

        private IEnumerable<Leaderboard> GetLeaderboardList(int lBCategoryId, string eventCode, int siteId, int currencyId, int topNum, int beforeDays)
        {
            var startTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);  // 當月份1號開始
            var endTime = DateTime.Now.AddDays(beforeDays * -1).Date.AddDays(1).AddMilliseconds(-1); 

            if (startTime > endTime)
            {
                (startTime, endTime) = (endTime, startTime);
                startTime = startTime.Date;
            }
            
            var leaderboard = _eventsService.GetLeaderboardList(lBCategoryId, eventCode, siteId, currencyId, topNum, startTime ,endTime);
            var leaderBoardYesterday = _eventsService.GetLeaderboardList(lBCategoryId, eventCode, siteId, currencyId, topNum, startTime ,endTime.AddDays(-1) ).ToList();
            
            if (leaderBoardYesterday.Any())
            {
                var count = 0;
                foreach (var data in leaderboard)
                {
                    count++;
                    var item = leaderBoardYesterday.FirstOrDefault(x => x.CustId == data.CustId);
                    if (item == null)
                        data.Status = 1;
                    data.Status = -count.CompareTo(leaderBoardYesterday.IndexOf(item) + 1);
                }
            }
            
            return leaderboard;
        }

        private IEnumerable<Leaderboard> GetLeaderboardListMonth(int lBCategoryId, string eventCode, int siteId, int currencyId, int topNum, DateTime selectDays)
        {
            var endTime = new DateTime(selectDays.Year, selectDays.Month, 1).AddMonths(1).AddDays(-1);
            var leaderboard = _eventsService.GetLeaderboardList(lBCategoryId, eventCode, siteId, currencyId, topNum, selectDays ,endTime);
            
            return leaderboard;
        }
        
        private LeaderboardCustRank GetCustRankV3(int lBCategoryId, string eventCode, int custId, int beforeDays)
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
 
        public IEnumerable<Leaderboard> GetLeaderboardListWithCache(string eventCode, int lBCategoryId, int siteId, int currencyId, int topNum, int beforeDays)
        {
            var key = $"Leaderboard_{lBCategoryId}_{eventCode}_{siteId}_{currencyId}_{topNum}_{beforeDays}";
            return _cacheHelper.TryGetOrCreate(key,
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(1)),
                i => GetLeaderboardList(lBCategoryId, eventCode, siteId, currencyId, topNum, beforeDays));
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

        public IEnumerable<Prize> CheckDrawPrize(ref int eventId, int custId, int currencyId, out int prizeId, out int result)
        {
            return _eventsService.CheckDrawPrize(ref eventId, custId, currencyId, out prizeId, out result);
        }
        
        public int DrawPrize(ref int eventId, int custId, int currencyId, string userName, int siteId, out int result, out Prize drawPrize)
        {

            return _eventsService.DrawPrize(ref eventId, custId, currencyId, userName, siteId, out result, out drawPrize);
        }
        public IEnumerable<CustGiftList> GetCustDrawPrizeList(int eventId, int siteId, int custId)
        {
            return _eventsService.GetCustDrawPrizeList(eventId, siteId, custId);
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

