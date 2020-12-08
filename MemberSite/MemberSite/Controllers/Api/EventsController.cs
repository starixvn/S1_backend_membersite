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
        [HttpPost]
        public ApiResult<IEnumerable<Leaderboard>> GetLeaderboardList(GetLeaderboardListReq getLeaderboardListReq)
        {
            var apiResult = new ApiResult<IEnumerable<Leaderboard>>();
            int siteId = 0;
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
            apiResult.Data = eventsBLL.GetLeaderboardListWithCache(getLeaderboardListReq.LBCategoryId, siteId, getLeaderboardListReq.TopNum, getLeaderboardListReq.BeforeDays);
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
            int custRank = 0;

            if (!eventsBLL.GetCustRankWithCache(getCustRankReq.LBCategoryId, authData.CustId, getCustRankReq.BeforeDays, out custRank))
            {
                apiResult.ErrorCode = 99;
                return apiResult;
            }
            apiResult.Data.CustRank = custRank;
            apiResult.ErrorCode = 0;
            return apiResult;
        }
    }
}
