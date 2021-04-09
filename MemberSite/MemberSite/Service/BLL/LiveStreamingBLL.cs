using System;
using System.Collections.Generic;
using MemberSite.Service.DAL;
using MemberSite.Service.Model;
using Microsoft.Extensions.Caching.Memory;

namespace MemberSite.Service.BLL
{
    public class LiveStreamingBLL
    {
        private readonly IMemoryCache cache;
        private readonly LiveStreamingService liveStreamingService;

        public LiveStreamingBLL(IMemoryCache cache, LiveStreamingService liveStreamingService)
        {
            this.cache = cache;
            this.liveStreamingService = liveStreamingService;
        }

        public IEnumerable<LiveStreamingData> GetLiveStreamingList(int SiteId, string LangId)
        {
            var liveStreaming = new List<LiveStreamingData>();

            string key = $"LiveStreamingList_{SiteId}";
            IEnumerable<LiveStreaming> liveStreaminglist = cache.Get<IEnumerable<LiveStreaming>>(key);
            if (liveStreaminglist == null)
            {
                liveStreaminglist = liveStreamingService.GetLiveStreamingList();
                if (liveStreaminglist != null)
                {
                    cache.Set(key, liveStreaminglist, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(30)));
                }
            }

            string home;
            string away;
            string league;

            foreach (var liveStreamingdetail in liveStreaminglist)
            {

                if (LangId == "th")
                {
                    home = liveStreamingdetail.HomeTH;
                    away = liveStreamingdetail.AwayTH;
                    league = liveStreamingdetail.LeagueTH;
                }
                else if (LangId == "cs")
                {
                    home = liveStreamingdetail.HomeCH;
                    away = liveStreamingdetail.AwayCH;
                    league = liveStreamingdetail.LeagueCH;
                }
                else
                {
                    home = liveStreamingdetail.Home;
                    away = liveStreamingdetail.Away;
                    league = liveStreamingdetail.League;
                }

                liveStreaming.Add(new LiveStreamingData()
                {
                    MatchID = liveStreamingdetail.MatchID,
                    Channel = liveStreamingdetail.Channel,
                    TimeStart = liveStreamingdetail.TimeStart,
                    TimeStop = liveStreamingdetail.TimeStop,
                    Name = liveStreamingdetail.Name,
                    Home = home,
                    Away = away,
                    Type = liveStreamingdetail.Type,
                    League = league,
                    NowPlaying = liveStreamingdetail.NowPlaying,
                    IsLive = liveStreamingdetail.IsLive,
                    State = liveStreamingdetail.State,
                    HomeScore = liveStreamingdetail.HomeScore,
                    AwayScore = liveStreamingdetail.AwayScore,

                });
            }
            return liveStreaming;
        }
    }
}
