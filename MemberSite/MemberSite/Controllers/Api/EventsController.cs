using MemberSite.Service.BLL;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MemberSite.Controllers.Api
{
    //[AllowAnonymous]
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly AuthorizeBLL authorizeBLL;
        private readonly EventsBLL eventsBLL;
        private readonly CustInfoBLL custInfoBLL;
        private readonly SelLangBLL selLangBLL;
        private readonly ServerInfoBLL serverInfoBLL;
        private readonly MessageBLL messageBLL;

        public EventsController(AuthorizeBLL authorizeBLL, EventsBLL eventsBLL, CustInfoBLL custInfoBLL, SelLangBLL selLangBLL, ServerInfoBLL serverInfoBLL, MessageBLL messageBLL)

        {
            this.authorizeBLL = authorizeBLL;
            this.eventsBLL = eventsBLL;
            this.custInfoBLL = custInfoBLL;
            this.selLangBLL = selLangBLL;
            this.serverInfoBLL = serverInfoBLL;
            this.messageBLL = messageBLL;
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
            var authData = authorizeBLL.GetAuthData();
            var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);

            apiResult.Data = eventsBLL.GetLeaderboardListWithCache(authData.CustId, getLeaderboardListReq.LBCategoryId, custInfo.SiteId, custInfo.CurrencyId, getLeaderboardListReq.TopNum, getLeaderboardListReq.BeforeDays);
            foreach (var L in apiResult.Data)
            {
                if (L.CustId != authData.CustId)
                    L.UserName = eventsBLL.MaskName(L.UserName);
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
            var apiResult = new ApiResult<LeaderboardCustRank>();
            apiResult.Data = new LeaderboardCustRank();
            var authData = authorizeBLL.GetAuthData();

            eventsBLL.GetCustRankWithCache(getCustRankReq.LBCategoryId, authData.CustId, getCustRankReq.BeforeDays, out int custRank, out Decimal point);

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
        [Authorize]
        [HttpPost]
        public ApiResult<IEnumerable<Leaderboard>> GetLeaderboardListV2(GetLeaderboardListV2Req getLeaderboardListV2Req)
        {
            var apiResult = new ApiResult<IEnumerable<Leaderboard>>();
            var authData = authorizeBLL.GetAuthData();
            var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);

            apiResult.Data = eventsBLL.GetLeaderboardListWithCacheV2(authData.CustId, getLeaderboardListV2Req.EventCode, getLeaderboardListV2Req.LBCategoryId, custInfo.SiteId, custInfo.CurrencyId, getLeaderboardListV2Req.TopNum, getLeaderboardListV2Req.BeforeDays);
            foreach (var L in apiResult.Data)
            {
                if (L.CustId != authData.CustId)
                    L.UserName = eventsBLL.MaskName(L.UserName);
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
            var apiResult = new ApiResult<LeaderboardCustRank>();
            apiResult.Data = new LeaderboardCustRank();
            var authData = authorizeBLL.GetAuthData();

            eventsBLL.GetCustRankWithCacheV2(getCustRankV2Req.LBCategoryId, getCustRankV2Req.EventCode, authData.CustId, getCustRankV2Req.BeforeDays, out int custRank, out Decimal point);

            apiResult.Data.CustRank = custRank;
            apiResult.Data.point = point;
            apiResult.ErrorCode = 0;
            return apiResult;
        }
        [Authorize]
        [HttpPost]
        public ApiResult<CustGift> CheckDrawPrize(int eventId)
        {
            var apiResult = new ApiResult<CustGift>();
            apiResult.Data = new CustGift();
            var authData = authorizeBLL.GetAuthData();
            var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);

            var prizes = eventsBLL.CheckDrawPrize(ref eventId, custInfo.SiteId, custInfo.CustId, custInfo.CurrencyId, out int prizeId, out int result);
            if (result == 100)
            {
                apiResult.Data.PrizeId = prizeId;
                apiResult.Data.PrizeName = prizes.Where(x => x.PrizeId == prizeId).FirstOrDefault()?.PrizeName;
            }
            apiResult.ErrorCode = result;
            //apiResult.ErrorCode = eventsBLL.CheckDrawPrize(custInfo.SiteId, custInfo.CurrencyId, authData.CustId, out prizeId, out giftName);
            return apiResult;
        }

        [Authorize]
        [HttpPost]
        public ApiResult<CustGift> DrawPrize(int eventId)
        {
            var apiResult = new ApiResult<CustGift>();
            apiResult.Data = new CustGift();
            var authData = authorizeBLL.GetAuthData();
            var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);

            if (custInfo.Status != (int)CustStatus.Running || custInfo.ActStatus != (int)CustActStatus.Depoisted)
            {
                apiResult.ErrorCode = 102;
                apiResult.Message = selLangBLL.GetMsg("msg_IneligibleAngPow");
            }
            else if ((custInfo.SiteId == 2 || custInfo.SiteId == 3) && custInfoBLL.GetCustTransSummary(authData.CustId).DepositSum < 200000)
            {
                apiResult.ErrorCode = 102;
                apiResult.Message = selLangBLL.GetMsg("msg_IneligibleAngPow");
            }
            else
            {
                var ress = eventsBLL.DrawPrize(ref eventId, custInfo.CustId, custInfo.CurrencyId, custInfo.UserName, custInfo.SiteId, out int result, out Prize prize);
                if (result == 100 || result == 0)
                {
                    apiResult.Data.PrizeId = prize.PrizeId;
                    apiResult.Data.PrizeName = prize.PrizeName;
                    apiResult.Data.Jackpot = prize.Jackpot;
                }

                if(result == 0)
                {
                    string Tital = selLangBLL.GetMsg("msg_DrawPrizeTital");
                    var dAmount = prize.Amount.ToString("#,##0.##");
                    if (custInfoBLL.IsConvertRequired(custInfo.CurrencyId))
                    {
                        dAmount = $"{prize.Amount / 1000:#,##0.##}";
                    }

                    string msg = string.Format(selLangBLL.GetMsg("msg_DrawPrize"), dAmount);
                    messageBLL.SendMessage(custInfo.SiteId, "DrawPrize", Tital, msg, custInfo.CustId, custInfo.UserName, custInfo.UserLang);
                }
                apiResult.ErrorCode = result;
            }

            return apiResult;
        }
        [Authorize]
        [HttpPost]
        public ApiResult<IEnumerable<CustGiftList>> GetCustDrawPrizeList(int eventId)
        {
            var apiResult = new ApiResult<IEnumerable<CustGiftList>>();
            var authData = authorizeBLL.GetAuthData();
            var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);
            apiResult.Data = eventsBLL.GetCustDrawPrizeList(eventId, custInfo.SiteId, authData.CustId);
            apiResult.ErrorCode = 0;
            return apiResult;
        }

        [HttpPost]
        public ApiResult<LeaderboardEvent> GetLeaderboardEventCode()
        {
            var apiResult = new ApiResult<LeaderboardEvent>();
            apiResult.Data = new LeaderboardEvent();
            int siteId = 0;
            string NowEventCode = null;
            string LastEventCode = null;
           
            if (User.Identity.IsAuthenticated)
            {
                var authData = authorizeBLL.GetAuthData();
                var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);
                siteId = custInfo.SiteId;
            }
            else
            {
                var serverInfo = serverInfoBLL.GetServerInfo();
                siteId = serverInfo.SiteId;
            }
            eventsBLL.GetLeaderboardEventCode(siteId, out NowEventCode, out LastEventCode);
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
            int siteId = 0;
            int currencyId = 0;
            if (User.Identity.IsAuthenticated)
            {
                var authData = authorizeBLL.GetAuthData();
                var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);
                siteId = custInfo.SiteId;
                currencyId = custInfo.CurrencyId;
            }
            else
            {
                var serverInfo = serverInfoBLL.GetServerInfo();
                siteId = serverInfo.SiteId;
                currencyId = selLangBLL.GetPreferCurrencyId();
            }

            apiResult.Data = eventsBLL.GetLeaderboardInfoWithCache(leaderboardInfoReq.EventCode , siteId, currencyId);
            apiResult.ErrorCode = 0;
            return apiResult;
        }

    }
}
