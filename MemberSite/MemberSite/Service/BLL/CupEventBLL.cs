using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using Newtonsoft.Json;

namespace MemberSite.Service.BLL
{
    public class CupEventBLL
    {
        private readonly CustInfoBll _custInfoBll;
        private readonly CupEventService _cupEventService;
        private readonly EventsService _eventsService;
        private readonly MySqlClient _mainDbClient;
        private readonly ICacheHelper _redisHelper;
        
        public CupEventBLL(IServiceProvider provider)
        {
            _custInfoBll = provider.GetRequiredService<CustInfoBll>();
            _cupEventService = provider.GetRequiredService<CupEventService>();
            _eventsService = provider.GetRequiredService<EventsService>();
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _redisHelper = provider.GetRequiredService<ICacheHelper>();
        }

        #region 隊伍設定

        public CupEventPredictionSettingResponse GetCupEventPredictionSettingResponse(string langId)
        {
            var allTeamEnglishName = GetCupEventPredictionCountry("en");
            var dbData = GetCupEventPredictionSetting(langId);
            var selectOption = GetSelectOptions(langId);

            var responseData = new CupEventPredictionSettingResponse()
            {
                Top24Team = FilterData(dbData, allTeamEnglishName, "top24Team"),
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

        #region Private
        
        /// <summary>
        /// 如果Redis 沒有，就從db撈，之後塞到 Redis cache 1 天
        /// </summary>
        /// <returns></returns>
        private List<CupEventPredictionCountry> GetCupEventPredictionCountry(string langId)
        {
            var countryKey = $"CupEventPredictionCountryList:{langId}";
            var allTeamName = _redisHelper.TryGetOrCreate(countryKey, TimeSpan.FromDays(1),
                () => GetCupEventPredictionSelectOptions(langId).ToList());
            return allTeamName;
        }

        /// <summary>
        /// 如果Redis 沒有，就從db撈，之後塞到 Redis cache 1 分鐘
        /// </summary>
        /// <param name="langId"></param>
        /// <returns></returns>
        private List<CupEventPredictionSettingDto> GetCupEventPredictionSetting(string langId)
        {
            var cupEventPredictionSettingKey = $"CupEventPredictionSetting:{langId}";
            var dbData = _redisHelper.TryGetOrCreate(cupEventPredictionSettingKey, TimeSpan.FromMinutes(1),
                () => _cupEventService.GetCupEventPredictionSetting(langId).ToList());
            return dbData;
        }

        private IEnumerable<CupEventPredictionCountry> GetCupEventPredictionSelectOptions(string langId)
        {
            var dbData = _cupEventService.GetCupEventPredictionOptions(langId);

            var result = dbData.Select(r => new CupEventPredictionCountry
            {
                CountryId = r.TeamId,
                CountryName = r.TeamName,
                EnCountryName = r.EnTeamName
            });

            return result;
        }

        private IEnumerable<CupEventSetting> FilterData(IEnumerable<CupEventPredictionSettingDto> sourceData, IEnumerable<CupEventPredictionCountry> teamEnglishNames, string levelName)
        {
            return sourceData.Where(r => r.LevelName == levelName)
                .OrderBy(c => c.Sort)
                .Select(x => new CupEventSetting
                {
                    CountryName = x.TeamName,
                    ImageName = $"{teamEnglishNames.FirstOrDefault(r => r.CountryId == x.TeamId)?.CountryName}.png",
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
                new MatchTimeInfoDto { MatchName = "Match 38", MatchTime = new DateTimeOffset(new DateTime(2024, 6, 29, 16, 0, 0,DateTimeKind.Utc)).ToString("yyyy-MM-dd'T'HH:mm:sszzz") },
                new MatchTimeInfoDto { MatchName = "Match 37", MatchTime = new DateTimeOffset(new DateTime(2024, 6, 29, 19, 0, 0,DateTimeKind.Utc)).ToString("yyyy-MM-dd'T'HH:mm:sszzz") },
                new MatchTimeInfoDto { MatchName = "Match 40", MatchTime = new DateTimeOffset(new DateTime(2024, 6, 30, 16, 0, 0,DateTimeKind.Utc)).ToString("yyyy-MM-dd'T'HH:mm:sszzz") },
                new MatchTimeInfoDto { MatchName = "Match 39", MatchTime = new DateTimeOffset(new DateTime(2024, 6, 30, 19, 0, 0,DateTimeKind.Utc)).ToString("yyyy-MM-dd'T'HH:mm:sszzz") },
                new MatchTimeInfoDto { MatchName = "Match 42", MatchTime = new DateTimeOffset(new DateTime(2024, 7, 01, 16, 0, 0,DateTimeKind.Utc)).ToString("yyyy-MM-dd'T'HH:mm:sszzz") },
                new MatchTimeInfoDto { MatchName = "Match 41", MatchTime = new DateTimeOffset(new DateTime(2024, 7, 01, 19, 0, 0,DateTimeKind.Utc)).ToString("yyyy-MM-dd'T'HH:mm:sszzz") },
                new MatchTimeInfoDto { MatchName = "Match 43", MatchTime = new DateTimeOffset(new DateTime(2024, 7, 01, 16, 0, 0,DateTimeKind.Utc)).ToString("yyyy-MM-dd'T'HH:mm:sszzz") },
                new MatchTimeInfoDto { MatchName = "Match 44", MatchTime = new DateTimeOffset(new DateTime(2024, 7, 01, 19, 0, 0,DateTimeKind.Utc)).ToString("yyyy-MM-dd'T'HH:mm:sszzz") },
                new MatchTimeInfoDto { MatchName = "Match 45", MatchTime = new DateTimeOffset(new DateTime(2024, 7, 05, 16, 0, 0,DateTimeKind.Utc)).ToString("yyyy-MM-dd'T'HH:mm:sszzz") },
                new MatchTimeInfoDto { MatchName = "Match 46", MatchTime = new DateTimeOffset(new DateTime(2024, 7, 05, 19, 0, 0,DateTimeKind.Utc)).ToString("yyyy-MM-dd'T'HH:mm:sszzz") },
                new MatchTimeInfoDto { MatchName = "Match 48", MatchTime = new DateTimeOffset(new DateTime(2024, 7, 06, 16, 0, 0,DateTimeKind.Utc)).ToString("yyyy-MM-dd'T'HH:mm:sszzz") },
                new MatchTimeInfoDto { MatchName = "Match 47", MatchTime = new DateTimeOffset(new DateTime(2024, 7, 06, 19, 0, 0,DateTimeKind.Utc)).ToString("yyyy-MM-dd'T'HH:mm:sszzz") },
                new MatchTimeInfoDto { MatchName = "Match 49", MatchTime = new DateTimeOffset(new DateTime(2024, 7, 09, 19, 0, 0,DateTimeKind.Utc)).ToString("yyyy-MM-dd'T'HH:mm:sszzz") },
                new MatchTimeInfoDto { MatchName = "Match 50", MatchTime = new DateTimeOffset(new DateTime(2024, 7, 10, 19, 0, 0,DateTimeKind.Utc)).ToString("yyyy-MM-dd'T'HH:mm:sszzz") },
                new MatchTimeInfoDto { MatchName = "Match 51", MatchTime = new DateTimeOffset(new DateTime(2024, 7, 14, 19, 0, 0,DateTimeKind.Utc)).ToString("yyyy-MM-dd'T'HH:mm:sszzz") }
            };
        }

        #endregion
        
        #endregion

        #region 充值 & 發票

        public async Task<MemberDepositTicketInfoResponse> GetCupEventTicket(int siteId, int custId, string userName, string langId, int currencyId)
        {
            var responseData = new MemberDepositTicketInfoResponse();
            
            var custInfoProfile = _custInfoBll.GetCustInfoProfile(custId);
            var cupEventName = CupEventNameEnum.EuroCup2024.ToString();
            var promotionMission = GetPromotionMission(siteId, currencyId, cupEventName).ToList();
            
            var userTicketsBefore = _cupEventService.GetCupEventMemberTicketInfo(custId).ToList();// 取得User總共的票
            var cupEventPromotionMissionProgress = GetCupEventPromotionMissionProgress(siteId, custInfoProfile, promotionMission, userTicketsBefore);
            
            // 未驗證前給的資料
            responseData.UserName = userName;
            responseData.PromotionMission = promotionMission;
            responseData.CupEventPromotionMissionProgress = cupEventPromotionMissionProgress;
            responseData.SelectOptions = GetSelectOptions(langId);// 取得32強分組選單
            
            // 檢查驗證
            if (!CheckCustVerifyStatus(siteId, custInfoProfile))
            {
                responseData.ErrorCode = 104;
                return responseData;
            }
            
            #region 發票
            
            if (promotionMission.Any())
            {
                switch (siteId)
                {
                    case 1:
                        await TicketProcessNova88(siteId, custId, currencyId, userTicketsBefore, promotionMission, cupEventPromotionMissionProgress);
                        break;
                    case 2:
                        await TicketProcessGoal123(siteId, custId, currencyId, userTicketsBefore ,promotionMission.FirstOrDefault());
                        break;
                    case 3:
                        await TicketProcessBwing(siteId, custId, currencyId, userTicketsBefore ,promotionMission , responseData.CupEventPromotionMissionProgress);
                        responseData.AvailablePredict = promotionMission.All(x => x.IsCompleted);
                        break;
                }
            }
            
            var userTickets = _cupEventService.GetCupEventMemberTicketInfo(custId).ToList();// 取得User總共的票
            var userTicketResult = MapToUserTicketResult(userTickets, siteId, currencyId , out var finishedCount); // Mapping 成 Api 需要的格式
            
            responseData.UserTicketResult = userTicketResult;
            responseData.AvailableTickets = userTickets.Count; //總共的票
            responseData.UsedTickets = finishedCount; // 已投票的票數
            responseData.UnUsedTickets = userTickets.Count - finishedCount;
            
            #endregion
            
            return responseData;
        }
        
        public IEnumerable<CupEventTicketSummaryResponse> GetCupEventTicketSummary(string langId)
        {
            var countrySetting = GetCupEventPredictionSetting(langId);
            if (!countrySetting.Any()) throw new Exception("not found countrySetting");
            var top24Team = countrySetting.Where(r => r.LevelName == "top24Team").OrderBy(x => x.Sort).ToList();
            
            var countryList = GetCupEventPredictionCountry("en"); // 圖片名稱要用的
            if (countryList == null) throw new Exception("enCountry not found");
            
            var userTicketsAll = 0;
            var usedTickets = _cupEventService.GetCupEventMemberTicketInfo(0).ToList();
            if (usedTickets.Any())
            {
                usedTickets = usedTickets.Where(x=>x.TeamId is not null).ToList(); //已投票
                userTicketsAll = usedTickets.Count ; //總票數
            }
            
            var responseData = top24Team.GroupBy(x => new { x.TeamId, x.TeamName })
                .Select(x => new CupEventTicketSummaryResponse
                {
                    CountryId = x.Key.TeamId,
                    CountryName = x.Key.TeamName,
                    ImageName = $"{countryList.FirstOrDefault(y=>y.CountryId == x.Key.TeamId)?.EnCountryName}.png",
                    Percentage = usedTickets.Any() ? Math.Round(usedTickets.Count(r => r.TeamId == x.Key.TeamId) / (decimal)userTicketsAll * 100, 2) : 0
                }).ToList().OrderByDescending(x => x.Percentage);
            
            return responseData;
        }
        
        public async Task UserPollTicket(int custId, int siteId, int currencyId, List<UserPollTicketRequest> request)
        {
            CheckCupEventTicket(siteId,custId, currencyId, 1);

            await using var conn = _mainDbClient.GetMySqlConnection();
            await conn.OpenAsync();
            await using var trans = await conn.BeginTransactionAsync();
            
            var executeCount = 0;
            
            // 發票
            foreach (var model in request)
            {
                var result = await _cupEventService.UserPollTicketAsync(custId, model ,trans);
                if (result > 0)
                {
                    executeCount += 1;
                }
            }
            
            if (executeCount == request.Count)
            {
                MySqlClient.CommitAndDisposeConnection(conn, trans);
            }
            else
            {
                MySqlClient.RollbackAndDisposeConnection(conn, trans);
            }
        }
        
        public async Task MultiUserPollTicket(int custId, int siteId, int currencyId, MultiUserPollTicketRequest request)
        {
            CheckCupEventTicket(siteId,custId, currencyId,request.Count);
            
            await using var conn = _mainDbClient.GetMySqlConnection();
            await conn.OpenAsync();
            await using var trans = await conn.BeginTransactionAsync();
            
            // 發票
            var executeCount = 0;
                
            for (var i = 1; i <= request.Count; i++)
            {
                var result = await _cupEventService.UserPollTicketAsync(custId, request.UserPollTicket, trans);
                if (result > 0)
                {
                    executeCount += 1;
                }
            }
            if (executeCount == request.Count)
            {
                MySqlClient.CommitAndDisposeConnection(conn, trans);
            }
            else
            {
                MySqlClient.RollbackAndDisposeConnection(conn, trans);
                throw new Exception("pollTicket error");
            }
        }
        
        public IEnumerable<MemberDepositDto> GetMemberDeposit(int siteId, int custId,int currencyId)
        {
            var custDepositList = _cupEventService.GetMemberDeposit(siteId ,custId, currencyId);
            
            return custDepositList;
        }
        
        public async Task<bool> PredictCupEventScore(int siteId, int custId,int currencyId, PredictCupEventScoreRequest request)
        {
            var result = await _cupEventService.PredictCupEventScore(siteId, custId ,currencyId ,request);
            if (result > 0)
            {
                return true;
            }

            return false;
        }
        
        public List<CupEventPredictScoreResponse> GetCupEventPredictScore(int custId , string langId)
        {
            var dbData = GetCupEventPredictionSetting(langId);
            var allTeamEnglishName = GetCupEventPredictionCountry("en");
            var finalData = FilterData(dbData, allTeamEnglishName, "final").ToList();

            var result = new List<CupEventPredictScoreResponse>();
            if (finalData.Count == 2)
            {
                var scoreList = _cupEventService.GetCupEventPredictScore(custId);
                var homeTeam = finalData.First(x => x.Sort == 1);
                var awayTeam = finalData.First(x => x.Sort == 2); 
                
                result = new List<CupEventPredictScoreResponse>
                {
                    new()
                    {
                        Team = "Home",
                        TeamDetail = new CupEventSetting
                        {
                            CountryName = homeTeam.CountryName,
                            ImageName = homeTeam.ImageName,
                            Sort = homeTeam.Sort
                        },
                        Score = scoreList?.HomeTeamScore ?? 0 
                    },
                    new()
                    {
                        Team = "Away",
                        TeamDetail = new CupEventSetting
                        {
                            CountryName = awayTeam.CountryName,
                            ImageName = awayTeam.ImageName,
                            Sort = awayTeam.Sort
                        },
                        Score = scoreList?.AwayTeamScore ?? 0 
                    }
                };
            }
            
            return result;
        }
        
        #region private
        
        /// <summary>
        /// 取得任務活動存款流水進度條
        /// </summary>
        /// <returns></returns>
        private CupEventPromotionMissionProgress GetCupEventPromotionMissionProgress(int siteId, CustInfoProfile custInfo,
            IReadOnlyCollection<PromotionMission> promotionMission , IEnumerable<CupEventMemberTicketInfoDto> userTickets)
        {
            var response = new CupEventPromotionMissionProgress();

            if (siteId == 1)
            {
                // 已領取的票
                var used = userTickets.Select(t => t.TicketType).ToList();
                
                //尚有未完成的存款單
                if (!used.Contains(1) || !used.Contains(2))
                {
                    var depositList = _cupEventService.GetMemberDeposit(siteId ,custInfo.CustId, custInfo.CurrencyId).ToList();
                    response.DepositAmount = depositList.Sum(x => x.DepositAmount);
                }
                else
                {
                    response.DepositAmount = promotionMission.Max(x => x.DepositAmount);
                }

                //尚有未完成的流水單
                if (!used.Contains(3) || !used.Contains(4))
                {
                    var custTurnover = _cupEventService.GetCupEventPromotionMissionTurnover(siteId , custInfo.CustId , custInfo.CurrencyId, 0);
                    response.TurnoverSum = custTurnover;
                }
                else
                {
                    response.TurnoverSum = promotionMission.Max(x => x.TurnoverAmount);
                }
            }
            else if (siteId == 3)
            {
                var affList = GetCupEventAffInfo(siteId, custInfo).ToList();
                response.AffList = affList;
            }
            
            return response;
        }
        
        /// <summary>
        /// 取得任務活動，不過期
        /// </summary>
        /// <returns></returns>
        private IEnumerable<PromotionMission> GetPromotionMission(int siteId, int currencyId, string missionName)
        {
            var promotionMissionKey = $"promotionMissionKey:{siteId}:{currencyId}:{missionName}";
            var promotionMission = _redisHelper.TryGetOrCreate(promotionMissionKey, TimeSpan.FromDays(60),
                () => _eventsService.GetPromotionMission(siteId, currencyId, missionName).ToList());
            
            return promotionMission;
        }

        /// <summary>
        /// 處理玩家票券狀態
        /// </summary>
        /// <param name="tickets"></param>
        /// <param name="siteId"></param>
        /// <param name="currencyId"></param>
        /// <param name="finishedCount"></param>
        /// <returns></returns>
        private UserTicketResult MapToUserTicketResult(List<CupEventMemberTicketInfoDto> tickets, int siteId, int currencyId , out int finishedCount)
        {
            var userTicketResult = new UserTicketResult
            {
                TicketList = new List<UserTicket>()
            };
            finishedCount = 0;
            
            if (!tickets.Any()) return userTicketResult;
            
            finishedCount = tickets.Count(x => x.TeamId is not null);
            
            if (siteId is 1)
            {
                var cupEventDateSetting = GetCupEventDateSetting().First(r => r.SiteId == siteId && r.CurrencyId == currencyId);
                foreach (var ticket in tickets)
                {
                    var ticketModel = new UserTicket()
                    {
                        TicketType = ticket.TicketType,
                        CountryId = ticket.TeamId,
                        TicketStatus = GetTicketStatus(ticket, cupEventDateSetting)
                    };
                    userTicketResult.TicketList.Add(ticketModel);
                }
            }
            // siteId = 2,3
            else
            {
                var groupData = tickets.Where(x => x.TeamId is not null)
                    .GroupBy(x => x.TeamId)
                    .Select(x => new GroupCountryTicket
                    {
                        CountryId = x.Key,
                        Count = x.Count()
                    }).ToList();
                    
                userTicketResult.GroupCountryTicket = groupData;
            }
            
            return userTicketResult;
        }

        private CupEventUserTicketStatus GetTicketStatus(CupEventMemberTicketInfoDto ticket, CupEventDateSettingDto cupEventDateSetting)
        {
            var userCurrentTime = GetUserCurrentTime();

            if (userCurrentTime < cupEventDateSetting.TicketOpenTime) //投票時間還沒開始
            {
                return CupEventUserTicketStatus.UnStarted;
            }

            if (userCurrentTime > cupEventDateSetting.TicketCloseTime) //投票時間已結束
            {
                return CupEventUserTicketStatus.UnStarted;
            }

            return ticket.TeamId == null ? CupEventUserTicketStatus.Active : CupEventUserTicketStatus.Finished;
        }

        /// <summary>
        /// 取得玩家當前UTC時區時間
        /// </summary>
        /// <returns></returns>
        private DateTimeOffset GetUserCurrentTime()
        {
            return new DateTimeOffset(DateTime.UtcNow, new TimeSpan(0, 0, 0));
        }

        private IEnumerable<CupEventDateSettingDto> GetCupEventDateSetting()
        {
            var cupEventDateSettingKey = "CupEventDateSetting";
            var cupEbentDateSetting = _redisHelper.TryGetOrCreate(cupEventDateSettingKey, TimeSpan.FromDays(60),
                () => _cupEventService.GetCupEventDateSetting().ToList());

            return cupEbentDateSetting;
        }

        /// <summary>
        /// 取得 24 強 Group小組
        /// </summary>
        /// <param name="langId"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private SelectOptions GetSelectOptions(string langId)
        {
            var countrySetting = GetCupEventPredictionSetting(langId);
            if (!countrySetting.Any()) throw new Exception("not found countrySetting");

            var top24Team = countrySetting.Where(r => r.LevelName == "top24Team").OrderBy(x => x.Sort).ToList();
            if (top24Team.Count < 24) throw new Exception("countrySetting top24Team less than 24 Team");

            var enCountry = GetCupEventPredictionCountry("en"); // 取得英文的國家名稱給圖片名稱用
            if (enCountry == null) throw new Exception("enCountry not found");

            var selectOptions = new SelectOptions()
            {
                GroupA = top24Team.Where(r => r.Sort is >= 1 and <= 4)
                     .Select(x => new GroupSelect()
                     {
                         CountryId = x.TeamId,
                         CountryName = x.TeamName,
                         ImageName = $"{enCountry.First(r => r.CountryId == x.TeamId).CountryName}.png"
                     }).ToList(),

                GroupB = top24Team.Where(r => r.Sort is >= 5 and <= 8)
                     .Select(x => new GroupSelect()
                     {
                         CountryId = x.TeamId,
                         CountryName = x.TeamName,
                         ImageName = $"{enCountry.First(r => r.CountryId == x.TeamId).CountryName}.png"
                     }).ToList(),

                GroupC = top24Team.Where(r => r.Sort is >= 9 and <= 12)
                     .Select(x => new GroupSelect()
                     {
                         CountryId = x.TeamId,
                         CountryName = x.TeamName,
                         ImageName = $"{enCountry.First(r => r.CountryId == x.TeamId).CountryName}.png"
                     }).ToList(),

                GroupD = top24Team.Where(r => r.Sort is >= 13 and <= 16)
                     .Select(x => new GroupSelect()
                     {
                         CountryId = x.TeamId,
                         CountryName = x.TeamName,
                         ImageName = $"{enCountry.First(r => r.CountryId == x.TeamId).CountryName}.png"
                     }).ToList(),

                GroupE = top24Team.Where(r => r.Sort is >= 17 and <= 20)
                     .Select(x => new GroupSelect()
                     {
                         CountryId = x.TeamId,
                         CountryName = x.TeamName,
                         ImageName = $"{enCountry.First(r => r.CountryId == x.TeamId).CountryName}.png"
                     }).ToList(),

                GroupF = top24Team.Where(r => r.Sort is >= 21 and <= 24)
                     .Select(x => new GroupSelect()
                     {
                         CountryId = x.TeamId,
                         CountryName = x.TeamName,
                         ImageName = $"{enCountry.First(r => r.CountryId == x.TeamId).CountryName}.png"
                     }).ToList(),

                // GroupG = top24Team.Where(r => r.Sort is >= 13 and <= 16)
                //      .Select(x => new GroupSelect()
                //      {
                //          CountryId = x.TeamId,
                //          CountryName = x.TeamName,
                //          ImageName = $"{enCountry.First(r => r.CountryId == x.TeamId).CountryName}.png"
                //      }).ToList(),
                //
                // GroupH = top24Team.Where(r => r.Sort is >= 29 and <= 32)
                //      .Select(x => new GroupSelect()
                //      {
                //          CountryId = x.TeamId,
                //          CountryName = x.TeamName,
                //          ImageName = $"{enCountry.First(r => r.CountryId == x.TeamId).CountryName}.png"
                //      }).ToList()
            };

            return selectOptions;
        }

        /// <summary>
        /// Nova88 發票任務邏輯
        /// </summary>
        /// <param name="siteId"></param>
        /// <param name="custId"></param>
        /// <param name="currencyId"></param>
        /// <param name="userTickets"></param>
        /// <param name="promotionMission"></param>
        /// <param name="progress"></param>
        private async Task TicketProcessNova88(int siteId, int custId, int currencyId , 
            IReadOnlyCollection<CupEventMemberTicketInfoDto> userTickets, List<PromotionMission> promotionMission ,CupEventPromotionMissionProgress progress)
        {
            if (userTickets.Count >= 4)
            {
                //票滿了 不往下檢查
                SetPromotionMissionCompleted(promotionMission);
                return;
            }
            
            var insertTickets = new List<CustTicketInfoDto>();
            
            var missions = RemoveClaimedPromotionMission(promotionMission, userTickets);
            foreach (var mission in missions)
            {
                var isInsert = false;
                var ticketInit = new CustTicketInfoDto
                {
                    SiteId = siteId,
                    CustId = custId,
                    CurrencyId = currencyId,
                    TicketStatus = 1,
                    TicketType = mission.MissionNo,
                    TransId = string.Empty
                };

                if (mission.DepositFlag && progress.DepositAmount >= mission.DepositAmount)
                {
                    isInsert = true;
                }
                else if (mission.TurnoverFlag && progress.TurnoverSum >= mission.TurnoverAmount)
                {
                    isInsert = true;
                }
                
                if (isInsert)
                {
                    insertTickets.Add(ticketInit);
                    mission.IsCompleted = true;
                }
            }
                
            // 發票
            var tasks = new List<Task<int>>();
            if (insertTickets.Count > 0) tasks.AddRange(insertTickets.Select(item => Task.Run(() => _cupEventService.InsertCupEventMemberTicket(item))));
            if (tasks.Count > 0) await Task.WhenAll(tasks);
            
        }

        /// <summary>
        /// Goal123 發票任務邏輯
        /// </summary>
        /// <param name="siteId"></param>
        /// <param name="custId"></param>
        /// <param name="currencyId"></param>
        /// <param name="userTickets"></param>
        /// <param name="promotionMission"></param>
        private async Task TicketProcessGoal123(int siteId, int custId, int currencyId ,
        IEnumerable<CupEventMemberTicketInfoDto> userTickets, PromotionMission promotionMission)
        {
            var depositList = _cupEventService.GetMemberDeposit(siteId ,custId, currencyId).ToList();
            
            // 存款有達標的存款單
            depositList = depositList.Where(x => x.DepositAmount >= promotionMission.DepositAmount).ToList();
            
            // 排除使用過存款單
            depositList = RemoveClaimedDepositList(depositList, userTickets).ToList();
            
            var insertTickets = depositList.Select( x => 
                new CustTicketInfoDto {
                    SiteId = siteId, 
                    CustId = custId,
                    CurrencyId = currencyId, 
                    TicketStatus = 1,
                    TransId = JsonConvert.SerializeObject(new List<string>{x.TransId})
                }).ToList();
            
            // 發票
            var tasks = new List<Task<int>>();
            if (insertTickets.Count > 0) tasks.AddRange(insertTickets.Select(item => Task.Run(() => _cupEventService.InsertCupEventMemberTicket(item))));
            if (tasks.Count > 0) await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Bwing 發票任務邏輯
        /// </summary>
        /// <param name="siteId"></param>
        /// <param name="custId"></param>
        /// <param name="currencyId"></param>
        /// <param name="userTickets"></param>
        /// <param name="promotionMission"></param>
        /// <param name="progress"></param>
        private async Task TicketProcessBwing(int siteId, int custId, int currencyId,
            IReadOnlyCollection<CupEventMemberTicketInfoDto> userTickets, List<PromotionMission> promotionMission , CupEventPromotionMissionProgress progress )
        {
            if (userTickets.Count >= 10)
            {
                //票滿了 不往下檢查
                SetPromotionMissionCompleted(promotionMission);
                return;
            }
            
            var insertTickets = new List<CustTicketInfoDto>();
            
            //排除掉已完成的任務
            var missions = RemoveClaimedPromotionMission(promotionMission, userTickets);
            
            // 排除使用過存款單
            var depositList = _cupEventService.GetMemberDeposit(siteId ,custId, currencyId).ToList();
            depositList = RemoveClaimedDepositList(depositList, userTickets).OrderBy(x=>x.StatementTime).ToList();
            
            foreach (var mission in missions)
            {
                var isInsert = false;
                var insertCount = 0;
                var ticket = new CustTicketInfoDto
                {
                    SiteId = siteId,
                    CustId = custId,
                    CurrencyId = currencyId,
                    TicketStatus = 1,
                    TicketType = mission.MissionNo,
                    TransId = string.Empty
                };

                //處理 ticketType 2 and 3
                if (mission.DepositFlag)
                { 
                    var unUsedDepositSum = (decimal)0.00;
                    var useDepositList = new List<string>();
                    
                    foreach (var deposit in depositList)
                    {
                        unUsedDepositSum += deposit.DepositAmount;
                        progress.DepositAmount += deposit.DepositAmount;
                        
                        depositList = depositList.Where(m => m.TransId != deposit.TransId).ToList();
                        useDepositList.Add(deposit.TransId);
                        
                        if (unUsedDepositSum >= mission.DepositAmount)
                        {
                            isInsert = true;
                            insertCount += mission.TicketCount;
                            ticket.TransId = JsonConvert.SerializeObject(useDepositList);
                            progress.DepositAmount = 0;
                            break;
                        }
                    }
                }
                //處理 ticketType 4
                else if (mission.AffFlag)
                {
                    if (progress.AffList.Count() >= 3)
                    {
                        if (progress.AffList.Any(aff => aff.Mission1 && aff.Mission2))
                        {
                            isInsert = true;
                            insertCount = mission.TicketCount;
                        }
                    }
                }
                //處理 ticketType 5
                else if (mission.TurnoverFlag)
                {
                    // 檢查下注
                    var betList = _cupEventService.GetCupEventSportBet(siteId, custId, currencyId, mission.TurnoverAmount).ToList();
                    if (betList.Count >= 20 )
                    {
                        isInsert = true;
                        insertCount = mission.TicketCount;
                    }
                    else
                    {
                        progress.CompletedBetCount = betList.Count;
                    }
                }
                //處理 ticketType 1
                else
                {
                    if (CheckCustVerifyStatusBwing(custId))
                    {
                        isInsert = true;
                        insertCount = mission.TicketCount;
                    }
                }
                
                if (isInsert)
                {
                    for (var i = 1; i <= insertCount; i++)
                    {
                        insertTickets.Add(ticket);
                    }
                    mission.IsCompleted = true;
                }
                else
                {
                    break;
                }
            }
            
            // 發票
            var tasks = new List<Task<int>>();
            if (insertTickets.Count > 0) tasks.AddRange(insertTickets.Select(item => Task.Run(() => _cupEventService.InsertCupEventMemberTicket(item))));
            if (tasks.Count > 0) await Task.WhenAll(tasks);
        }
        
        private void SetPromotionMissionCompleted(List<PromotionMission> promotionMission)
        {
            foreach (var mission in promotionMission)
            {
                mission.IsCompleted = true;
            }
        }

        private bool CheckCustVerifyStatus(int siteId, CustInfoProfile custInfo)
        {
            var status = false;
            switch (siteId)
            {
                case 1 or 2:
                    if (custInfo.PhoneVerified && custInfo.EmailVerified) status = true;
                   break;
                case 3:
                    status = true;
                    break;
            }
            
            return status;
        }
        
        private bool CheckCustVerifyStatusBwing(int custId)
        {
            var custInfoProfile = _custInfoBll.GetCustInfoProfile(custId);

            if (custInfoProfile.PhoneVerified)
            {
                return true;
            };
            return false;
        }
        
        private void CheckCupEventTicket(int siteId, int custId, int currencyId, int count)
        {
            var cupEventDateSetting = GetCupEventDateSetting().First(r => r.SiteId == siteId && r.CurrencyId == currencyId);
            var userCurrentTime = GetUserCurrentTime();

            if (userCurrentTime < cupEventDateSetting.TicketOpenTime) // 投票時間還沒開始
            {
                throw new Exception("pollTicket not yet started");
            }

            if (userCurrentTime > cupEventDateSetting.TicketCloseTime) // 投票時間已結束
            {
                throw new Exception("pollTicket time over");
            }
            
            // 檢查預測數量
            var userTickets = _cupEventService.GetCupEventMemberTicketInfo(custId).Where(x=>x.TeamId is null);// 取得User尚未預測的票
            if (userTickets.Count() < count)
            {
                throw new Exception("not enough tickets");
            }
        }

        /// <summary>
        /// 排除掉已領取過的任務
        /// </summary>
        /// <param name="promotionMission"></param>
        /// <param name="userTickets"></param>
        /// <returns></returns>
        private IEnumerable<PromotionMission> RemoveClaimedPromotionMission(IReadOnlyCollection<PromotionMission> promotionMission ,IEnumerable<CupEventMemberTicketInfoDto> userTickets)
        {
            // 已領取的票
            var used = userTickets.Select(t => t.TicketType).ToList();
            
            // Set IsCompleted
            var completedMission = promotionMission.Where(m => used.Contains(m.MissionNo)).ToList();
            SetPromotionMissionCompleted(completedMission);
            
            // 排除已領過的missionNo
            var result = promotionMission.Where(m => !used.Contains(m.MissionNo)).OrderBy(x=>x.MissionNo).ToList();

            return result;
        }

        /// <summary>
        /// 排除掉已使用過的存款單
        /// </summary>
        /// <param name="depositList"></param>
        /// <param name="userTickets"></param>
        /// <returns></returns>
        private IEnumerable<MemberDepositDto> RemoveClaimedDepositList(IEnumerable<MemberDepositDto> depositList ,IEnumerable<CupEventMemberTicketInfoDto> userTickets)
        {
            var usedDepositList = new List<string>();
            // 已領取的存款單
            foreach (var model in userTickets)
            {
                if (!string.IsNullOrEmpty(model.TransId))
                {
                    var transIds = JsonConvert.DeserializeObject<List<string>>(model.TransId);
                    usedDepositList.AddRange(transIds);
                }
            }
            
            // 排除已領過的存款單
            var result = depositList.Where(m => !usedDepositList.Contains(m.TransId)).ToList();

            return result;
        }

        /// <summary>
        /// 取得用戶推薦名單
        /// </summary>
        /// <param name="siteId"></param>
        /// <param name="custInfo"></param>
        /// <returns></returns>
        private IEnumerable<CupEventAffResponse> GetCupEventAffInfo(int siteId, CustInfoProfile custInfo)
        {
            var response = new List<CupEventAffResponse>();
            
            var affList = _cupEventService.GetCupEventAffInfo(siteId ,custInfo.CustId, custInfo.CurrencyId);
            affList = affList.Where(x => x.AccountName != custInfo.AccountName && x.SignupIp != custInfo.SignupIp).ToList();
            foreach (var aff in affList)
            {
                var affTickets = _cupEventService.GetCupEventMemberTicketInfo(aff.CustId).ToList();
                var affInfo = new CupEventAffResponse
                {
                    AccountName = aff.AccountName,
                    Mission1 = affTickets.Any(x => x.TicketType == 1),
                    Mission2 = affTickets.Any(x => x.TicketType == 2)
                };
                response.Add(affInfo);
            }
            
            return response;
        }
        
        #endregion
        
        #endregion
    }
}
