using MemberSite.Service.DAL;
using MemberSite.Service.Model;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MemberSite.Service.BLL
{
    public class EventsBLL
    {
        private readonly EvnetsService evnetsService;
        private readonly IMemoryCache cache;
        public static string MaskName(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            string pattern = @"(?<=[\w]{2})[\w-\._\+%]*(?=[\w]{2})";
            if (input.Length < 6)
                pattern = @"(?<=[\w]{1})[\w-\._\+%]*(?=[\w]{1})";
            return Regex.Replace(input, pattern, m => new string('*', m.Length));
        }
        public EventsBLL(EvnetsService evnetsService, IMemoryCache cache)
        {
            this.evnetsService = evnetsService;
            this.cache = cache;

        }
        public IEnumerable<Leaderboard> GetLeaderboardList(int lBCategoryId, int siteId, int currencyId, int topNum, int beforeDays)
        {
            var leaderboard = evnetsService.GetLeaderboardList(lBCategoryId, siteId, currencyId, topNum, beforeDays);
            foreach (var L in leaderboard)
            {
                L.UserName = MaskName(L.UserName);
            }
            return leaderboard;

        }
        public bool GetCustRank(int lBCategoryId, int custId, int beforeDays, out int custRank, out int point)
        {
            return evnetsService.GetCustRank(lBCategoryId, custId, beforeDays, out custRank, out point);
        }
        
        public IEnumerable<Leaderboard> GetLeaderboardListWithCache(int lBCategoryId, int siteId, int currencyId, int topNum, int beforeDays)
        {
            string key = $"Leaderboard_{lBCategoryId}_{siteId}_{currencyId}_{topNum}_{beforeDays}";
            var leaderboard = cache.Get<IEnumerable<Leaderboard>>(key);
            if (leaderboard != null)
            {
                return leaderboard;
            }
            leaderboard = evnetsService.GetLeaderboardList(lBCategoryId, siteId, currencyId, topNum, beforeDays);
            foreach (var L in leaderboard)
            {
                L.UserName = MaskName(L.UserName);
            }
            if (leaderboard != null)
            {
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(10));
                cache.Set(key, leaderboard, cacheEntryOptions);
            }

            return leaderboard;
        }
        public bool GetCustRankWithCache(int lBCategoryId, int custId, int beforeDays, out int custRank, out int point)
        {
            string key = $"Leaderboard_{lBCategoryId}_{custId}_{beforeDays}";
            var leaderboardCustRank = cache.Get<LeaderboardCustRank>(key);
            if (leaderboardCustRank != null)
            {
                custRank = leaderboardCustRank.CustRank;
                point = leaderboardCustRank.point;
                return true;
            }
            bool rtn = evnetsService.GetCustRank(lBCategoryId, custId, beforeDays, out custRank, out point);
            if (rtn)
            {
                leaderboardCustRank = new LeaderboardCustRank
                {
                    CustRank = custRank,
                    point = point
                };
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(10));
                cache.Set(key, leaderboardCustRank, cacheEntryOptions);
            }

            return rtn;
        }
    }
}
