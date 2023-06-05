using MemberSite.Service.DAL;
using MemberSite.Service.Model;
using Microsoft.Extensions.Caching.Memory;
using System.Text.RegularExpressions;
using MemberSite.Service.Library;
using Newtonsoft.Json;

namespace MemberSite.Service.BLL
{
    public class EventsBLL
    {
        private readonly EventsService _eventsService;
        private readonly CacheHelper _cacheHelper;
        private readonly ICacheHelper _redisHelper;
        
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
            _redisHelper = provider.GetRequiredService<ICacheHelper>();
        }              
        
        public IEnumerable<Leaderboard> GetLeaderboardList(string eventCode, int lBCategoryId, int siteId, int currencyId, int topNum, int beforeDays)
        {
            var list = GetLeaderboardListWithCache(eventCode, lBCategoryId, siteId, currencyId, beforeDays);
            return list.OrderByDescending(x => x.WTurnover).Take(topNum);
        }

        public IEnumerable<Leaderboard> GetLeaderboardListWithCache(string eventCode, int lBCategoryId, int siteId, int currencyId, int beforeDays)
        {
            var key = $"LeaderboardListDays:{lBCategoryId}:{eventCode}:{siteId}:{currencyId}:{beforeDays}";
            var json = _redisHelper.StringGet(key);
            return JsonConvert.DeserializeObject<IEnumerable<Leaderboard>>(json);
        }

        public IEnumerable<Leaderboard> GetLeaderboardListMonth(string eventCode, int lBCategoryId, int siteId, int currencyId, int topNum, DateTime selectDays)
        {           
            var list = GetLeaderboardListMonthWithCache(eventCode, lBCategoryId, siteId, currencyId, selectDays.Date.ToString("yyyyMM"));
            return list.OrderByDescending(x => x.WTurnover).Take(topNum);
        }

        public IEnumerable<Leaderboard> GetLeaderboardListMonthWithCache(string eventCode, int lBCategoryId, int siteId, int currencyId, string yyyyMM)
        {
            var key = $"LeaderboardListMonth:{lBCategoryId}:{eventCode}:{siteId}:{currencyId}:{yyyyMM}";
            var json = _redisHelper.StringGet(key);
            return JsonConvert.DeserializeObject<IEnumerable<Leaderboard>>(json);
        }

        public LeaderboardCustRank GetCustRank(int lBCategoryId, string eventCode, int siteId, int currencyId ,int custId, int beforeDays)
        {
            var list = GetLeaderboardListWithCache(eventCode, lBCategoryId, siteId, currencyId, beforeDays);
            var rank = 0;

            foreach (var data in list.OrderByDescending(x => x.WTurnover))
            {
                rank++;
                if (data.CustId == custId)
                {
                    return new LeaderboardCustRank
                    {
                        CustRank = rank,
                        point = data.WTurnover
                    };
                }
            }

            return new LeaderboardCustRank
            {
                CustRank = 0,
                point = 0
            };
        }
        
        public (int custRank , decimal point) GetCustRankWithCacheMonth(int lBCategoryId, string eventCode,  int siteId, int currencyId ,int custId, DateTime selectDays)
        {
            var list = GetLeaderboardListMonthWithCache(eventCode, lBCategoryId, siteId, currencyId, selectDays.Date.ToString("yyyyMM"));
            var rank = 0;
            foreach (var data in list.OrderByDescending(x => x.WTurnover))
            {
                rank++;
                if (data.CustId == custId)
                {
                    return (rank,data.WTurnover);
                }
            }
            
            return (0, 0);
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

        private IEnumerable<PrizeDisPlayList> GetDrawPrizeDisPlayList(int eventId, int currencyId)
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

        private EventDrawInfo GetEventDrawInfo(int eventId)
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

        private DrawPrizeMessage GetDrawPrizeMessage(int eventId, string langId)
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

