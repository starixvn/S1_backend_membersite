using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.BLL
{
    public class CupEventBLL
    {
        private readonly CustInfoBll _custInfoBll;
        private readonly CupEventService _cupEventPredictionService;
        private readonly EventsService _eventsService;
        private readonly MySqlClient _mainDbClient;
        private readonly ICacheHelper _redisHelper;
        
        public CupEventBLL(IServiceProvider provider)
        {
            _custInfoBll = provider.GetRequiredService<CustInfoBll>();
            _cupEventPredictionService = provider.GetRequiredService<CupEventService>();
            _eventsService = provider.GetRequiredService<EventsService>();
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _redisHelper = provider.GetRequiredService<ICacheHelper>();
        }

        #region 隊伍設定

        public CupEventPredictionSettingResponse GetCupEventPredictionSettingResponse(string langId)
        {
            var allTeamEnglishName = GetCupEventPredictionCountry();
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
        private List<CupEventPredictionCountry> GetCupEventPredictionCountry()
        {
            const string countryKey = "CupEventPredictionCountryList";
            var allTeamEnglishName = _redisHelper.TryGetOrCreate(countryKey, TimeSpan.FromDays(1),
                () => GetCupEventPredictionSelectOptions().ToList());
            return allTeamEnglishName;
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
                () => _cupEventPredictionService.GetCupEventPredictionSetting(langId).ToList());
            return dbData;
        }

        private IEnumerable<CupEventPredictionCountry> GetCupEventPredictionSelectOptions()
        {
            var dbData = _cupEventPredictionService.GetCupEventPredictionOptions();

            var result = dbData.Select(r => new CupEventPredictionCountry
            {
                CountryId = r.TeamId,
                CountryName = r.TeamName
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
                    ImageName = $"{teamEnglishNames.FirstOrDefault(r => r.CountryId == x.TeamId)?.CountryName.ToLower()}.png",
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
            
            var cupEventName = CupEventNameEnum.EuroCup2024.ToString();
            var promotionMission = GetPromotionMission(siteId, currencyId, cupEventName).ToList();
            var custTurnover = _cupEventPredictionService.GetCupEventPromotionMissionTurnover(siteId , custId , currencyId, 0);
            var custDepositList = _cupEventPredictionService.GetMemberDeposit(siteId ,custId, currencyId).ToList();
            var depositSum = custDepositList.Sum(x => x.DepositAmount);
          
            // 未驗證前給的資料
            responseData.UserName = userName;
            responseData.PromotionMission = promotionMission;
            responseData.CupEventPromotionMissionProgress = new CupEventPromotionMissionProgress
            {
                DepositAmount = depositSum,
                TurnoverSum = custTurnover
            };
            responseData.SelectOptions = GetSelectOptions(langId);// 取得32強分組選單
            
            // 檢查驗證
            if (!CheckCustVerifyStatus(siteId, custId))
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
                        await TicketProcessNova88(siteId, custId, currencyId, promotionMission, depositSum, custTurnover);
                        break;
                    case 2:
                        await TicketProcessGoal123(siteId, custId, currencyId, promotionMission.FirstOrDefault(), custDepositList);
                        break;
                }
            }
            var userTickets = _cupEventPredictionService.GetCupEventMemberTicketInfo(custId).ToList();// 取得User總共的票
            var userTicketResult = MapToUserTicketResult(userTickets, siteId, currencyId , out var finishedCount); // Mapping 成 Api 需要的格式
            
            responseData.UserTicketResult = userTicketResult;
            responseData.AvailableTickets = userTickets.Count; //總共的票
            responseData.UsedTickets = finishedCount; // 已投票的票數
            responseData.UnUsedTickets = userTickets.Count - finishedCount;
            
            #endregion
            
            return responseData;
        }
        
        public IEnumerable<CupEventTicketSummaryResponse> GetCupEventTicketSummary()
        {
            var responseData = new List<CupEventTicketSummaryResponse>();
            
            var usedTickets = _cupEventPredictionService.GetCupEventMemberTicketInfo(0).Where(x=>x.TeamId is not null).ToList(); //已投票

            if (usedTickets.Any())
            {
                var enCountry = GetCupEventPredictionCountry(); // 取得英文的國家名稱給圖片名稱用
                if (enCountry == null) throw new Exception("enCountry not found");
                
                var userTicketsAll = usedTickets.Count; //總票數
                
                responseData = usedTickets.GroupBy(x=>x.TeamId)
                    .Select(x => new CupEventTicketSummaryResponse
                    {
                        CountryId = x.Key,
                        CountryName = enCountry.First(r => r.CountryId == x.Key).CountryName,
                        ImageName = $"{enCountry.First(r => r.CountryId == x.Key).CountryName.ToLower()}.png",
                        Percentage = Math.Round(x.Count() / (decimal)userTicketsAll * 100, 2)
                    }).ToList();
            }
            
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
                var result = await _cupEventPredictionService.UserPollTicketAsync(custId, model ,trans);
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
                var result = await _cupEventPredictionService.UserPollTicketAsync(custId, request.UserPollTicket, trans);
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
            var custDepositList = _cupEventPredictionService.GetMemberDeposit(siteId ,custId, currencyId);
            
            return custDepositList;
        }
        
        #region private
        
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
        /// <param name="custId"></param>
        /// <param name="finishedCount"></param>
        /// <returns></returns>
        private UserTicketResult MapToUserTicketResult(List<CupEventMemberTicketInfoDto> tickets, int siteId, int custId , out int finishedCount)
        {
            var userTicketResult = new UserTicketResult
            {
                TicketList = new List<UserTicket>()
            };
            finishedCount = 0;
            
            if (!tickets.Any()) return userTicketResult;
            
            finishedCount = tickets.Count(x => x.TeamId is not null);
            
            if (siteId is 1 or 3)
            {
                foreach (var ticket in tickets)
                {
                    var ticketModel = new UserTicket()
                    {
                        TicketType = ticket.TicketType,
                        CountryId = ticket.TeamId,
                        TicketStatus = GetTicketStatus(ticket, siteId, custId)
                    };
                    userTicketResult.TicketList.Add(ticketModel);
                }
            }
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

        private CupEventUserTicketStatus GetTicketStatus(CupEventMemberTicketInfoDto ticket, int siteId, int currencyId)
        {
            var cupEventDateSetting = GetCupEventDateSetting().First(r => r.SiteId == siteId && r.CurrencyId == currencyId);
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
                () => _cupEventPredictionService.GetCupEventDateSetting().ToList());

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

            var enCountry = GetCupEventPredictionCountry(); // 取得英文的國家名稱給圖片名稱用
            if (enCountry == null) throw new Exception("enCountry not found");

            var selectOptions = new SelectOptions()
            {
                GroupA = top24Team.Where(r => r.Sort is >= 1 and <= 4)
                     .Select(x => new GroupSelect()
                     {
                         CountryId = x.TeamId,
                         CountryName = x.TeamName,
                         ImageName = $"{enCountry.First(r => r.CountryId == x.TeamId).CountryName.ToLower()}.png"
                     }).ToList(),

                GroupB = top24Team.Where(r => r.Sort is >= 5 and <= 8)
                     .Select(x => new GroupSelect()
                     {
                         CountryId = x.TeamId,
                         CountryName = x.TeamName,
                         ImageName = $"{enCountry.First(r => r.CountryId == x.TeamId).CountryName.ToLower()}.png"
                     }).ToList(),

                GroupC = top24Team.Where(r => r.Sort is >= 9 and <= 12)
                     .Select(x => new GroupSelect()
                     {
                         CountryId = x.TeamId,
                         CountryName = x.TeamName,
                         ImageName = $"{enCountry.First(r => r.CountryId == x.TeamId).CountryName.ToLower()}.png"
                     }).ToList(),

                GroupD = top24Team.Where(r => r.Sort is >= 13 and <= 16)
                     .Select(x => new GroupSelect()
                     {
                         CountryId = x.TeamId,
                         CountryName = x.TeamName,
                         ImageName = $"{enCountry.First(r => r.CountryId == x.TeamId).CountryName.ToLower()}.png"
                     }).ToList(),

                GroupE = top24Team.Where(r => r.Sort is >= 17 and <= 20)
                     .Select(x => new GroupSelect()
                     {
                         CountryId = x.TeamId,
                         CountryName = x.TeamName,
                         ImageName = $"{enCountry.First(r => r.CountryId == x.TeamId).CountryName.ToLower()}.png"
                     }).ToList(),

                GroupF = top24Team.Where(r => r.Sort is >= 21 and <= 24)
                     .Select(x => new GroupSelect()
                     {
                         CountryId = x.TeamId,
                         CountryName = x.TeamName,
                         ImageName = $"{enCountry.First(r => r.CountryId == x.TeamId).CountryName.ToLower()}.png"
                     }).ToList(),

                // GroupG = top24Team.Where(r => r.Sort is >= 13 and <= 16)
                //      .Select(x => new GroupSelect()
                //      {
                //          CountryId = x.TeamId,
                //          CountryName = x.TeamName,
                //          ImageName = $"{enCountry.First(r => r.CountryId == x.TeamId).CountryName.ToLower()}.png"
                //      }).ToList(),
                //
                // GroupH = top24Team.Where(r => r.Sort is >= 29 and <= 32)
                //      .Select(x => new GroupSelect()
                //      {
                //          CountryId = x.TeamId,
                //          CountryName = x.TeamName,
                //          ImageName = $"{enCountry.First(r => r.CountryId == x.TeamId).CountryName.ToLower()}.png"
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
        /// <param name="promotionMission"></param>
        /// <param name="depositSum"></param>
        /// <param name="custTurnover"></param>
        private async Task TicketProcessNova88(int siteId, int custId, int currencyId ,List<PromotionMission> promotionMission , decimal depositSum , decimal custTurnover)
        {
            var userTickets = _cupEventPredictionService.GetCupEventMemberTicketInfo(custId).ToList();
            if (userTickets.Count >= 4)
            {
                //票滿了 不往下檢查
                SetPromotionMissionCompleted(promotionMission);
                return;
            }
            
            var insertTickets = new List<CustTicketInfoDto>();
            
            // 已領取的票
            var used = userTickets.Select(t => t.TicketType).ToList();
            
            // Set IsCompleted
            var completedMission = promotionMission.Where(m => used.Contains(m.MissionNo)).ToList();
            SetPromotionMissionCompleted(completedMission);
            
            // 排除已領過的missionNo
            promotionMission = promotionMission.Where(m => !used.Contains(m.MissionNo)).ToList();
            foreach (var mission in promotionMission)
            {
                var isInsert = false;
                var ticketInit = new CustTicketInfoDto
                {
                    SiteId = siteId,
                    CustId = custId,
                    CurrencyId = currencyId,
                    TicketStatus = 1,
                    TicketType = mission.MissionNo,
                    TransId = ""
                };

                if (mission.DepositFlag && depositSum > mission.DepositAmount)
                {
                    isInsert = true;
                }
                else if (mission.TurnoverFlag && custTurnover > mission.TurnoverAmount)
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
            if (insertTickets.Count > 0) tasks.AddRange(insertTickets.Select(item => Task.Run(() => _cupEventPredictionService.InsertCupEventMemberTicket(item))));
            if (tasks.Count > 0) await Task.WhenAll(tasks);
            
        }

        /// <summary>
        /// Goal123 發票任務邏輯
        /// </summary>
        /// <param name="siteId"></param>
        /// <param name="custId"></param>
        /// <param name="currencyId"></param>
        /// <param name="promotionMission"></param>
        /// <param name="depositList"></param>
        private async Task TicketProcessGoal123(int siteId, int custId, int currencyId ,PromotionMission promotionMission , IEnumerable<MemberDepositDto> depositList )
        {
            var userTickets = _cupEventPredictionService.GetCupEventMemberTicketInfo(custId).ToList();

            // 存款有達標的存款單
            depositList = depositList.Where(x => x.DepositAmount >= promotionMission.DepositAmount);
            
            // 已領取的存款單
            var used = userTickets.Select(t => t.TransId).ToList();
            
            // 排除已領過的存款單
            depositList = depositList.Where(m => !used.Contains(m.TransId)).ToList();
            
            var insertTickets = depositList.Select( x => 
                new CustTicketInfoDto {
                    SiteId = siteId, 
                    CustId = custId,
                    CurrencyId = currencyId, 
                    TicketStatus = 1,
                    TransId = x.TransId
                }).ToList();
            
            // 發票
            var tasks = new List<Task<int>>();
            if (insertTickets.Count > 0) tasks.AddRange(insertTickets.Select(item => Task.Run(() => _cupEventPredictionService.InsertCupEventMemberTicket(item))));
            if (tasks.Count > 0) await Task.WhenAll(tasks);
            
        }
        
        private void SetPromotionMissionCompleted(List<PromotionMission> promotionMission)
        {
            foreach (var mission in promotionMission)
            {
                mission.IsCompleted = true;
            }
        }

        private bool CheckCustVerifyStatus(int siteId ,int custId)
        {
            var custInfoProfile = _custInfoBll.GetCustInfoProfile(custId);

            var status = false;
            switch (siteId)
            {
                case 1 or 2:
                    if (custInfoProfile.PhoneVerified && custInfoProfile.EmailVerified) status = true;
                   break;
            }
            
            return status;
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
            var userTickets = _cupEventPredictionService.GetCupEventMemberTicketInfo(custId).Where(x=>x.TeamId is null);// 取得User尚未預測的票
            if (userTickets.Count() < count)
            {
                throw new Exception("not enough tickets");
            }
        }
        
        #endregion
        
        #endregion
    }
}
