using MemberSite.Service.BLL;
using MemberSite.Service.BLL.Interface;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace MemberSite.Controllers.Api
{
    //[AllowAnonymous]
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly AuthorizeBLL _authorizeBll;
        private readonly EventsBLL _eventsBll;
        private readonly IAngPaoBLL _angPaoBll;
        private readonly ICacheHelper _redisHelper;
        private readonly CustInfoBll _custInfoBll;
        private readonly NewYear2024BLL _newYear2024Bll;
        private readonly SelLangBLL _selLangBll;
        private readonly ServerInfoBll _serverInfoBll;
        private readonly MessageBll _messageBll;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly ILogger<EventsController> _logger;
        private readonly QueueJobBLL _queueJobBll;

        public EventsController(IServiceProvider provider)
        {
            _authorizeBll = provider.GetRequiredService<AuthorizeBLL>();
            _eventsBll = provider.GetRequiredService<EventsBLL>();
            _angPaoBll = provider.GetRequiredService<IAngPaoBLL>();
            _redisHelper = provider.GetRequiredService<ICacheHelper>();
            _custInfoBll = provider.GetRequiredService<CustInfoBll>();
            _selLangBll = provider.GetRequiredService<SelLangBLL>();
            _serverInfoBll = provider.GetRequiredService<ServerInfoBll>();
            _messageBll = provider.GetRequiredService<MessageBll>();
            _logger = provider.GetRequiredService<ILogger<EventsController>>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
            _newYear2024Bll = provider.GetRequiredService<NewYear2024BLL>();
            _queueJobBll = provider.GetRequiredService<QueueJobBLL>();
        }
        
        #region Leaderboard
        
        //[Authorize]
        [HttpPost]
        public ApiResult<IEnumerable<Leaderboard>> GetLeaderboardList(GetLeaderboardListReq getLeaderboardListV2Req)
        {
            var apiResult = new ApiResult<IEnumerable<Leaderboard>>();
            int siteId;
            int currencyId;

            var authData = _authorizeBll.GetAuthData();
            if (authData is {CustId: > 0})
            {
                var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
                siteId = custInfo.SiteId;
                currencyId = custInfo.CurrencyId;
            }
            else
            {
                var serverInfo = _serverInfoBll.GetServerInfo();
                siteId = serverInfo.SiteId;
                currencyId = _selLangBll.GetPreferCurrencyId();
            }

            apiResult.Data = _eventsBll.GetLeaderboardList(
                getLeaderboardListV2Req.LBCategoryId,
                siteId,
                currencyId,
                getLeaderboardListV2Req.TopNum,
                getLeaderboardListV2Req.BeforeDays);

            if (authData is {CustId: > 0})
            {
                foreach (var L in apiResult.Data)
                {
                    if (L.CustId != authData.CustId)
                        L.UserName = _eventsBll.MaskName(L.UserName);
                }
            }
            else
            {
                foreach (var L in apiResult.Data)
                {
                    L.UserName = _eventsBll.MaskName(L.UserName);
                }
            }

            apiResult.ErrorCode = 0;
            return apiResult;
        }

        [HttpPost]
        public ApiResult<IEnumerable<Leaderboard>> GetLeaderboardListMonth(
            GetLeaderboardListMonthReq getLeaderboardListMonthReq)
        {
            var apiResult = new ApiResult<IEnumerable<Leaderboard>>()
            {
                Data = new List<Leaderboard>()
            };
            int siteId;
            int currencyId;

            if (getLeaderboardListMonthReq.SelectDays.Month <= DateTime.UtcNow.AddMonths(-2).Month)
            {
                return apiResult;
            }

            var authData = _authorizeBll.GetAuthData();
            if (authData is {CustId: > 0})
            {
                var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
                siteId = custInfo.SiteId;
                currencyId = custInfo.CurrencyId;
            }
            else
            {
                var serverInfo = _serverInfoBll.GetServerInfo();
                siteId = serverInfo.SiteId;
                currencyId = _selLangBll.GetPreferCurrencyId();
            }

            apiResult.Data = _eventsBll.GetLeaderboardListMonth(
                getLeaderboardListMonthReq.LBCategoryId,
                siteId,
                currencyId,
                getLeaderboardListMonthReq.TopNum,
                getLeaderboardListMonthReq.SelectDays);

            if (authData is {CustId: > 0})
            {
                foreach (var L in apiResult.Data)
                {
                    if (L.CustId != authData.CustId)
                        L.UserName = _eventsBll.MaskName(L.UserName);
                }
            }
            else
            {
                foreach (var L in apiResult.Data)
                {
                    L.UserName = _eventsBll.MaskName(L.UserName);
                }
            }

            apiResult.ErrorCode = 0;
            return apiResult;
        }

        [HttpPost]
        [EnableCors("LeaderBoard")]
        public ApiResult<LeaderboardSetting> GetLeaderboardSetting(GetLeaderboardSettingReq req)
        {
            var result = new ApiResult<LeaderboardSetting>();
            
            try
            {
                var time = _dateTimeHelper.ToCurrencyZone(DateTime.UtcNow, req.CurrencyId).Result;
                var date = time.ToString("yyyyMM");
                
                var calculation = _eventsBll.GetLeaderboardPointCalculation(req.SiteId, date).FirstOrDefault(x=>x.CurrencyId == req.CurrencyId);
                var rankingBonus = _eventsBll.GetLeaderboardRankingBonus(req.SiteId,req.CurrencyId);

                var data = new LeaderboardSetting
                {
                    LeaderboardPointCalculation = calculation,
                    LeaderboardRankingBonus = rankingBonus
                };
                result.ErrorCode = 0;
                result.Data = data;
                
                return result;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "GetLeaderboardPointCalculation");
                return new ApiResult<LeaderboardSetting>
                {
                    Message = "System Error.",
                    ErrorCode = 9000
                };
            }

        }
        
        [Authorize]
        [HttpPost]
        public ApiResult<LeaderboardCustRank> GetCustRank(GetCustRankReq input)
        {
            var apiResult = new ApiResult<LeaderboardCustRank>
            {
                Data = new LeaderboardCustRank()
            };
            var authData = _authorizeBll.GetAuthData();
            if (authData is {CustId: > 0})
            {
                var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
                apiResult.Data = _eventsBll.GetCustRank(input.LBCategoryId, custInfo.SiteId, custInfo.CurrencyId, authData.CustId, input.BeforeDays);
                apiResult.ErrorCode = 0;
            }

            return apiResult;
        }

        [Authorize]
        [HttpPost]
        public ApiResult<LeaderboardCustRank> GetCustRankMonth(GetCustRankMonthReq getCustRankMonthReq)
        {
            var apiResult = new ApiResult<LeaderboardCustRank>
            {
                Data = new LeaderboardCustRank()
            };
            var authData = _authorizeBll.GetAuthData();
            if (authData is {CustId: > 0})
            {
                var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);

                var (custRank, point) = _eventsBll.GetCustRankWithCacheMonth(getCustRankMonthReq.LBCategoryId, custInfo.SiteId, custInfo.CurrencyId, authData.CustId, getCustRankMonthReq.SelectDays);

                apiResult.Data.CustRank = custRank;
                apiResult.Data.point = point;
                apiResult.ErrorCode = 0;
            }

            return apiResult;
        }
        
        #endregion

        #region DrawPrize
        
        [Authorize]
        [HttpPost]
        public async Task<ApiResult<CustGift>> CheckDrawPrize(DrawPrizeReq drawPrizeReq)
        {
            var apiResult = new ApiResult<CustGift>()
            {
                Data = new CustGift()
            };
            var authData = _authorizeBll.GetAuthData();
            var eventId = drawPrizeReq.EventId;

            if (authData is {CustId: > 0})
            {
                var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
                if (eventId is 12 or 13 or 14)
                {
                    var (errorCode, ticketCount) = await _angPaoBll.CheckDrawPrize(custInfo.CustId, custInfo.CurrencyId);
                    apiResult.Data.TicketCount = ticketCount;
                    apiResult.ErrorCode = errorCode;
                    apiResult.Message = apiResult.ErrorCode switch
                    {
                        0 => string.Format(_selLangBll.GetMsg("msg_times_chance"), ticketCount),
                        102 => _selLangBll.GetMsg("msg_turnover_not_enough"),
                        103 => _selLangBll.GetMsg("msg_limit_has_been_reached"),
                        _ => _selLangBll.GetMsg("msg_Ineligible"),
                    };
                }
                else
                {
                    //檢查活動,獎品狀態,使用者是否抽過
                    var prizes = _eventsBll.CheckDrawPrize(ref eventId, custInfo.CustId, custInfo.CurrencyId,
                        out var prizeId, out var result);
                    if (result == 100)
                    {
                        apiResult.Data.PrizeId = prizeId;
                        apiResult.Data.PrizeName = prizes.FirstOrDefault(x => x.PrizeId == prizeId)?.PrizeName;
                    }
                    else if (result == 0)
                    {
                        var eventDrawInfo = _eventsBll.GetEventDrawInfoWithCache(eventId);
                        //檢查使用者抽獎資格
                        if (eventDrawInfo.ConditionSp != null &&
                            !_eventsBll.CheckEventCondition(eventDrawInfo.ConditionSp, eventId, authData.CustId,
                                custInfo.CurrencyId))
                        {
                            result = 102; //使用者資格不符
                            apiResult.Message = _selLangBll.GetMsg("msg_Ineligible");
                        }
                    }
                    else
                    {
                        apiResult.Message = _selLangBll.GetMsg("msg_Ineligible");
                    }

                    apiResult.ErrorCode = result;
                }
            }

            return apiResult;
        }

        [Authorize]
        [HttpPost]
        public async Task<ApiResult<CustGift>> DrawPrize(DrawPrizeReq drawPrizeReq)
        {
            var apiResult = new ApiResult<CustGift>
            {
                Data = new CustGift()
            };
            var authData = _authorizeBll.GetAuthData();
            if (authData is {CustId: > 0})
            {
                var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
                var serverInfo = _serverInfoBll.GetServerInfo();
                var eventId = drawPrizeReq.EventId;

                #region AngPao

                // if (eventId is 12 or 13 or 14)
                // {
                //     var errorCode = -1;
                //     var key = $"AgPao:{custInfo.CustId}";
                //     try
                //     {
                //         var dAmount = "";
                //         var time = await _dateTimeHelper.ToCurrencyZone(DateTime.UtcNow, custInfo.CurrencyId);
                //         if (await _cacheHelper.SetNxAsync(key, "", TimeSpan.FromSeconds(5)))
                //         {
                //             var (error, prize) = await _angPaoBLL.DrawPrize(eventId, custInfo, time.DateTime);
                //             if (error is > 0 and < 100)
                //             {
                //                 apiResult.Data.PrizeId = prize.PrizeId;
                //                 apiResult.Data.PrizeName = prize.PrizeName;
                //                 apiResult.Data.Jackpot = prize.Jackpot;
                //                 if (prize.Amount > 0)
                //                 {
                //                     errorCode = 0;
                //                     _perCurrency.TryGetValue((int)custInfo.CurrencyId, out var currencyName);
                //                     dAmount = prize.Amount.ToString("#,##0.##");
                //                     if (_custInfoBll.IsConvertRequired(custInfo.CurrencyId))
                //                     {
                //                         dAmount = $"{prize.Amount / 1000:#,##0.##}";
                //                     }
                //                     var drawPrizeMessage = _eventsBll.GetDrawPrizeMessageWithCache(eventId, _selLangBll.GetLangId());
                //                     var prizeName = $"{currencyName} {dAmount}";
                //                     var title = drawPrizeMessage.Tital;
                //                     var msg = string.Format(drawPrizeMessage.Message, prizeName);
                //                     await _messageBll.SendMessage(new Message("System", custInfo.CustId, custInfo.UserLang, title, msg));
                //                 }
                //                 else
                //                 {
                //                     errorCode = 1;
                //                 }
                //             }
                //             else if (error > 100)
                //             {
                //                 errorCode = error;
                //             }
                //         }
                //         apiResult.ErrorCode = errorCode;
                //         apiResult.Message = errorCode switch
                //         {
                //             0 => string.Format(_selLangBll.GetMsg("msg_congratulation_event_reward"), dAmount),
                //             1 => _selLangBll.GetMsg("msg_keep_up"),
                //             102 => _selLangBll.GetMsg("msg_turnover_not_enough"),
                //             103 => _selLangBll.GetMsg("msg_limit_has_been_reached"),
                //             104 => _selLangBll.GetMsg("msg_ProfileOrPhoneNotFinish"),
                //             105 => _selLangBll.GetMsg("msg_Event_has_not_started"),
                //             _ => _selLangBll.GetMsg("msg_Ineligible"),
                //         };
                //     }
                //     catch (Exception ex)
                //     {
                //         _logger.LogError(ex, "drop prize failed");
                //     }
                //     finally
                //     {
                //         await _cacheHelper.KeyDeleteAsync(key);
                //     }
                // }

                #endregion

                if (eventId is 15 or 16 or 17)
                {
                    var errorCode = -1;

                    var prize = new Prize();
                    try
                    {
                        var lockKey = $"DrawPrizeLock:{custInfo.CustId}";
                        var lockValue = Guid.NewGuid().ToString(); // 每次取鎖使用唯一值，確保為同一個執行序所執行
                        var lockExpiry = TimeSpan.FromSeconds(15); //lock 15 sec        
                        
                        if (await _redisHelper.TryCreateLockAsync(lockKey, lockValue, lockExpiry))
                        {
                            var (error, prizeList) = await _newYear2024Bll.DrawPrize(eventId, custInfo, 1);
                        
                            if (prizeList is not null)
                            {
                                prize = prizeList.FirstOrDefault();
                                if (error is >= 0 and < 100)
                                {
                                    errorCode = DealDrawPrize(custInfo, prize);
                                }
                                apiResult.Data.DrawPrizeType = prize.DrawPrizeType;
                                apiResult.Data.PrizeId = prize.PrizeId;
                                apiResult.Data.PrizeName = prize.PrizeName;
                            }
                            else if (error > 100)
                            {
                                errorCode = error;
                            }
                            await _redisHelper.UnLockAsync(lockKey, lockValue);
                        }                          
                        
                        apiResult.ErrorCode = errorCode is > 100 or -1 ? errorCode : (int) ErrorCode.Success;
                        apiResult.Message = errorCode switch
                        {
                            0 => string.Format(_selLangBll.GetMsg("msg_congratulation_event_reward"), prize.Amount.ToString("#,##0.##") ),
                            1 => _selLangBll.GetMsg("msg_keep_up"),
                            2 => prize.PrizeName,
                            104 => _selLangBll.GetMsg("msg_ProfileOrPhoneNotFinish"),
                            105 => _selLangBll.GetMsg("msg_Event_has_not_started"),
                            106 or 102 => "Not Enough Eggs",
                            _ => _selLangBll.GetMsg("msg_Ineligible"),
                        };
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "drop prize failed");
                    }
                }
                else
                {
                    var eventDrawInfo = _eventsBll.GetEventDrawInfoWithCache(eventId);

                    if ((custInfo.Status != (int) CustStatus.Running ||
                         custInfo.ActStatus != (int) CustActStatus.Depoisted) && (eventId < 6))
                    {
                        apiResult.ErrorCode = 102;
                        apiResult.Message = _selLangBll.GetMsg("msg_Ineligible");
                    }
                    else if ((eventId == 1 || eventId == 2 || eventId == 3) &&
                             (custInfo.SiteId == 2 || custInfo.SiteId == 3) &&
                             _custInfoBll.GetCustTransSummary(authData.CustId).DepositSum < 200000)
                    {
                        apiResult.ErrorCode = 102;
                        apiResult.Message = _selLangBll.GetMsg("msg_Ineligible");
                    }
                    else if (eventDrawInfo.ConditionSp != null &&
                             !_eventsBll.CheckEventCondition(eventDrawInfo.ConditionSp, eventId, authData.CustId,
                                 custInfo.CurrencyId))
                    {
                        apiResult.ErrorCode = 102;
                        apiResult.Message = _selLangBll.GetMsg("msg_Ineligible");
                    }
                    else
                    {
                        _eventsBll.DrawPrize(ref eventId, custInfo.CustId, custInfo.CurrencyId, custInfo.UserName,
                            custInfo.SiteId, out var result, out var prize);
                        if (result == 100 || result == 0)
                        {
                            apiResult.Data.PrizeId = prize.PrizeId;
                            apiResult.Data.PrizeName = prize.PrizeName;
                            apiResult.Data.Jackpot = prize.Jackpot;
                        }

                        if (result == 0)
                        {
                            var currencyName = Enum.GetName(typeof(CurrencyId), custInfo.CurrencyId);
                            var dAmount = prize.Amount.ToString("#,##0.##");
                            if (_custInfoBll.IsConvertRequired(custInfo.CurrencyId))
                            {
                                dAmount = $"{prize.Amount / 1000:#,##0.##}";
                            }

                            var Tital = string.Format(_selLangBll.GetMsg("msg_DrawPrizeTital1"), serverInfo.SiteName);

                            var msg = string.Format(_selLangBll.GetMsg("msg_DrawPrize1"), Tital, currencyName, dAmount);

                            if (eventId > 8) //得獎訊息抽出到db設定
                            {
                                var Message = _eventsBll.GetDrawPrizeMessageWithCache(eventId, _selLangBll.GetLangId());
                                if (!string.IsNullOrEmpty(Message.Tital) && !string.IsNullOrEmpty(Message.Message))
                                {
                                    var prizeName = $"{currencyName} {dAmount}";
                                    if (_selLangBll.GetLangId() == "vn")
                                        prizeName = $"{dAmount} {currencyName}";
                                    Tital = Message.Tital;
                                    msg = string.Format(Message.Message, prizeName);
                                }
                            }

                            await _messageBll.SendMessage(new Message("System", custInfo.CustId, custInfo.UserLang,
                                Tital, msg));
                        }
                        else
                        {
                            apiResult.Message = _selLangBll.GetMsg("msg_Ineligible");
                        }

                        apiResult.ErrorCode = result;
                    }
                }
            }

            return apiResult;
        }

        [Authorize]
        [HttpPost]
        public async Task<ApiResult<List<CustGift>>> MultiDrawPrize(MultiDrawPrizeReq drawPrizeReq)
        {
            var apiResult = new ApiResult<List<CustGift>>
            {
                Data = new List<CustGift>()
            };

            var authData = _authorizeBll.GetAuthData();
            if (authData is {CustId: > 0})
            {
                var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
                var eventId = drawPrizeReq.EventId;

                var lockKey = $"DrawPrizeLock:{custInfo.CustId}";
                var lockValue = Guid.NewGuid().ToString(); // 每次取鎖使用唯一值，確保為同一個執行序所執行
                var lockExpiry = TimeSpan.FromSeconds(15); //lock 15 sec        
                if (await _redisHelper.TryCreateLockAsync(lockKey, lockValue, lockExpiry))
                {
                    if (eventId is 15 or 16 or 17)
                    {
                        var errorCode = -1;
                        try
                        {
                            var (error, prizeList) = await _newYear2024Bll.DrawPrize(eventId, custInfo, drawPrizeReq.DrawTimes);

                            if (error > 100)
                            {
                                errorCode = error;
                            }
                            else if (error is >= 0 and < 100) 
                            {
                                foreach (var prize in prizeList)
                                {
                                    DealDrawPrize(custInfo, prize);
                              
                                    var prizeData = new CustGift
                                    {
                                        PrizeId = prize.PrizeId,
                                        PrizeName = prize.PrizeName,
                                        DrawPrizeType = prize.DrawPrizeType
                                    };
                                    apiResult.Data.Add(prizeData);
                                }

                                errorCode = 0;
                            }

                            apiResult.ErrorCode = errorCode == (int)ErrorCode.Success ? (int)ErrorCode.Success : errorCode ;
                            apiResult.Message = errorCode switch
                            {
                                0 => "Success",
                                104 => _selLangBll.GetMsg("msg_ProfileOrPhoneNotFinish"),
                                105 => _selLangBll.GetMsg("msg_Event_has_not_started"),
                                106 or 102 => "Not Enough Eggs",
                                _ => _selLangBll.GetMsg("msg_Ineligible"),
                            };
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "drop prize failed");
                        }
                    }

                    await _redisHelper.UnLockAsync(lockKey, lockValue);
                }   
            }

            return apiResult;
        }

        [Authorize]
        [HttpPost]
        public async Task<ApiResult<IEnumerable<CustGiftList>>> GetCustDrawPrizeList(DrawPrizeReq drawPrizeReq)
        {
            var apiResult = new ApiResult<IEnumerable<CustGiftList>>();
            var authData = _authorizeBll.GetAuthData();
            if (authData is {CustId: > 0})
            {
                var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
                var eventId = drawPrizeReq.EventId;
                if (eventId is 12 or 13 or 14)
                {
                    apiResult.Data = await _angPaoBll.GetTodayCustDrawPrizeList(eventId, custInfo.CustId, custInfo.CurrencyId);
                }
                else
                {
                    apiResult.Data = _eventsBll.GetCustDrawPrizeList(eventId, custInfo.SiteId, authData.CustId);
                }

                if (eventId is 15 or 16 or 17)
                {
                    foreach (var gift in apiResult.Data)
                    {
                        if (gift.PrizeName == "Advant Play Free Bet")
                        {
                            gift.PrizeName = string.Concat(gift.PrizeName, $" {(int)gift.Amount}");
                        }
                    }
                }

                apiResult.ErrorCode = 0;
            }

            return apiResult;
        }

        [HttpPost]
        public ApiResult<IEnumerable<PrizeDisPlayList>> GetDisplayDrawPrizeList(DrawPrizeReq drawPrizeReq)
        {
            try
            {
                var currencyId = 0;
                var authData = _authorizeBll.GetAuthData();
                if (authData is {CustId: > 0})
                {
                    var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
                    currencyId = custInfo.CurrencyId;
                }
                else
                {
                    currencyId = _selLangBll.GetPreferCurrencyId();
                }

                var apiResult = new ApiResult<IEnumerable<PrizeDisPlayList>>
                {
                    Data = _eventsBll.GetDrawPrizeDisPlayListWithCache(drawPrizeReq.EventId, currencyId),
                    ErrorCode = 0
                };
                return apiResult;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "GetDisplayDrawPrizeList");
                return new ApiResult<IEnumerable<PrizeDisPlayList>>
                {
                    Message = "System Error.",
                    ErrorCode = 9000
                };
            }

        }
        
        [Authorize]
        [HttpPost]
        public async Task<ApiResult<CheckPromotionMissionResponse>> CheckPromotionMission()
        {
            var apiResult = new ApiResult<CheckPromotionMissionResponse>();

            var authData = _authorizeBll.GetAuthData();

            if (authData is {CustId: > 0})
            {
                var data = new CheckPromotionMissionResponse();

                var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
                var custPromotionMission = await _newYear2024Bll.CheckCustPromotionMission(custInfo);
                var eggTicket = await _newYear2024Bll.GetEggTicket(custInfo.CustId);

                if (custPromotionMission is not null)
                {
                    var claimMission = custPromotionMission.Where(x => x.IsCompleted);
                    if (claimMission.Any())
                    {
                        eggTicket = _newYear2024Bll.ClaimPromotionMission(custInfo, claimMission, eggTicket);
                    }

                    data.EggTicket = eggTicket;
                    data.NewYearMission2024 = custPromotionMission;
                    apiResult.Data = data;
                }
                else
                {
                    apiResult.Message = _selLangBll.GetMsg("msg_Ineligible");
                }

                apiResult.ErrorCode = (int) ErrorCode.Success;
            }

            return apiResult;
        }

        [Authorize]
        [HttpPost]
        public async Task<ApiResult> ClaimNewYearBigPrize(DrawPrizeReq req)
        {
            var apiResult = new ApiResult();

            var authData = _authorizeBll.GetAuthData();
            if (authData is {CustId: > 0})
            {
                var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
                
                var lockKey = $"ClaimNewYearBigPrizeLock:{custInfo.CustId}";
                var lockValue = Guid.NewGuid().ToString(); // 每次取鎖使用唯一值，確保為同一個執行序所執行
                var lockExpiry = TimeSpan.FromSeconds(15); //lock 15 sec        
                if (await _redisHelper.TryCreateLockAsync(lockKey, lockValue, lockExpiry))
                {
                    var (result,prize) = await _newYear2024Bll.ClaimNewYearBigPrize(custInfo, req.EventId);
                
                    switch (result)
                    {
                        case >= 0:
                            SendDrawPrizeMessage(req.EventId, custInfo, prize);
                            apiResult.ErrorCode = (int) ErrorCode.Success;
                            break;
                    
                        case -1:
                            apiResult.Message = _selLangBll.GetMsg("msg_Ineligible");
                            break;
                    
                        case -2:
                            apiResult.Message = "Already Claimed";
                            break;
                    }

                    await _redisHelper.UnLockAsync(lockKey, lockValue);
                }            
            }

            return apiResult;
        }
        
        [Authorize]
        [HttpPost]
        public async Task<ApiResult<CustBlessingsStickerResponse>> GetCustBlessingsSticker(DrawPrizeReq req)
        {
            var apiResult = new ApiResult<CustBlessingsStickerResponse>
            {
                Data = new CustBlessingsStickerResponse()
            };

            var authData = _authorizeBll.GetAuthData();

            if (authData is {CustId: > 0})
            {
                var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
                var sticker = await _newYear2024Bll.GetCustBlessingsSticker(custInfo.CustId);
                var isClaimed = _newYear2024Bll.CheckNewYearBigPrize(custInfo, req.EventId);
                
                apiResult.Data.Stickers = sticker;
                apiResult.Data.IsClaimedBigPrize = isClaimed;
                apiResult.ErrorCode = (int) ErrorCode.Success;
            }

            return apiResult;
        }

        #region Private

        private int DealDrawPrize(Customer custInfo, Prize prize)
        {
            var errorCode = -1;
            
            if (prize.Amount > 0)
            {
                errorCode = 0;
            }

            // PrizeType 1: 金額類  , PrizeType = 2 物件類
            switch (prize.PrizeType)
            {
                // 再接再厲
                case 1 when prize.Amount <= 0:
                    prize.DrawPrizeType = DrawPrizeTypeEnum.NoPrize;
                    errorCode = 1;
                    break;
                
                // 物件類型的獎品
                case 2 when prize.PrizeName != "Blessings Sticker":
                {
                    prize.DrawPrizeType = DrawPrizeTypeEnum.NormalPrize;
                    
                    // 進Queue Worker
                    var data = new DrawPrizeQueueJob()
                    {
                        PrizeId = prize.PrizeId,
                        PrizeName = prize.PrizeName
                    };
                    _queueJobBll.InsertQueueJob(custInfo.CustId, QueueJob.DrawPrize, JsonConvert.SerializeObject(data));
                    errorCode = 2;
                    break;
                }

                case 2 when prize.PrizeName == "Blessings Sticker":
                {
                    prize.DrawPrizeType = DrawPrizeTypeEnum.BlessingsSticker;
                    errorCode = 2;
                    break;
                }
            }

            return errorCode;
        }

        private void SendDrawPrizeMessage(int eventId, Customer custInfo, Prize prize)
        {
            var drawPrizeMessage = _eventsBll.GetDrawPrizeMessageWithCache(eventId, _selLangBll.GetLangId());
            var currencyName = Enum.GetName(typeof(CurrencyId), custInfo.CurrencyId);
            var prizeName = $"{currencyName} {prize.PrizeName}";
            var title = string.Format(drawPrizeMessage.Tital, prizeName);
            var msg = string.Format(drawPrizeMessage.Message, prizeName);
            _messageBll.SendMessage(new Message("System", custInfo.CustId, custInfo.UserLang, title, msg));
        }

        #endregion
        
        #endregion
    }
}
