using MemberSite.Service.BLL;
using MemberSite.Service.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemberSite.Controllers.Api
{
    //[AllowAnonymous]
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly AuthorizeBLL authorizeBLL;
        private readonly EventsBLL eventsBLL;
        private readonly ServerInfoBLL serverInfoBLL;
        private readonly CustInfoBLL custInfoBLL;

        public EventsController(AuthorizeBLL authorizeBLL, EventsBLL eventsBLL, ServerInfoBLL serverInfoBLL, CustInfoBLL custInfoBLL)
        {
            this.authorizeBLL = authorizeBLL;
            this.eventsBLL = eventsBLL;
            this.serverInfoBLL = serverInfoBLL;
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

            apiResult.Data = eventsBLL.GetLeaderboardListWithCache(getLeaderboardListReq.LBCategoryId, custInfo.SiteId, custInfo.CurrencyId, getLeaderboardListReq.TopNum, getLeaderboardListReq.BeforeDays);
            apiResult.ErrorCode = 0;
            return apiResult;
        }
        
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

            if (!eventsBLL.GetCustRankWithCache(getCustRankReq.LBCategoryId, authData.CustId, getCustRankReq.BeforeDays, out int custRank, out int point))
            {
                apiResult.ErrorCode = 99;
                return apiResult;
            }

            apiResult.Data.CustRank = custRank;
            apiResult.Data.point = point;
            apiResult.ErrorCode = 0;
            return apiResult;
        }
    }
}
