using MemberSite.Service.DAL;
using MemberSite.Service.Model;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MemberSite.Service.BLL
{
    public class EventsBLL
    {
        private readonly EventsService eventsService;
        private readonly IMemoryCache cache;
        public string MaskName(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var pattern = @"(?<=[\w]{2})[\w-\._\+%]*(?=[\w]{2})";
            if (input.Length < 6)
                pattern = @"(?<=[\w]{1})[\w-\._\+%]*(?=[\w]{1})";
            return Regex.Replace(input, pattern, m => new string('*', m.Length));
        }
        public EventsBLL(EventsService eventsService, IMemoryCache cache)
        {
            this.eventsService = eventsService;
            this.cache = cache;
        }
        public IEnumerable<Leaderboard> GetLeaderboardList(int lBCategoryId, int siteId, int currencyId, int topNum, int beforeDays)
        {
            var leaderboard = eventsService.GetLeaderboardList(lBCategoryId, siteId, currencyId, topNum, beforeDays);
            return leaderboard;

        }
        public void GetCustRank(int lBCategoryId, int custId, int beforeDays, out int custRank, out Decimal point)
        {
            eventsService.GetCustRank(lBCategoryId, custId, beforeDays, out custRank, out point);
        }
        
        public IEnumerable<Leaderboard> GetLeaderboardListWithCache(int custId ,int lBCategoryId, int siteId, int currencyId, int topNum, int beforeDays)
        {
            var key = $"Leaderboard_{lBCategoryId}_{siteId}_{currencyId}_{topNum}_{beforeDays}";
            var leaderboard = cache.Get<IEnumerable<Leaderboard>>(key);
            if (leaderboard != null)
            {
                return leaderboard;
            }
            leaderboard = GetLeaderboardList(lBCategoryId, siteId, currencyId, topNum, beforeDays);
            
            if (leaderboard != null)
            {
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(10));
                cache.Set(key, leaderboard, cacheEntryOptions);
            }

            return leaderboard;
        }
        public void GetCustRankWithCache(int lBCategoryId, int custId, int beforeDays, out int custRank, out Decimal point)
        {
            var key = $"Leaderboard_{lBCategoryId}_{custId}_{beforeDays}";
            var leaderboardCustRank = cache.Get<LeaderboardCustRank>(key);
            if (leaderboardCustRank != null)
            {
                custRank = leaderboardCustRank.CustRank;
                point = leaderboardCustRank.point;
                return;
            }

            GetCustRank(lBCategoryId, custId, beforeDays, out custRank, out point);

            leaderboardCustRank = new LeaderboardCustRank
            {
                CustRank = custRank,
                point = point
            };

            var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(10));
            cache.Set(key, leaderboardCustRank, cacheEntryOptions);
        }
        public IEnumerable<Leaderboard> GetLeaderboardListV2(int lBCategoryId, string eventCode, int siteId, int currencyId, int topNum, int beforeDays)
        {
            var leaderboard = eventsService.GetLeaderboardListV2(lBCategoryId, eventCode, siteId, currencyId, topNum, beforeDays);
            return leaderboard;

        }
        public void GetCustRankV2(int lBCategoryId, string eventCode, int custId, int beforeDays, out int custRank, out Decimal point)
        {
            eventsService.GetCustRankV2(lBCategoryId, eventCode, custId, beforeDays, out custRank, out point);
        }
        public IEnumerable<Leaderboard> GetLeaderboardListV3(int lBCategoryId, string eventCode, int siteId, int currencyId, int topNum, int beforeDays)
        {
            var leaderboard = eventsService.GetLeaderboardListV3(lBCategoryId, eventCode, siteId, currencyId, topNum, beforeDays);
            return leaderboard;

        }

        public IEnumerable<Leaderboard> GetLeaderboardListV4(int lBCategoryId, string eventCode, int siteId, int currencyId, int topNum, int beforeDays)
        {
            var leaderboard = eventsService.GetLeaderboardListV3(lBCategoryId, eventCode, siteId, currencyId, topNum, beforeDays);
            var leaderboardyesterday = eventsService.GetLeaderboardListV3(lBCategoryId, eventCode, siteId, currencyId, topNum, beforeDays +1).ToList();
            if (leaderboardyesterday.Any())
            {
                int count = 0;
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
            var leaderboard = eventsService.GetLeaderboardListMonth(lBCategoryId, eventCode, siteId, currencyId, topNum, selectDays);
            return leaderboard;

        }
        public void GetCustRankV3(int lBCategoryId, string eventCode, int custId, int beforeDays, out int custRank, out Decimal point)
        {
            eventsService.GetCustRankV3(lBCategoryId, eventCode, custId, beforeDays, out custRank, out point);
        }
        public void GetCustRankMonth(int lBCategoryId, string eventCode, int custId, DateTime selectDays, out int custRank, out Decimal point)
        {
            eventsService.GetCustRankMonth(lBCategoryId, eventCode, custId, selectDays, out custRank, out point);
        }
        public IEnumerable<Leaderboard> GetLeaderboardListWithCacheV2(int custId, string eventCode, int lBCategoryId, int siteId, int currencyId, int topNum, int beforeDays)
        {
            var key = $"Leaderboard_{lBCategoryId}_{eventCode}_{siteId}_{currencyId}_{topNum}_{beforeDays}";
            var leaderboard = cache.Get<IEnumerable<Leaderboard>>(key);
            if (leaderboard != null)
            {
                return leaderboard;
            }
            leaderboard = GetLeaderboardListV3(lBCategoryId, eventCode, siteId, currencyId, topNum, beforeDays);

            if (leaderboard.Any())
            {
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(10));
                cache.Set(key, leaderboard, cacheEntryOptions);
            }

            return leaderboard;
        }
        public IEnumerable<Leaderboard> GetLeaderboardListWithCacheV3(int custId, string eventCode, int lBCategoryId, int siteId, int currencyId, int topNum, int beforeDays)
        {
            var key = $"Leaderboard_{lBCategoryId}_{eventCode}_{siteId}_{currencyId}_{topNum}_{beforeDays}_v3";
            var leaderboard = cache.Get<IEnumerable<Leaderboard>>(key);
            if (leaderboard != null)
            {
                return leaderboard;
            }
            leaderboard = GetLeaderboardListV4(lBCategoryId, eventCode, siteId, currencyId, topNum, beforeDays);

            if (leaderboard.Any())
            {
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(10));
                cache.Set(key, leaderboard, cacheEntryOptions);
            }

            return leaderboard;
        }
        
        public IEnumerable<Leaderboard> GetLeaderboardListMonthWithCache(int custId, string eventCode, int lBCategoryId, int siteId, int currencyId, int topNum, DateTime selectDays)
        {
            var key = $"Leaderboard_{lBCategoryId}_{eventCode}_{siteId}_{currencyId}_{topNum}_{selectDays.Date}";
            var leaderboard = cache.Get<IEnumerable<Leaderboard>>(key);
            if (leaderboard != null)
            {
                return leaderboard;
            }
            leaderboard = GetLeaderboardListMonth(lBCategoryId, eventCode, siteId, currencyId, topNum, selectDays);

            if (leaderboard.Any())
            {
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(10));
                cache.Set(key, leaderboard, cacheEntryOptions);
            }

            return leaderboard;
        }
        public void GetCustRankWithCacheV2(int lBCategoryId, string eventCode, int custId, int beforeDays, out int custRank, out Decimal point)
        {
            var key = $"Leaderboard_{lBCategoryId}_{eventCode}_{custId}_{beforeDays}";
            var leaderboardCustRank = cache.Get<LeaderboardCustRank>(key);
            if (leaderboardCustRank != null)
            {
                custRank = leaderboardCustRank.CustRank;
                point = leaderboardCustRank.point;
                return;
            }

            GetCustRankV3(lBCategoryId, eventCode, custId, beforeDays, out custRank, out point);

            leaderboardCustRank = new LeaderboardCustRank
            {
                CustRank = custRank,
                point = point
            };

            var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(10));
            cache.Set(key, leaderboardCustRank, cacheEntryOptions);
        }
        
        public void GetCustRankWithCacheMonth(int lBCategoryId, string eventCode, int custId, DateTime selectDays, out int custRank, out Decimal point)
        {
            var key = $"Leaderboard_{lBCategoryId}_{eventCode}_{custId}_{selectDays}";
            var leaderboardCustRank = cache.Get<LeaderboardCustRank>(key);
            if (leaderboardCustRank != null)
            {
                custRank = leaderboardCustRank.CustRank;
                point = leaderboardCustRank.point;
                return;
            }

            GetCustRankMonth(lBCategoryId, eventCode, custId, selectDays, out custRank, out point);

            leaderboardCustRank = new LeaderboardCustRank
            {
                CustRank = custRank,
                point = point
            };

            var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(10));
            cache.Set(key, leaderboardCustRank, cacheEntryOptions);
        }
        public IEnumerable<Prize> CheckDrawPrize(ref int eventId, int siteId, int custId, int currencyId, out int prizeId, out int result)
        {
            return eventsService.CheckDrawPrize(ref eventId, siteId, custId, currencyId, out prizeId, out result);
        }
        public int DrawPrize(ref int eventId, int custId, int currencyId, string userName, int siteId, out int result, out Prize drawPrize)
        {
            
            return eventsService.DrawPrize(ref eventId, custId, currencyId, userName, siteId, out result, out drawPrize);
        }
        public IEnumerable<CustGiftList> GetCustDrawPrizeList(int eventId, int siteId, int custId)
        {
            return eventsService.GetCustDrawPrizeList( eventId, siteId, custId);
        }
        public void GetLeaderboardEventCode(int siteId, out string eventCodeNow, out string eventCodeLast)
        {
            eventsService.GetLeaderboardEventCode( siteId, out eventCodeNow, out eventCodeLast);
        }
        public LeaderboardInfo GetLeaderboardInfo(string eventCode, int siteId, int currencyId)
        {
            var events = new List<RewardInfo>();
            var eventslist = eventsService.GetLeaderboardInfo(eventCode, siteId, currencyId);
            Dictionary<string, string> TitalList = null;
            if(eventslist.Tital != null)
                TitalList = JsonConvert.DeserializeObject<Dictionary<string, string>>(eventslist.Tital);
            Dictionary<string, Dictionary<string, string>> RewardDESCList = null;
            if (eventslist.RewardDESC != null)
                RewardDESCList = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(eventslist.RewardDESC);
            foreach (var eventsdetail in eventslist.RewardInfolist)
            {
                if (eventsdetail.DetailUrl != null && eventsdetail.Rewards != null)
                {
                    var detailUrlList = JsonConvert.DeserializeObject<Dictionary<string, string>>(eventsdetail.DetailUrl);
                    var rewardsList = JsonConvert.DeserializeObject<Dictionary<string, List<Dictionary<string,string>>>>(eventsdetail.Rewards);
                   
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

            var newevents = new LeaderboardInfo() {
                EventCode = eventslist.EventCode,
                TitalList = TitalList,
                LobbyPic = eventslist.LobbyPic,
                RewardPic = eventslist.RewardPic,
                RewardDESCList = RewardDESCList,
                StartTime = eventslist.StartTime,
                EndTime = eventslist.EndTime,
                RewardInfolist =events
            };

            return newevents;
        }
        public LeaderboardInfo GetLeaderboardInfoWithCache(string eventCode, int siteId, int currencyId)
        {
            var key = $"tLeaderboardIInfo_{eventCode}_{siteId}_{currencyId}";
            var leaderboardInfo = cache.Get<LeaderboardInfo>(key);
            if (leaderboardInfo != null)
            {
                return leaderboardInfo;
            }

            leaderboardInfo = GetLeaderboardInfo(eventCode, siteId, currencyId);

            if (leaderboardInfo != null)
            {
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(60));
                cache.Set(key, leaderboardInfo, cacheEntryOptions);
            }

            return leaderboardInfo;
        }
        public void GetEventDrawInfo(int eventId, out string eventName, out string conditionSP)
        {
            eventsService.GetEventDrawInfo( eventId, out eventName, out conditionSP);
        }
        public class EventDrawInfo
        {
            public string eventName { get; set; }
            public string conditionSP { get; set; }
        }
        public void GetEventDrawInfoWithCache(int eventId, out string eventName, out string conditionSP)
        {
            var key = $"EventDrawInfo_{eventId}";
            var eventDrawInfo = cache.Get<EventDrawInfo>(key);
            if (eventDrawInfo != null)
            {
                eventName = eventDrawInfo.eventName;
                conditionSP = eventDrawInfo.conditionSP;
                return;
            }
            
            eventsService.GetEventDrawInfo(eventId, out eventName, out conditionSP);

            eventDrawInfo = new EventDrawInfo
            {
                eventName = eventName,
                conditionSP = conditionSP
            };

            var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(30));
            cache.Set(key, eventDrawInfo, cacheEntryOptions);



        }
        public bool CheckEventCondition(string conditionSP, int eventId, int custId, int currencyId)
        {
            return eventsService.CheckEventCondition(conditionSP, eventId, custId, currencyId);
        }
    }
}

