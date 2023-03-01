using MemberSite.Service.DAL.Interface;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using Microsoft.Extensions.Options;

namespace MemberSite.Service.BLL
{
    public class HotEventBLL
    {
        private readonly IHotEventService _hotEventService;
        private readonly IOptions<Dictionary<string, SiteConfig>> siteConfig;
        private readonly ICacheHelper _redisHelper;

        public HotEventBLL(IServiceProvider provider)
        {
            _hotEventService = provider.GetRequiredService<IHotEventService>();
            siteConfig = provider.GetRequiredService<IOptions<Dictionary<string, SiteConfig>>>();
            _redisHelper = provider.GetRequiredService<ICacheHelper>();
        }

        public async Task<IEnumerable<HotEventResponse>> GetHotEventDataList(int siteId, string langId, ServerInfo serverInfo)
        {
            var hotEventList = await _hotEventService.GetHotEventDataList(siteId, langId);

            if (!hotEventList.Any() && langId != "en") // 如果某個語系沒資料，就抓抓看 en 語系給前台，Ariel的需求 
            {
                hotEventList = await _hotEventService.GetHotEventDataList(siteId, "en");
            }

            var sportTeamsInfo = await GetSportTeamsInfo();
            var sportLeaguesInfo = await GetSportLeaguesInfo();

            var response = hotEventList.Select(r => new HotEventResponse
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
                BackgroundPic = GetBackgroundPic(serverInfo, sportLeaguesInfo, r),
                HomeTeamPic = GetHomeTeamPic(serverInfo, sportLeaguesInfo, r, sportTeamsInfo),
                AwayTeamPic = GetAwayTeamPic(serverInfo, sportLeaguesInfo, r, sportTeamsInfo)
            });

            return response;
        }

        #region private method


        private string GetBackgroundPic(ServerInfo serverInfo, IEnumerable<SportLeaguesInfoDto> sportLeaguesInfo, HotEventDto hotEvent)
        {
            return $"{siteConfig.Value[serverInfo.SiteName].HotEventImageUrl}/{MapSportLeaguesName(sportLeaguesInfo, hotEvent.HomeTeamLeaguesId)}/{serverInfo.SiteName}/Background.jpg";
        }

        private string GetHomeTeamPic(ServerInfo serverInfo, IEnumerable<SportLeaguesInfoDto> sportLeaguesInfo, HotEventDto r, IEnumerable<SportTeamsInfoDto> sportTeamsInfo)
        {
            return $"{siteConfig.Value[serverInfo.SiteName].HotEventImageUrl}/{MapSportLeaguesName(sportLeaguesInfo, r.HomeTeamLeaguesId)}/{MapSportTeamName(sportTeamsInfo, r.HomeTeamLeaguesId, r.HomeTeamId)}.png";
        }

        private string GetAwayTeamPic(ServerInfo serverInfo, IEnumerable<SportLeaguesInfoDto> sportLeaguesInfo, HotEventDto r, IEnumerable<SportTeamsInfoDto> sportTeamsInfo)
        {
            return $"{siteConfig.Value[serverInfo.SiteName].HotEventImageUrl}/{MapSportLeaguesName(sportLeaguesInfo, r.AwayTeamLeaguesId)}/{MapSportTeamName(sportTeamsInfo, r.AwayTeamLeaguesId, r.AwayTeamId)}.png";
        }

        private static string MapSportLeaguesName(IEnumerable<SportLeaguesInfoDto> leaguesList, int sportLeaguesId)
        {
            return leaguesList.Where(r => r.SportLeaguesId == sportLeaguesId)
                .Select(r => r.SportLeaguesName)
                .FirstOrDefault();
        }

        private static string MapSportTeamName(IEnumerable<SportTeamsInfoDto> teams, int sportLeaguesId, int sportTeamId)
        {
            return teams.Where(r => r.SportLeaguesId == sportLeaguesId && r.TeamId == sportTeamId)
                .Select(x => x.TeamName)
                .FirstOrDefault();
        }

        private async Task<IEnumerable<SportTeamsInfoDto>> GetSportTeamsInfo()
        {
            const string sportTeamInfoKey = $"sportTeamInfoKey";
            var sportTeamsInfoResult = await _redisHelper.TryGetOrCreateAsync(sportTeamInfoKey, TimeSpan.FromMinutes(10),
                async () => await _hotEventService.GetSportTeamsInfoList());

            return sportTeamsInfoResult;
        }

        private async Task<IEnumerable<SportLeaguesInfoDto>> GetSportLeaguesInfo()
        {
            const string sportLeaguesInfoKey = $"sportLeaguesInfoKey";
            var sportLeaguesInfoResult = await _redisHelper.TryGetOrCreateAsync(sportLeaguesInfoKey, TimeSpan.FromMinutes(10),
                async () => await _hotEventService.GetSportLeaguesInfoList());

            return sportLeaguesInfoResult;
        }

        #endregion
    }
}
