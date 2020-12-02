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
    public class EvnetsController
    {
        private readonly AuthorizeBLL authorizeBLL;
        private readonly EvnetsBLL evnetsBLL;
        private readonly ServerInfoBLL serverInfoBLL;

        public EvnetsController(AuthorizeBLL authorizeBLL, EvnetsBLL evnetsBLL, ServerInfoBLL serverInfoBLL)
        {
            this.authorizeBLL = authorizeBLL;
            this.evnetsBLL = evnetsBLL;
            this.serverInfoBLL = serverInfoBLL;
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
            var serverInfo = serverInfoBLL.GetServerInfo();
            apiResult.Data = evnetsBLL.GetLeaderboardListWithCache(getLeaderboardListReq.LBCategoryId, serverInfo.SiteId, getLeaderboardListReq.TopNum, getLeaderboardListReq.BeforeDays);
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

            if (!evnetsBLL.GetCustRankWithCache(getCustRankReq.LBCategoryId, authData.CustId, getCustRankReq.BeforeDays, out custRank))
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
