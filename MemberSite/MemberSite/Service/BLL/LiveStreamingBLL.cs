using System.Collections;

using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;

using Microsoft.Extensions.Caching.Memory;

namespace MemberSite.Service.BLL
{
    public class LiveStreamingBll
    {
        private readonly LiveStreamingService _liveStreamingService;
        private readonly CacheHelper _cacheHelper;

        public LiveStreamingBll(IServiceProvider provider)
        {
            _liveStreamingService = provider.GetRequiredService<LiveStreamingService>();
            _cacheHelper = provider.GetRequiredService<CacheHelper>();
        }

        public IEnumerable<LiveStreamingData> GetLiveStreamingList(int siteId, string langId)
        {
            string[] passBetType = { "GOLF" };

            var liveStreaming = new List<LiveStreamingData>();

            var key = $"LiveStreamingList_{siteId}";
            var liveStreamingList = _cacheHelper.TryGetOrCreate(key,
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(30)),
                i => _liveStreamingService.GetLiveStreamingList());

            foreach (var liveStreamingDetail in liveStreamingList)
            {
                string home;
                string away;
                string league;

                if (langId == "th")
                {
                    home = liveStreamingDetail.HomeTH;
                    away = liveStreamingDetail.AwayTH;
                    league = liveStreamingDetail.LeagueTH;
                }
                else if (langId == "cs")
                {
                    home = liveStreamingDetail.HomeCH;
                    away = liveStreamingDetail.AwayCH;
                    league = liveStreamingDetail.LeagueCH;
                }
                else
                {
                    home = liveStreamingDetail.Home;
                    away = liveStreamingDetail.Away;
                    league = liveStreamingDetail.League;
                }

                var exists = ((IList)passBetType).Contains(liveStreamingDetail.Type);

                if (!exists)
                {
                    liveStreaming.Add(new LiveStreamingData()
                    {
                        MatchID = liveStreamingDetail.MatchID,
                        Channel = liveStreamingDetail.Channel,
                        TimeStart = liveStreamingDetail.TimeStart,
                        TimeStop = liveStreamingDetail.TimeStop,
                        Name = liveStreamingDetail.Name,
                        Home = home,
                        Away = away,
                        Type = liveStreamingDetail.Type,
                        League = league,
                        NowPlaying = liveStreamingDetail.NowPlaying,
                        IsLive = liveStreamingDetail.IsLive,
                        State = liveStreamingDetail.State,
                        HomeScore = liveStreamingDetail.HomeScore,
                        AwayScore = liveStreamingDetail.AwayScore,

                    });
                }
            }
            return liveStreaming;
        }
    }
}
