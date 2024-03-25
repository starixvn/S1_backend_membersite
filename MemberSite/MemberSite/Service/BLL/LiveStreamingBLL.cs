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
        private readonly HotEventBLL _hotEventBLL;

        public LiveStreamingBll(IServiceProvider provider)
        {
            _liveStreamingService = provider.GetRequiredService<LiveStreamingService>();
            _cacheHelper = provider.GetRequiredService<CacheHelper>();
            _hotEventBLL = provider.GetRequiredService<HotEventBLL>();
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

        public async Task<IEnumerable<SportLiveStreamingResponse>> GetSportLiveStreaming(int siteId, int currencyId, string langId, ServerInfo serverInfo)
        {
            var sportLiveStreamingList = await _liveStreamingService.GetSportLiveStreaming(siteId, langId);

            if (!sportLiveStreamingList.Any() && langId != "en")
            {
                sportLiveStreamingList = await _liveStreamingService.GetSportLiveStreaming(siteId, "en");
            }

            var sportTeamsInfo = await _hotEventBLL.GetSportTeamsInfo();
            var sportLeaguesInfo = await _hotEventBLL.GetSportLeaguesInfo();

            var response = sportLiveStreamingList.Where(x => x.ICurrencyList.Any(y => y == currencyId)).Select(r => new SportLiveStreamingResponse
            {
                SportLeaguesTitle = r.SportLeaguesTitle,
                MatchId = r.MatchId,
                TpId = r.TpId,
                GameId = r.GameId,
                HomeTeamName = r.HomeTeamName,
                AwayTeamName = r.AwayTeamName,
                BattleTime = r.BattleTime,
                HomeOdds = r.HomeOdds,
                AwayOdds = r.AwayOdds,
                Handicap = r.Handicap,
                BackgroundPic = _hotEventBLL.GetBackgroundPic(serverInfo, sportLeaguesInfo, r),
                HomeTeamPic = _hotEventBLL.GetHomeTeamPic(serverInfo, sportLeaguesInfo, r, sportTeamsInfo),
                AwayTeamPic = _hotEventBLL.GetAwayTeamPic(serverInfo, sportLeaguesInfo, r, sportTeamsInfo)
            }).Take(5);

            return response;
        }
    }
}
