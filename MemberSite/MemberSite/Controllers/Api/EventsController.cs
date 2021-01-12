using MemberSite.Service.BLL;
using MemberSite.Service.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
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
        public EventsController(AuthorizeBLL authorizeBLL, EventsBLL eventsBLL, CustInfoBLL custInfoBLL)
        {
            this.authorizeBLL = authorizeBLL;
            this.eventsBLL = eventsBLL;
            this.custInfoBLL = custInfoBLL;
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
    }
}
