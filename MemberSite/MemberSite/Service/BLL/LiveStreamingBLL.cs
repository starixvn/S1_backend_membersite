using System.Collections.Generic;
using MemberSite.Service.DAL;
using MemberSite.Service.Model;

namespace MemberSite.Service.BLL
{
    public class LiveStreamingBLL
    {
        private readonly LiveStreamingService liveStreamingService;

        public LiveStreamingBLL(LiveStreamingService liveStreamingService)
        {
            this.liveStreamingService = liveStreamingService;
        }

        public IEnumerable<LiveStreamingData> GetLiveStreamingList(int SiteId, string LangId)
        {
            var liveStreaming = new List<LiveStreamingData>();
            var liveStreaminglist = liveStreamingService.GetLiveStreamingList();
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
