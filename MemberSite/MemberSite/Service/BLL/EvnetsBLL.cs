using MemberSite.Service.DAL;
using MemberSite.Service.Model;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemberSite.Service.BLL
{
    public class EvnetsBLL
    {
        private readonly EvnetsService evnetsService;
        private readonly IMemoryCache cache;

        public EvnetsBLL(EvnetsService evnetsService, IMemoryCache cache)
        {
            this.evnetsService = evnetsService;
            this.cache = cache;

        }
        public IEnumerable<Leaderboard> GetLeaderboardList(int lBCategoryId, int siteId, int topNum, int beforeDays)
        {
            return evnetsService.GetLeaderboardList(lBCategoryId, siteId, topNum, beforeDays);
        }
        public bool GetCustRank(int lBCategoryId, int custId, int beforeDays, out int custRank)
        {
            return evnetsService.GetCustRank(lBCategoryId, custId, beforeDays, out custRank);
        }
        public IEnumerable<Leaderboard> GetLeaderboardListWithCache(int lBCategoryId, int siteId, int topNum, int beforeDays)
        {
            string key = $"Leaderboard_{lBCategoryId}_{siteId}_{topNum}_{beforeDays}";
            var leaderboard = cache.Get<IEnumerable<Leaderboard>>(key);
            if (leaderboard != null)
            {
                return leaderboard;
            }
            leaderboard = evnetsService.GetLeaderboardList(lBCategoryId, siteId, topNum, beforeDays);
            if (leaderboard != null)
            {
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(10));
                cache.Set(key, leaderboard, cacheEntryOptions);
            }

            return leaderboard;
        }
        public bool GetCustRankWithCache(int lBCategoryId, int custId, int beforeDays, out int custRank)
        {
            string key = $"Leaderboard_{lBCategoryId}_{custId}_{beforeDays}";
            var leaderboardCustRank = cache.Get<LeaderboardCustRank>(key);
            if (leaderboardCustRank != null)
            {
                custRank = leaderboardCustRank.CustRank;
                return true;
            }
            bool rtn = evnetsService.GetCustRank(lBCategoryId, custId, beforeDays, out custRank);
            if (rtn)
            {
                leaderboardCustRank = new LeaderboardCustRank();
                leaderboardCustRank.CustRank = custRank;
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(10));
                cache.Set(key, leaderboardCustRank, cacheEntryOptions);
            }

            return rtn;
        }
    }
}
