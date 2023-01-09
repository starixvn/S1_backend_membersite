using MemberSite.Service.BLL;
using MemberSite.Service.BLL.Interface;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace MemberSite.Controllers.Api
{
    //[AllowAnonymous]
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly AuthorizeBLL _authorizeBll;
        private readonly IEventsBLL _eventsBll;
        private readonly CustInfoBll _custInfoBll;
        private readonly SelLangBLL _selLangBll;
        private readonly ServerInfoBll _serverInfoBll;
        private readonly MessageBll _messageBll;
        private readonly Dictionary<int, string> _perCurrency;
        private readonly ILogger<EventsController> _logger;

        public EventsController(IServiceProvider provider)

        {
            _authorizeBll = provider.GetRequiredService<AuthorizeBLL>();
            _eventsBll = provider.GetRequiredService<IEventsBLL>();
            _custInfoBll = provider.GetRequiredService<CustInfoBll>();
            _selLangBll = provider.GetRequiredService<SelLangBLL>();
            _serverInfoBll = provider.GetRequiredService<ServerInfoBll>();
            _messageBll = provider.GetRequiredService<MessageBll>();
            _logger = provider.GetRequiredService<ILogger<EventsController>>();

            _perCurrency = new Dictionary<int, string>()
            {
              {2,"MYR" },{4,"THB" },{13,"CNY" },{15,"IDR" },{51,"VND" },{61,"INR" },{96,"USDT" }
            };
        }

        public class GetLeaderboardListReq
        {
            public int LBCategoryId { get; set; }
            public int TopNum { get; set; }
            public int BeforeDays { get; set; }
        }
        [Authorize]
        [HttpPost]
        public ApiResult<IEnumerable<Leaderboard>> GetLeaderboardList(GetLeaderboardListReq getLeaderboardListReq)
        {
            var apiResult = new ApiResult<IEnumerable<Leaderboard>>();
            var authData = _authorizeBll.GetAuthData();
            var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);

            apiResult.Data = _eventsBll.GetLeaderboardListWithCache(getLeaderboardListReq.LBCategoryId, custInfo.SiteId, custInfo.CurrencyId, getLeaderboardListReq.TopNum, getLeaderboardListReq.BeforeDays);
            foreach (var L in apiResult.Data)
            {
                if (L.CustId != authData.CustId)
                    L.UserName = _eventsBll.MaskName(L.UserName);
            }
            apiResult.ErrorCode = 0;
            return apiResult;
        }
        //private string MaskName(string input)
        //{
        //    if (string.IsNullOrEmpty(input))
        //        return input;

        //    string pattern = @"(?<=[\w]{2})[\w-\._\+%]*(?=[\w]{2})";
        //    if (input.Length < 6)
        //        pattern = @"(?<=[\w]{1})[\w-\._\+%]*(?=[\w]{1})";
        //    return Regex.Replace(input, pattern, m => new string('*', m.Length));
        //}
        public class GetCustRankReq
        {
            public int LBCategoryId { get; set; }
            public int BeforeDays { get; set; }
        }
        [Authorize]
        [HttpPost]
        public ApiResult<LeaderboardCustRank> GetCustRank(GetCustRankReq getCustRankReq)
        {
            var apiResult = new ApiResult<LeaderboardCustRank>
            {
                Data = new LeaderboardCustRank()
            };
            var authData = _authorizeBll.GetAuthData();

            _eventsBll.GetCustRankWithCache(getCustRankReq.LBCategoryId, authData.CustId, getCustRankReq.BeforeDays, out var custRank, out var point);

            apiResult.Data.CustRank = custRank;
            apiResult.Data.point = point;
            apiResult.ErrorCode = 0;
            return apiResult;
        }

        public class GetLeaderboardListV2Req
        {
            public int LBCategoryId { get; set; }
            public string EventCode { get; set; }
            public int TopNum { get; set; }
            public int BeforeDays { get; set; }
        }
        //[Authorize]
        [HttpPost]
        public ApiResult<IEnumerable<Leaderboard>> GetLeaderboardListV2(GetLeaderboardListV2Req getLeaderboardListV2Req)
        {
            var apiResult = new ApiResult<IEnumerable<Leaderboard>>();
            var custId = 0;
            var siteId = 0;
            var currencyId = 0;
            
            var authData = _authorizeBll.GetAuthData();
            if (authData is { CustId: > 0 })
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

            apiResult.Data = JsonSerializer.Deserialize<IEnumerable<Leaderboard>>(JsonSerializer.Serialize(
                _eventsBll.GetLeaderboardListWithCacheV2(getLeaderboardListV2Req.EventCode,
                    getLeaderboardListV2Req.LBCategoryId, siteId, currencyId,
                    getLeaderboardListV2Req.TopNum, getLeaderboardListV2Req.BeforeDays)));
            
            foreach (var L in apiResult.Data)
            {
                if (L.CustId != custId)
                    L.UserName = _eventsBll.MaskName(L.UserName);
            }
            apiResult.ErrorCode = 0;
            return apiResult;
        }
        
        //[Authorize]
        [HttpPost]
        public ApiResult<IEnumerable<Leaderboard>> GetLeaderboardListV3(GetLeaderboardListV2Req getLeaderboardListV2Req)
        {
            var apiResult = new ApiResult<IEnumerable<Leaderboard>>();
            var custId = 0;
            var siteId = 0;
            var currencyId = 0;
            
            var authData = _authorizeBll.GetAuthData();
            if (authData is { CustId: > 0 })
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

            apiResult.Data = JsonSerializer.Deserialize<IEnumerable<Leaderboard>>(JsonSerializer.Serialize(
                _eventsBll.GetLeaderboardListWithCacheV3(getLeaderboardListV2Req.EventCode,
                    getLeaderboardListV2Req.LBCategoryId, siteId, currencyId,
                    getLeaderboardListV2Req.TopNum, getLeaderboardListV2Req.BeforeDays)));

            foreach (var L in apiResult.Data)
            {
                if (L.CustId != custId)
                    L.UserName = _eventsBll.MaskName(L.UserName);
            }
            apiResult.ErrorCode = 0;
            return apiResult;
        }

        public class GetLeaderboardListMonthReq
        {
            public int LBCategoryId { get; set; }
            public string EventCode { get; set; }
            public int TopNum { get; set; }
            public DateTime SelectDays { get; set; }
        }
        //[Authorize]
        [HttpPost]
        public ApiResult<IEnumerable<Leaderboard>> GetLeaderboardListMonth(GetLeaderboardListMonthReq getLeaderboardListMonthReq)
        {
            var apiResult = new ApiResult<IEnumerable<Leaderboard>>();
            var custId = 0;
            var siteId = 0;
            var currencyId = 0;
            
            var authData = _authorizeBll.GetAuthData();
            if (authData is { CustId: > 0 })
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

            apiResult.Data = JsonSerializer.Deserialize<IEnumerable<Leaderboard>>(JsonSerializer.Serialize(
                _eventsBll.GetLeaderboardListMonthWithCache(getLeaderboardListMonthReq.EventCode,
                    getLeaderboardListMonthReq.LBCategoryId, siteId, currencyId,
                    getLeaderboardListMonthReq.TopNum, getLeaderboardListMonthReq.SelectDays)));

            foreach (var L in apiResult.Data)
            {
                if (L.CustId != custId)
                    L.UserName = _eventsBll.MaskName(L.UserName);
            }
            apiResult.ErrorCode = 0;
            return apiResult;
        }
        public class GetCustRankV2Req
        {
            public int LBCategoryId { get; set; }
            public string EventCode { get; set; }
            public int BeforeDays { get; set; }
        }
        [Authorize]
        [HttpPost]
        public ApiResult<LeaderboardCustRank> GetCustRankV2(GetCustRankV2Req getCustRankV2Req)
        {
            var apiResult = new ApiResult<LeaderboardCustRank>
            {
                Data = new LeaderboardCustRank()
            };
            var authData = _authorizeBll.GetAuthData();

            _eventsBll.GetCustRankWithCacheV2(getCustRankV2Req.LBCategoryId, getCustRankV2Req.EventCode, authData.CustId, getCustRankV2Req.BeforeDays, out var custRank, out var point);

            _eventsBll.GetCustRankWithCacheV2(getCustRankV2Req.LBCategoryId, getCustRankV2Req.EventCode, authData.CustId, getCustRankV2Req.BeforeDays+1, out var yesterdaycustRank, out var yesterdaypoint);
            
            apiResult.Data.CustRank = custRank;
            apiResult.Data.point = point;
            apiResult.Data.Status = -custRank.CompareTo(yesterdaycustRank);
            apiResult.ErrorCode = 0;
            return apiResult;
        }

        public class GetCustRankMonthReq
        {
            public int LBCategoryId { get; set; }
            public string EventCode { get; set; }
            public DateTime SelectDays { get; set; }
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

            _eventsBll.GetCustRankWithCacheMonth(getCustRankMonthReq.LBCategoryId, getCustRankMonthReq.EventCode, authData.CustId, getCustRankMonthReq.SelectDays, out var custRank, out var point);

            apiResult.Data.CustRank = custRank;
            apiResult.Data.point = point;
            apiResult.ErrorCode = 0;
            return apiResult;
        }

        public class DrawPrizeReq
        {
            public int eventId { get; set; }
        }
        [Authorize]
        [HttpPost]
        public async Task<ApiResult<CustGift>> CheckDrawPrize(DrawPrizeReq drawPrizeReq)
        {
            var apiResult = new ApiResult<CustGift>()
            {
                Data = new CustGift()
            };
            var authData = _authorizeBll.GetAuthData();
            var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
            var eventId = drawPrizeReq.eventId;
            if (eventId == 12 || eventId == 13 || eventId == 14)
            {
                var (errorCode, ticketCount) = await _eventsBll.CheckDrawPrize(custInfo.CustId, custInfo.CurrencyId);
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
                var prizes = _eventsBll.CheckDrawPrize(ref eventId, custInfo.SiteId, custInfo.CustId, custInfo.CurrencyId, out var prizeId, out var result);
                if (result == 100)
                {
                    apiResult.Data.PrizeId = prizeId;
                    apiResult.Data.PrizeName = prizes.FirstOrDefault(x => x.PrizeId == prizeId)?.PrizeName;
                }
                else if (result == 0)
                {
                    var eventDrawInfo = _eventsBll.GetEventDrawInfoWithCache(eventId);
                    //檢查使用者抽獎資格
                    if (eventDrawInfo.ConditionSp != null && !_eventsBll.CheckEventCondition(eventDrawInfo.ConditionSp, eventId, authData.CustId, custInfo.CurrencyId))
                    {
                        result = 102;//使用者資格不符
                        apiResult.Message = _selLangBll.GetMsg("msg_Ineligible");
                    }
                }
                else
                {
                    apiResult.Message = _selLangBll.GetMsg("msg_Ineligible");
                }
                apiResult.ErrorCode = result;
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
            var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
            var serverInfo = _serverInfoBll.GetServerInfo();
            var eventId = drawPrizeReq.eventId;

            if (eventId == 12 || eventId == 13 || eventId == 14)
            {
                var errorCode = -1;

                var (error,  prize) = await _eventsBll.DrawPrize(eventId, custInfo);
                if (error > 0 && error < 100)
                {
                    apiResult.Data.PrizeId = prize.PrizeId;
                    apiResult.Data.PrizeName = prize.PrizeName;
                    apiResult.Data.Jackpot = prize.Jackpot;
                    if (prize.Amount > 0)
                    {
                        errorCode = 0;
                        _perCurrency.TryGetValue((int)custInfo.CurrencyId, out var currencyName);
                        var dAmount = prize.Amount.ToString("#,##0.##");
                        if (_custInfoBll.IsConvertRequired(custInfo.CurrencyId))
                        {
                            dAmount = $"{prize.Amount / 1000:#,##0.##}";
                        }
                        var drawPrizeMessage = _eventsBll.GetDrawPrizeMessageWithCache(eventId, _selLangBll.GetLangId());
                        var prizeName = $"{currencyName} {dAmount}";
                        var title = drawPrizeMessage.Tital;
                        var msg = string.Format(drawPrizeMessage.Message, prizeName);
                        _messageBll.SendMessage(custInfo.SiteId, "DrawPrize", title, msg, custInfo.CustId, custInfo.UserName, custInfo.UserLang);
                    }
                    else
                    {
                        errorCode = 1;
                    }
                }
                else if (error > 100) 
                {
                    errorCode = error;
                }
                apiResult.ErrorCode = errorCode;
                apiResult.Message = errorCode switch
                {
                    0 => string.Format(_selLangBll.GetMsg("msg_congratulation_event_reward"), prize.Amount.ToString("#,##0.##")),
                    1 => _selLangBll.GetMsg("msg_keep_up"),
                    102 => _selLangBll.GetMsg("msg_turnover_not_enough"),
                    103 => _selLangBll.GetMsg("msg_limit_has_been_reached"),
                    104 => _selLangBll.GetMsg("msg_ProfileOrPhoneNotFinish"),
                    _ => _selLangBll.GetMsg("msg_Ineligible"),
                };
            }
            else 
            {
                var eventDrawInfo = _eventsBll.GetEventDrawInfoWithCache(eventId);

                if ((custInfo.Status != (int)CustStatus.Running || custInfo.ActStatus != (int)CustActStatus.Depoisted) && (eventId < 6))
                {
                    apiResult.ErrorCode = 102;
                    apiResult.Message = _selLangBll.GetMsg("msg_Ineligible");
                }
                else if ((eventId == 1 || eventId == 2 || eventId == 3) && (custInfo.SiteId == 2 || custInfo.SiteId == 3) && _custInfoBll.GetCustTransSummary(authData.CustId).DepositSum < 200000)
                {
                    apiResult.ErrorCode = 102;
                    apiResult.Message = _selLangBll.GetMsg("msg_Ineligible");
                }
                else if (eventDrawInfo.ConditionSp != null && !_eventsBll.CheckEventCondition(eventDrawInfo.ConditionSp, eventId, authData.CustId, custInfo.CurrencyId))
                {
                    apiResult.ErrorCode = 102;
                    apiResult.Message = _selLangBll.GetMsg("msg_Ineligible");
                }
                else
                {
                    var ress = _eventsBll.DrawPrize(ref eventId, custInfo.CustId, custInfo.CurrencyId, custInfo.UserName, custInfo.SiteId, out var result, out var prize);
                    if (result == 100 || result == 0)
                    {
                        apiResult.Data.PrizeId = prize.PrizeId;
                        apiResult.Data.PrizeName = prize.PrizeName;
                        apiResult.Data.Jackpot = prize.Jackpot;
                    }

                    if (result == 0)
                    {

                        _perCurrency.TryGetValue((int)custInfo.CurrencyId, out var currencyName);
                        var dAmount = prize.Amount.ToString("#,##0.##");
                        if (_custInfoBll.IsConvertRequired(custInfo.CurrencyId))
                        {
                            dAmount = $"{prize.Amount / 1000:#,##0.##}";
                        }
                        var Tital = string.Format(_selLangBll.GetMsg("msg_DrawPrizeTital1"), serverInfo.SiteName);

                        var msg = string.Format(_selLangBll.GetMsg("msg_DrawPrize1"), Tital, currencyName, dAmount);
                        //活動已結束 找時間移除
                        //if ((eventId == 1 || eventId == 2 || eventId == 3))//old event
                        //{
                        //    Tital = selLangBLL.GetMsg("msg_DrawPrizeTital");

                        //    msg = string.Format(selLangBLL.GetMsg("msg_DrawPrize"), dAmount);
                        //}
                        if (eventId > 8)//得獎訊息抽出到db設定
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
                        _messageBll.SendMessage(custInfo.SiteId, "DrawPrize", Tital, msg, custInfo.CustId, custInfo.UserName, custInfo.UserLang);
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
        public async Task<ApiResult<IEnumerable<CustGiftList>>> GetCustDrawPrizeList(DrawPrizeReq drawPrizeReq)
        {
            var apiResult = new ApiResult<IEnumerable<CustGiftList>>();
            var authData = _authorizeBll.GetAuthData();
            var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
            var eventId = drawPrizeReq.eventId;
            if (eventId == 12 || eventId == 13 || eventId == 14)
            {
                apiResult.Data = await _eventsBll.GetTodayCustDrawPrizeList(eventId, custInfo.CustId, custInfo.CurrencyId);
            }
            else 
            {
                apiResult.Data = _eventsBll.GetCustDrawPrizeList(eventId, custInfo.SiteId, authData.CustId);
            }
            apiResult.ErrorCode = 0;
            return apiResult;
        }
        
        [HttpPost]
        public ApiResult<IEnumerable<PrizeDisPlayList>> GetDisplayDrawPrizeList(DrawPrizeReq drawPrizeReq)
        {
            try
            {
                var currencyId = 0;
                var authData = _authorizeBll.GetAuthData();
                if (authData is { CustId: > 0 })
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
                    Data = _eventsBll.GetDrawPrizeDisPlayListWithCache(drawPrizeReq.eventId, currencyId),
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

        [HttpPost]
        public ApiResult<LeaderboardEvent> GetLeaderboardEventCode()
        {
            var apiResult = new ApiResult<LeaderboardEvent>
            {
                Data = new LeaderboardEvent()
            };
            var siteId = 0;
            string NowEventCode = null;
            string LastEventCode = null;
           
            var authData = _authorizeBll.GetAuthData();
            if (authData is { CustId: > 0 })
            {
                var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
                siteId = custInfo.SiteId;
            }
            else
            {
                var serverInfo = _serverInfoBll.GetServerInfo();
                siteId = serverInfo.SiteId;
            }
            _eventsBll.GetLeaderboardEventCode(siteId, out NowEventCode, out LastEventCode);
            apiResult.Data.NowEventCode = NowEventCode;
            apiResult.Data.LastEventCode = LastEventCode;
            apiResult.ErrorCode = 0;
            return apiResult;
        }

        public class LeaderboardInfoReq
        {
            public string EventCode { get; set; }
            
        }
        [HttpPost]
        public ApiResult<LeaderboardInfo> GetLeaderboardInfo(LeaderboardInfoReq leaderboardInfoReq)
        {
            var apiResult = new ApiResult<LeaderboardInfo>();
            var siteId = 0;
            var currencyId = 0;
            var authData = _authorizeBll.GetAuthData();
            if (authData is { CustId: > 0 })
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

            apiResult.Data = _eventsBll.GetLeaderboardInfoWithCache(leaderboardInfoReq.EventCode , siteId, currencyId);
            apiResult.ErrorCode = 0;
            return apiResult;
        }

    }
}
