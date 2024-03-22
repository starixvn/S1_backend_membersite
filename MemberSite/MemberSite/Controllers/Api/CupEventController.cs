using MemberSite.Service.BLL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MemberSite.Controllers.Api
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class CupEventController : ControllerBase
    {
        private readonly CupEventBLL _cupEventBll;
        private readonly ServerInfoBll _serverInfoBll;
        private readonly AuthorizeBLL _authorizeBll;
        private readonly CustInfoBll _custInfoBll;
        private readonly SelLangBLL _selLangBll;
        private readonly ICacheHelper _redisHelper;

        public CupEventController(IServiceProvider provider)
        {
            _cupEventBll = provider.GetRequiredService<CupEventBLL>();
            _serverInfoBll = provider.GetRequiredService<ServerInfoBll>();
            _authorizeBll = provider.GetRequiredService<AuthorizeBLL>();
            _custInfoBll = provider.GetRequiredService<CustInfoBll>();
            _selLangBll = provider.GetRequiredService<SelLangBLL>();
            _redisHelper = provider.GetRequiredService<ICacheHelper>();
        }

        [HttpPost]
        public ApiResult<CupEventPredictionSettingResponse> GetCupEventPredictionSetting(CupEventPredictionSettingRequest request)
        {
            var apiResult = new ApiResult<CupEventPredictionSettingResponse>();
            try
            {
                var responseData = _cupEventBll.GetCupEventPredictionSettingResponse(request.LangId);
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
        public ApiResult<MemberDepositTicketInfoResponse> GetCupEventTicket()
        {
            var apiResult = new ApiResult<MemberDepositTicketInfoResponse>();

            try
            {
                var authData = _authorizeBll.GetAuthData();
                var serverInfo = _serverInfoBll.GetServerInfo();
                var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
                
                var responseData = _cupEventBll.GetCupEventTicket(serverInfo.SiteId, authData.CustId, authData.UserName, authData.LangId, custInfo.CurrencyId).Result;
                
                apiResult.Data = responseData;
                apiResult.ErrorCode = responseData.ErrorCode;
                apiResult.Message = apiResult.ErrorCode switch
                {
                    104 => _selLangBll.GetMsg("msg_ProfileOrPhoneNotFinish"),
                    _ => ""
                };
            }
            catch (Exception ex)
            {
                apiResult.Message = ex.Message;
            }

            return apiResult;
        }
        
        [HttpPost]
        [Authorize]
        public ApiResult<IEnumerable<MemberDepositDto>> GetMemberDeposit()
        {
            var apiResult = new ApiResult<IEnumerable<MemberDepositDto>>();

            try
            {
                var authData = _authorizeBll.GetAuthData();
                var serverInfo = _serverInfoBll.GetServerInfo();
                var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
                
                var responseData = _cupEventBll.GetMemberDeposit(serverInfo.SiteId, authData.CustId, custInfo.CurrencyId);
                
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
        public ApiResult<IEnumerable<CupEventTicketSummaryResponse>> GetCupEventTicketSummary()
        {
            var apiResult = new ApiResult<IEnumerable<CupEventTicketSummaryResponse>>();

            try
            {
                var responseData = _cupEventBll.GetCupEventTicketSummary();
                
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
        public async Task<ApiResult> UserPollTickets(List<UserPollTicketRequest> request)
        {
            var apiResult = new ApiResult();
            try
            {
                var authData = _authorizeBll.GetAuthData();
                var serverInfo = _serverInfoBll.GetServerInfo();
                var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
                
                var lockKey = $"UserPollTickets:{custInfo.CustId}";
                var lockValue = Guid.NewGuid().ToString(); // 每次取鎖使用唯一值，確保為同一個執行序所執行
                var lockExpiry = TimeSpan.FromSeconds(30); //lock 30 sec        
                
                if (await _redisHelper.TryCreateLockAsync(lockKey, lockValue, lockExpiry))
                {
                    await _cupEventBll.UserPollTicket(authData.CustId, serverInfo.SiteId, custInfo.CurrencyId, request);
                    apiResult.ErrorCode = 0;
                    await _redisHelper.UnLockAsync(lockKey, lockValue);
                }
            }
            catch (Exception ex)
            {
                apiResult.Message = ex.Message;
            }

            return apiResult;
        }
        
        [HttpPost]
        [Authorize]
        public async Task<ApiResult> MultiUserPollTickets(MultiUserPollTicketRequest request)
        {
            var apiResult = new ApiResult();
            try
            {
                var authData = _authorizeBll.GetAuthData();
                var serverInfo = _serverInfoBll.GetServerInfo();
                var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
                
                var lockKey = $"MultiUserPollTickets:{custInfo.CustId}";
                var lockValue = Guid.NewGuid().ToString(); // 每次取鎖使用唯一值，確保為同一個執行序所執行
                var lockExpiry = TimeSpan.FromSeconds(30); //lock 30 sec        
                
                if (await _redisHelper.TryCreateLockAsync(lockKey, lockValue, lockExpiry))
                {
                    await _cupEventBll.MultiUserPollTicket(authData.CustId, serverInfo.SiteId, custInfo.CurrencyId, request);
                    await _redisHelper.UnLockAsync(lockKey, lockValue);
                    apiResult.ErrorCode = 0;
                }                 
            }
            catch (Exception ex)
            {
                apiResult.Message = ex.Message;
            }

            return apiResult;
        }
    }
}
