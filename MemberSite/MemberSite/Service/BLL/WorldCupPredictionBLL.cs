using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

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

        #region 隊伍設定

        public WorldCupPredictionSettingResponse GetWorldCupPredictionSettingResponse(string langId)
        {
            var allTeamEnglishName = GetWorldCupPredictionCountry();
            var dbData = GetWorldCupPredictionSetting(langId);
            var selectOption = GetSelectOptions(langId);

            var responseData = new WorldCupPredictionSettingResponse()
            {
                Top32Team = FilterData(dbData, allTeamEnglishName, "top32Team"),
                RoundOf16 = FilterData(dbData, allTeamEnglishName, "roundOf16"),
                QuarterFinals = FilterData(dbData, allTeamEnglishName, "quarterFinals"),
                SemiFinals = FilterData(dbData, allTeamEnglishName, "semiFinals"),
                ThirdRdPlace = FilterData(dbData, allTeamEnglishName, "3rdPlace"),
                Final = FilterData(dbData, allTeamEnglishName, "final"),
                MatchTimes = GetMatchTimes(),
                SelectOptions = selectOption
            };

            return responseData;
        }

        /// <summary>
        /// 如果Redis 沒有，就從db撈，之後塞到 Redis cache 1 天
        /// </summary>
        /// <returns></returns>
        private List<WorldCupPredictionCountry> GetWorldCupPredictionCountry()
        {
            const string countryKey = "WorldCupPredictionCountryList";
            var allTeamEnglishName = _redisHelper.TryGetOrCreate(countryKey, TimeSpan.FromDays(1),
                () => GetWorldCupPredictionSelectOptions().ToList());
            return allTeamEnglishName;
        }

        /// <summary>
        /// 如果Redis 沒有，就從db撈，之後塞到 Redis cache 1 分鐘
        /// </summary>
        /// <param name="langId"></param>
        /// <returns></returns>
        private List<WorldCupPredictionSettingDto> GetWorldCupPredictionSetting(string langId)
        {
            var worldCupPredictionSettingKey = $"WorldCupPredictionSetting:{langId}";
            var dbData = _redisHelper.TryGetOrCreate(worldCupPredictionSettingKey, TimeSpan.FromMinutes(1),
                () => _worldCupPredictionService.GetWorldCupPredictionSetting(langId).ToList());
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

        #endregion

        #region 充值 & 發票

        /// <summary>
        /// 取得玩家充值資訊，票券資訊
        /// </summary>
        /// <param name="siteId"></param>
        /// <param name="custId"></param>
        /// <param name="userName"></param>
        /// <param name="langId"></param>
        /// <param name="currencyId"></param>
        /// <returns></returns>
        public MemberDepositTicketInfoResponse GetMemberDepositTicketInfo(int siteId, int custId, string userName, string langId, int currencyId)
        {
            var memberDepositAmountDaily = GetMemberDepositAmountDaily(siteId, custId);// 取得充值金額清單
            var depositAmountSetting = GetDepositAmountSetting(); // 取得充值門檻
            var userTickets = _worldCupPredictionService.GetWorldCupMemberTicketInfo(custId).ToList();// 取得User的票
            var userTicketResult = MapToUserTicketResult(userTickets, siteId, currencyId); // Mapping 成 Api 需要的格式
            var selectOption = GetSelectOptions(langId); // 取得32強分組選單


            var responseData = new MemberDepositTicketInfoResponse()
            {
                UserName = userName,
                TicketLimit = depositAmountSetting.Where(r => r.SiteId == siteId && r.CurrencyId == currencyId).Select(r => (int)r.Amount).ToList(),
                DepositAmount = memberDepositAmountDaily.Sum(r => r.DepositAmount),
                UserTicketResult = userTicketResult,
                AvailableTickets = userTickets.Count,
                UsedTickets = GetFinishedTicketCount(userTicketResult),
                SelectOptions = selectOption
            };

            return responseData;
        }


        public async Task UserPollTicket(int custId, int siteId, int currencyId, UserPollTicketRequest request)
        {
            // 投票參數驗證
            if (request.TicketType is < 1 or > 3) throw new Exception("ticketType not found");

            var worldCupDateSetting = GetWorldCupDateSetting().First(r => r.SiteId == siteId && r.CurrencyId == currencyId);
            var userCurrentTime = GetUserCurrentTime(worldCupDateSetting);

            if (userCurrentTime < worldCupDateSetting.TicketOpenTime) // 投票時間還沒開始
            {
                throw new Exception("pollTicket not yet started");
            }

            if (userCurrentTime > worldCupDateSetting.TicketCloseTime) // 投票時間已結束
            {
                throw new Exception("pollTicket time over");
            }

            await _worldCupPredictionService.UserPollTicketAsync(custId, request);// 投票
        }

        /// <summary>
        /// 取得充值門檻資訊，不過期
        /// </summary>
        /// <returns></returns>
        private IEnumerable<DepositAmountSettingDto> GetDepositAmountSetting()
        {
            var depositAmountSettingKey = $"depositAmountSetting";
            var depositAmountSetting = _redisHelper.TryGetOrCreate(depositAmountSettingKey, TimeSpan.FromDays(60),
                () => _worldCupPredictionService.GetDepositAmountSetting().ToList());

            return depositAmountSetting;
        }

        /// <summary>
        /// 取得玩家充值資訊，cache 1 分鐘
        /// </summary>
        /// <param name="siteId"></param>
        /// <param name="custId"></param>
        /// <returns></returns>
        private IEnumerable<MemberDepositAmountDailyDto> GetMemberDepositAmountDaily(int siteId, int custId)
        {
            var memberDepositAmountDailyKey = $"memberDepositAmountDaily:{siteId}:{custId}";
            var memberDepositAmountDaily = _redisHelper.TryGetOrCreate(memberDepositAmountDailyKey, TimeSpan.FromMinutes(1),
                () => _worldCupPredictionService.GetMemberDepositAmountDaily(siteId, custId).ToList());

            return memberDepositAmountDaily;
        }

        /// <summary>
        /// 處理玩家票券狀態
        /// </summary>
        /// <param name="tickets"></param>
        /// <returns></returns>
        private UserTicketResult MapToUserTicketResult(List<WorldCupMemberTicketInfoDto> tickets, int siteId, int custId)
        {
            var userTicketResult = new UserTicketResult();

            if (tickets != null && !tickets.Any())
                return userTicketResult;

            var ticket1 = tickets.FirstOrDefault(r => r.TicketType == 1);
            if (ticket1 != null)
            {
                userTicketResult.Ticket1 = new UserTicket()
                {
                    CountryId = ticket1.TeamId,
                    TicketStatus = GetTicketStatus(ticket1, siteId, custId)
                };
            }

            var ticket2 = tickets.FirstOrDefault(r => r.TicketType == 2);
            if (ticket2 != null)
            {
                userTicketResult.Ticket2 = new UserTicket()
                {
                    CountryId = ticket2.TeamId,
                    TicketStatus = GetTicketStatus(ticket2, siteId, custId)
                };
            }

            var ticket3 = tickets.FirstOrDefault(r => r.TicketType == 3);
            if (ticket3 != null)
            {
                userTicketResult.Ticket3 = new UserTicket()
                {
                    CountryId = ticket3.TeamId,
                    TicketStatus = GetTicketStatus(ticket3, siteId, custId)
                };
            }

            return userTicketResult;
        }

        private WorldCupUserTicketStatus GetTicketStatus(WorldCupMemberTicketInfoDto ticket, int siteId, int currencyId)
        {
            var worldCupDateSetting = GetWorldCupDateSetting().First(r => r.SiteId == siteId && r.CurrencyId == currencyId);
            var userCurrentTime = GetUserCurrentTime(worldCupDateSetting);

            if (userCurrentTime < worldCupDateSetting.TicketOpenTime) //投票時間還沒開始
            {
                return WorldCupUserTicketStatus.UnStarted;
            }

            if (userCurrentTime > worldCupDateSetting.TicketCloseTime) //投票時間已結束
            {
                return WorldCupUserTicketStatus.UnStarted;
            }

            return ticket.TeamId == null ? WorldCupUserTicketStatus.Active : WorldCupUserTicketStatus.Finished;
        }

        /// <summary>
        /// 取得玩家當前UTC時區時間
        /// </summary>
        /// <param name="worldCupDateSetting"></param>
        /// <returns></returns>
        private DateTimeOffset GetUserCurrentTime(WorldCupDateSettingDto worldCupDateSetting)
        {
            return new DateTimeOffset(DateTime.UtcNow, new TimeSpan(0, 0, 0));
        }

        private IEnumerable<WorldCupDateSettingDto> GetWorldCupDateSetting()
        {
            var worldCupDateSettingKey = "WorldCupDateSetting";
            var worldCupDateSetting = _redisHelper.TryGetOrCreate(worldCupDateSettingKey, TimeSpan.FromDays(60),
                () => _worldCupPredictionService.GetWorldCupDateSetting().ToList());

            return worldCupDateSetting;
        }

        private int GetFinishedTicketCount(UserTicketResult userTicketResult)
        {
            var finishedCount = 0;

            if (userTicketResult.Ticket1 is { CountryId: { } }) finishedCount += 1;
            if (userTicketResult.Ticket2 is { CountryId: { } }) finishedCount += 1;
            if (userTicketResult.Ticket3 is { CountryId: { } }) finishedCount += 1;

            return finishedCount;
        }

        /// <summary>
        /// 取得 32 強 Group小組
        /// </summary>
        /// <param name="langId"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private SelectOptions GetSelectOptions(string langId)
        {
            var countrySetting = GetWorldCupPredictionSetting(langId);
            if (!countrySetting.Any()) throw new Exception("not found countrySetting");

            var top32Team = countrySetting.Where(r => r.LevelName == "top32Team").OrderBy(x => x.Sort).ToList();
            if (top32Team.Count < 32) throw new Exception("countrySetting top32Team less than 32 Team");

            var enCountry = GetWorldCupPredictionCountry(); // 取得英文的國家名稱給圖片名稱用
            if (enCountry == null) throw new Exception("enCountry not found");

            var selectOptions = new SelectOptions()
            {
                GroupA = top32Team.Where(r => r.Sort is >= 1 and <= 4)
                     .Select(x => new GroupSelect()
                     {
                         CountryId = x.TeamId,
                         CountryName = x.TeamName,
                         ImageName = $"{enCountry.First(r => r.CountryId == x.TeamId).CountryName.ToLower()}.jpg"
                     }).ToList(),

                GroupB = top32Team.Where(r => r.Sort is >= 17 and <= 20)
                     .Select(x => new GroupSelect()
                     {
                         CountryId = x.TeamId,
                         CountryName = x.TeamName,
                         ImageName = $"{enCountry.First(r => r.CountryId == x.TeamId).CountryName.ToLower()}.jpg"
                     }).ToList(),

                GroupC = top32Team.Where(r => r.Sort is >= 5 and <= 8)
                     .Select(x => new GroupSelect()
                     {
                         CountryId = x.TeamId,
                         CountryName = x.TeamName,
                         ImageName = $"{enCountry.First(r => r.CountryId == x.TeamId).CountryName.ToLower()}.jpg"
                     }).ToList(),

                GroupD = top32Team.Where(r => r.Sort is >= 21 and <= 24)
                     .Select(x => new GroupSelect()
                     {
                         CountryId = x.TeamId,
                         CountryName = x.TeamName,
                         ImageName = $"{enCountry.First(r => r.CountryId == x.TeamId).CountryName.ToLower()}.jpg"
                     }).ToList(),

                GroupE = top32Team.Where(r => r.Sort is >= 9 and <= 12)
                     .Select(x => new GroupSelect()
                     {
                         CountryId = x.TeamId,
                         CountryName = x.TeamName,
                         ImageName = $"{enCountry.First(r => r.CountryId == x.TeamId).CountryName.ToLower()}.jpg"
                     }).ToList(),

                GroupF = top32Team.Where(r => r.Sort is >= 25 and <= 28)
                     .Select(x => new GroupSelect()
                     {
                         CountryId = x.TeamId,
                         CountryName = x.TeamName,
                         ImageName = $"{enCountry.First(r => r.CountryId == x.TeamId).CountryName.ToLower()}.jpg"
                     }).ToList(),

                GroupG = top32Team.Where(r => r.Sort is >= 13 and <= 16)
                     .Select(x => new GroupSelect()
                     {
                         CountryId = x.TeamId,
                         CountryName = x.TeamName,
                         ImageName = $"{enCountry.First(r => r.CountryId == x.TeamId).CountryName.ToLower()}.jpg"
                     }).ToList(),

                GroupH = top32Team.Where(r => r.Sort is >= 29 and <= 32)
                     .Select(x => new GroupSelect()
                     {
                         CountryId = x.TeamId,
                         CountryName = x.TeamName,
                         ImageName = $"{enCountry.First(r => r.CountryId == x.TeamId).CountryName.ToLower()}.jpg"
                     }).ToList()
            };

            return selectOptions;
        }

        #endregion

    }
}
