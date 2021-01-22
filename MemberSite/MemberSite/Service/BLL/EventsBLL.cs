using MemberSite.Service.DAL;
using MemberSite.Service.Model;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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

            string pattern = @"(?<=[\w]{2})[\w-\._\+%]*(?=[\w]{2})";
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
            string key = $"Leaderboard_{lBCategoryId}_{siteId}_{currencyId}_{topNum}_{beforeDays}";
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
            string key = $"Leaderboard_{lBCategoryId}_{custId}_{beforeDays}";
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

        public IEnumerable<Leaderboard> GetLeaderboardListWithCacheV2(int custId, string eventCode, int lBCategoryId, int siteId, int currencyId, int topNum, int beforeDays)
        {
            string key = $"Leaderboard_{lBCategoryId}_{eventCode}_{siteId}_{currencyId}_{topNum}_{beforeDays}";
            var leaderboard = cache.Get<IEnumerable<Leaderboard>>(key);
            if (leaderboard != null)
            {
                return leaderboard;
            }
            leaderboard = GetLeaderboardListV2(lBCategoryId, eventCode, siteId, currencyId, topNum, beforeDays);

            if (leaderboard != null)
            {
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(10));
                cache.Set(key, leaderboard, cacheEntryOptions);
            }

            return leaderboard;
        }
        public void GetCustRankWithCacheV2(int lBCategoryId, string eventCode, int custId, int beforeDays, out int custRank, out Decimal point)
        {
            string key = $"Leaderboard_{lBCategoryId}_{eventCode}_{custId}_{beforeDays}";
            var leaderboardCustRank = cache.Get<LeaderboardCustRank>(key);
            if (leaderboardCustRank != null)
            {
                custRank = leaderboardCustRank.CustRank;
                point = leaderboardCustRank.point;
                return;
            }

            GetCustRankV2(lBCategoryId, eventCode, custId, beforeDays, out custRank, out point);

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
                            DetailUrlList = detailUrlList,
                            RewardsList = rewardsList
                        });
                    
                }
            }

            var newevents = new LeaderboardInfo() {
                EventCode = eventslist.EventCode,
                Tital = eventslist.Tital,
                LobbyPic = eventslist.LobbyPic,
                RewardPic = eventslist.RewardPic,
                RewardDESC = eventslist.RewardDESC,
                StartTime = eventslist.StartTime,
                EndTime = eventslist.EndTime,
                RewardInfolist =events
            };

            return newevents;
        }
    }
}
