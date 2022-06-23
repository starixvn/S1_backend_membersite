using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;

namespace MemberSite.Service.BLL
{
    public class WorldCupPredictionBLL
    {
        private readonly WorldCupPredictionService _worldCupPredictionService;
        private readonly ICacheHelper _redisHelper;
       

        public WorldCupPredictionBLL(IServiceProvider provider)
        {
            this._worldCupPredictionService = provider.GetRequiredService<WorldCupPredictionService>();
            this._redisHelper = provider.GetRequiredService<ICacheHelper>();
        }

        public  WorldCupPredictionSettingResponse GetWorldCupPredictionSettingResponse(string langId)
        {
            var allTeamEnglishName = GetWorldCupPredictionCountry();
            var dbData = GetWorldCupPredictionSetting(langId);

            var responseData = new WorldCupPredictionSettingResponse()
            {
                Top32Team = FilterData(dbData, allTeamEnglishName, "top32Team"),
                RoundOf16 = FilterData(dbData, allTeamEnglishName, "roundOf16"),
                QuarterFinals = FilterData(dbData, allTeamEnglishName, "quarterFinals"),
                SemiFinals = FilterData(dbData, allTeamEnglishName, "semiFinals"),
                ThirdRdPlace = FilterData(dbData, allTeamEnglishName, "3rdPlace"),
                Final = FilterData(dbData, allTeamEnglishName, "final"),
                MatchTimes = GetMatchTimes(),
            };

            return responseData;
        }

        /// <summary>
        /// 如果Redis 沒有，就從db撈，之後塞到 Redis cache 1 天
        /// </summary>
        /// <returns></returns>
        private List<WorldCupPredictionCountry> GetWorldCupPredictionCountry()
        {
            const string countryKey = "Member:WorldCupPredictionCountryList";
            var allTeamEnglishName = _redisHelper.TryGetOrCreate(countryKey, TimeSpan.FromDays(1),
                _ => GetWorldCupPredictionSelectOptions().ToList());
            return allTeamEnglishName;
        }

        /// <summary>
        /// 如果Redis 沒有，就從db撈，之後塞到 Redis cache 1 分鐘
        /// </summary>
        /// <param name="langId"></param>
        /// <returns></returns>
        private List<WorldCupPredictionSettingDto> GetWorldCupPredictionSetting(string langId)
        {
            var worldCupPredictionSettingKey = $"Member:WorldCupPredictionSetting:{langId}";
            var dbData = _redisHelper.TryGetOrCreate(worldCupPredictionSettingKey, TimeSpan.FromMinutes(1),
                _ => _worldCupPredictionService.GetWorldCupPredictionSetting(langId).ToList());
            return dbData;
        }

        private IEnumerable<WorldCupPredictionCountry> GetWorldCupPredictionSelectOptions()
        {
            var dbData = _worldCupPredictionService.GetWorldCupPredictionOptions();

            var result = dbData.Select(r => new WorldCupPredictionCountry
            {
                CountryId = r.TeamId,
                CountryName = r.TeamName
            });

            return result;
        }

        private IEnumerable<WorldCupEventSetting> FilterData(IEnumerable<WorldCupPredictionSettingDto> sourceData,
            IEnumerable<WorldCupPredictionCountry> teamEnglishNames,
            string levelName)
        {
            return sourceData.Where(r => r.LevelName == levelName)
                .OrderBy(c => c.Sort)
                .Select(x => new WorldCupEventSetting
                {
                    CountryName = x.TeamName,
                    ImageName = $"{teamEnglishNames.FirstOrDefault(r => r.CountryId == x.TeamId)?.CountryName.ToLower()}.jpg",
                    Sort = x.Sort,
                }).ToList();
        }

