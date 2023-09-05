using MemberSite.Service.BLL;
using MemberSite.Service.Model;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MemberSite.Controllers.Api
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class WorldCupPredictionController : ControllerBase
    {
        private readonly WorldCupPredictionBLL _worldCupPredictionBll;
        private readonly ServerInfoBll _serverInfoBll;
        private readonly AuthorizeBLL _authorizeBll;
        private readonly CustInfoBll _custInfoBll;

        public WorldCupPredictionController(IServiceProvider provider)
        {
            _worldCupPredictionBll = provider.GetRequiredService<WorldCupPredictionBLL>();
            _serverInfoBll = provider.GetRequiredService<ServerInfoBll>();
            _authorizeBll = provider.GetRequiredService<AuthorizeBLL>();
            _custInfoBll = provider.GetRequiredService<CustInfoBll>();
        }

        [HttpPost]
        public ApiResult<WorldCupPredictionSettingResponse> GetWorldCupPredictionSetting(WorldCupPredictionSettingRequest request)
        {
            var apiResult = new ApiResult<WorldCupPredictionSettingResponse>();
            try
            {
                var responseData = _worldCupPredictionBll.GetWorldCupPredictionSettingResponse(request.LangId);
                apiResult.Data = responseData;
                apiResult.ErrorCode = 0;
            }
            catch (Exception ex)
            {
                apiResult.Message = ex.Message;
            }

            return apiResult;
        }

        [HttpPost]
        [Authorize]
        public ApiResult<MemberDepositTicketInfoResponse> GetMemberDepositTicketInfo()
        {
            var apiResult = new ApiResult<MemberDepositTicketInfoResponse>();

            try
            {
                var authData = _authorizeBll.GetAuthData();
                var serverInfo = _serverInfoBll.GetServerInfo();
                var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);

                var responseData = _worldCupPredictionBll.GetMemberDepositTicketInfo(serverInfo.SiteId, authData.CustId,
                    authData.UserName, authData.LangId, custInfo.CurrencyId);
                apiResult.Data = responseData;
                apiResult.ErrorCode = 0;
            }
            catch (Exception ex)
            {
                apiResult.Message = ex.Message;
            }

            return apiResult;
        }

        [HttpPost]
        [Authorize]
        public async Task<ApiResult> UserPollTickets(UserPollTicketRequest request)
        {
            var apiResult = new ApiResult();
            try
            {
                var authData = _authorizeBll.GetAuthData();
                var serverInfo = _serverInfoBll.GetServerInfo();
                var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
                await _worldCupPredictionBll.UserPollTicket(authData.CustId, serverInfo.SiteId, custInfo.CurrencyId, request);
                apiResult.ErrorCode = 0;
            }
            catch (Exception ex)
            {
                apiResult.Message = ex.Message;
            }

            return apiResult;
        }
    }
}
