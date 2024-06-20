using MemberSite.Extensions;
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
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly ICacheHelper _redisHelper;
        private readonly IWebHostEnvironment _environment;
        
        public OlympicEventController(IServiceProvider provider)
        {
            _olympicEventBll = provider.GetRequiredService<IOlympicEventBLL>();
            _cupEventBll = provider.GetRequiredService<CupEventBLL>();
            _serverInfoBll = provider.GetRequiredService<ServerInfoBll>();
            _authorizeBll = provider.GetRequiredService<AuthorizeBLL>();
            _custInfoBll = provider.GetRequiredService<CustInfoBll>();
            _selLangBll = provider.GetRequiredService<SelLangBLL>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
            _redisHelper = provider.GetRequiredService<ICacheHelper>();
            _environment = provider.GetRequiredService<IWebHostEnvironment>();
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
        public ApiResult<ClaimOlympicEventPrizeResponse> ClaimOlympicEventPrize(ClaimOlympicEventPrizeRequest request)
        {
            var apiResult = new ApiResult<ClaimOlympicEventPrizeResponse>();

            try
            {
                var authData = _authorizeBll.GetAuthData();
                var serverInfo = _serverInfoBll.GetServerInfo();
                var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
                
                var responseData = _olympicEventBll.ClaimOlympicEventPrize(serverInfo.SiteId, authData.CustId, authData.UserName,custInfo.CurrencyId,request.UsedGoldMetalCount).Result;
                
                apiResult.ErrorCode = responseData.ErrorCode;
                apiResult.Message = responseData.Message;
            }
            catch (Exception ex)
            {
                apiResult.Message = ex.Message;
            }

            return apiResult;
        }

    }
}