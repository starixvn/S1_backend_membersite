using MemberSite.Service.BLL;
using MemberSite.Service.BLL.Interface;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MemberSite.Controllers.Api
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class OlympicEventController : ControllerBase
    {
        private readonly IOlympicEventBLL _olympicEventBll;
        private readonly CupEventBLL _cupEventBll;
        private readonly ServerInfoBll _serverInfoBll;
        private readonly AuthorizeBLL _authorizeBll;
        private readonly CustInfoBll _custInfoBll;
        private readonly SelLangBLL _selLangBll;

        private readonly ILogger<OlympicEventController> _logger;
        private readonly ICacheHelper _redisHelper;
        
        public OlympicEventController(IServiceProvider provider)
        {
            _olympicEventBll = provider.GetRequiredService<IOlympicEventBLL>();
            _cupEventBll = provider.GetRequiredService<CupEventBLL>();
            _serverInfoBll = provider.GetRequiredService<ServerInfoBll>();
            _authorizeBll = provider.GetRequiredService<AuthorizeBLL>();
            _custInfoBll = provider.GetRequiredService<CustInfoBll>();
            _selLangBll = provider.GetRequiredService<SelLangBLL>();
            _logger = provider.GetRequiredService<ILogger<OlympicEventController>>();
            _redisHelper = provider.GetRequiredService<ICacheHelper>();
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
        [Authorize]
        public ApiResult<OlympicMemberDepositTicketInfoResponse> GetOlympicEventTicket()
        {
            var apiResult = new ApiResult<OlympicMemberDepositTicketInfoResponse>();

            try
            {
                 var authData = _authorizeBll.GetAuthData();
                 var serverInfo = _serverInfoBll.GetServerInfo();
                 var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
                
                 var responseData = _olympicEventBll.GetOlympicEventTicket(serverInfo.SiteId, authData.CustId, authData.UserName, authData.LangId, custInfo.CurrencyId).Result;
                
                 apiResult.Data = responseData;
                 apiResult.ErrorCode = 0;
                 apiResult.Message = responseData.ErrorCode switch
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
        public async Task<ApiResult<ClaimOlympicEventPrizeResponse>> ClaimOlympicEventPrize(ClaimOlympicEventPrizeRequest request)
        {
            var apiResult = new ApiResult<ClaimOlympicEventPrizeResponse>();

            try
            {
                var authData = _authorizeBll.GetAuthData();
                if (authData is { CustId: > 0 })
                {
                    
                    var lockKey = $"OlympicEventClaimLock:{authData.CustId}";
                    var lockValue = Guid.NewGuid().ToString(); // 每次取鎖使用唯一值，確保為同一個執行序所執行
                    var lockExpiry = TimeSpan.FromSeconds(5); //lock 5 sec        

                    if (await _redisHelper.TryCreateLockAsync(lockKey, lockValue, lockExpiry))
                    {
                        var serverInfo = _serverInfoBll.GetServerInfo();
                        var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);

                        var responseData = _olympicEventBll.ClaimOlympicEventPrize(serverInfo.SiteId, authData.CustId,
                            authData.UserName, custInfo.CurrencyId, request.UsedGoldMetalCount).Result;

                        apiResult.ErrorCode = responseData.ErrorCode;
                        apiResult.Message = responseData.Message;
                        
                        await _redisHelper.UnLockAsync(lockKey, lockValue);
                    }
                    //var key = $"SetClaimPromotion:{authData.CustId}";
                    // if (!await _redisHelper.SetNxAsync(key, string.Empty, TimeSpan.FromSeconds(5)))
                    // {
                    //     apiResult.ErrorCode = -100;
                    //     apiResult.Message = _selLangBll.GetMsg("msg_Ineligible");
                    //
                    //     _logger.LogInformation("Block duplicate claim bonus");
                    //
                    //     return apiResult;
                    // }
                    // if (custInfo.SiteId is 2 or 3) // bwing and goal
                    // {
                    //     _ = Task.Run(() =>
                    //     {
                    //         _ = _shieldBll.CheckCustShieldRisk(
                    //             custInfo,
                    //             ShieldRiskTriggeredPoint.ClaimBonus,
                    //             ShieldRiskTaggedIssue.HighRisk);
                    //     });
                    // }
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