        /// <summary>
        /// 賽程時間固定，PM提供，所以就直接HardCode
        /// </summary>
        /// <returns></returns>
        private IEnumerable<MatchTimeInfoDto> GetMatchTimes()
        {
            return new List<MatchTimeInfoDto>()
            {
                new MatchTimeInfoDto { MatchName = "Match 49", MatchTime = new DateTimeOffset(new DateTime(2022, 12, 03, 15, 0, 0,DateTimeKind.Utc)).ToString("yyyy-MM-dd'T'HH:mm:sszzz") },
                new MatchTimeInfoDto { MatchName = "Match 50", MatchTime = new DateTimeOffset(new DateTime(2022, 12, 03, 19, 0, 0,DateTimeKind.Utc)).ToString("yyyy-MM-dd'T'HH:mm:sszzz") },
                new MatchTimeInfoDto { MatchName = "Match 52", MatchTime = new DateTimeOffset(new DateTime(2022, 12, 04, 15, 0, 0,DateTimeKind.Utc)).ToString("yyyy-MM-dd'T'HH:mm:sszzz") },
                new MatchTimeInfoDto { MatchName = "Match 51", MatchTime = new DateTimeOffset(new DateTime(2022, 12, 04, 19, 0, 0,DateTimeKind.Utc)).ToString("yyyy-MM-dd'T'HH:mm:sszzz") },
                new MatchTimeInfoDto { MatchName = "Match 53", MatchTime = new DateTimeOffset(new DateTime(2022, 12, 05, 15, 0, 0,DateTimeKind.Utc)).ToString("yyyy-MM-dd'T'HH:mm:sszzz") },
                new MatchTimeInfoDto { MatchName = "Match 54", MatchTime = new DateTimeOffset(new DateTime(2022, 12, 05, 19, 0, 0,DateTimeKind.Utc)).ToString("yyyy-MM-dd'T'HH:mm:sszzz") },
                new MatchTimeInfoDto { MatchName = "Match 55", MatchTime = new DateTimeOffset(new DateTime(2022, 12, 06, 15, 0, 0,DateTimeKind.Utc)).ToString("yyyy-MM-dd'T'HH:mm:sszzz") },
                new MatchTimeInfoDto { MatchName = "Match 56", MatchTime = new DateTimeOffset(new DateTime(2022, 12, 06, 19, 0, 0,DateTimeKind.Utc)).ToString("yyyy-MM-dd'T'HH:mm:sszzz") },
                new MatchTimeInfoDto { MatchName = "Match 58", MatchTime = new DateTimeOffset(new DateTime(2022, 12, 09, 15, 0, 0,DateTimeKind.Utc)).ToString("yyyy-MM-dd'T'HH:mm:sszzz") },
                new MatchTimeInfoDto { MatchName = "Match 57", MatchTime = new DateTimeOffset(new DateTime(2022, 12, 09, 19, 0, 0,DateTimeKind.Utc)).ToString("yyyy-MM-dd'T'HH:mm:sszzz") },
                new MatchTimeInfoDto { MatchName = "Match 60", MatchTime = new DateTimeOffset(new DateTime(2022, 12, 10, 15, 0, 0,DateTimeKind.Utc)).ToString("yyyy-MM-dd'T'HH:mm:sszzz") },
                new MatchTimeInfoDto { MatchName = "Match 59", MatchTime = new DateTimeOffset(new DateTime(2022, 12, 10, 19, 0, 0,DateTimeKind.Utc)).ToString("yyyy-MM-dd'T'HH:mm:sszzz") },
                new MatchTimeInfoDto { MatchName = "Match 61", MatchTime = new DateTimeOffset(new DateTime(2022, 12, 13, 19, 0, 0,DateTimeKind.Utc)).ToString("yyyy-MM-dd'T'HH:mm:sszzz") },
                new MatchTimeInfoDto { MatchName = "Match 62", MatchTime = new DateTimeOffset(new DateTime(2022, 12, 14, 19, 0, 0,DateTimeKind.Utc)).ToString("yyyy-MM-dd'T'HH:mm:sszzz") },
                new MatchTimeInfoDto { MatchName = "Match 63", MatchTime = new DateTimeOffset(new DateTime(2022, 12, 17, 15, 0, 0,DateTimeKind.Utc)).ToString("yyyy-MM-dd'T'HH:mm:sszzz") },
                new MatchTimeInfoDto { MatchName = "Match 64", MatchTime = new DateTimeOffset(new DateTime(2022, 12, 18, 15, 0, 0,DateTimeKind.Utc)).ToString("yyyy-MM-dd'T'HH:mm:sszzz") },
            };
        }
    }
}
