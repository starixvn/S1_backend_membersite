using MemberSite.Service.BLL;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

using Microsoft.AspNetCore.Mvc;

namespace MemberSite.Controllers.Api
{
    [Route("api/[controller]")]
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class PlayerController : Controller
    {
        private readonly ServerInfoBll _serverInfoBLL;
        private readonly AppLinkBLL _appLinkBLL;
        private readonly DeepLinkBLL _deepLinkBLL;
        private readonly ILogger<PlayerController> _logger;
        private readonly AuthorizeBLL _authorizeBll;
        private readonly CustNotificationBLL _custNotificationBLL;

        public PlayerController(IServiceProvider provider)
        {
            _serverInfoBLL = provider.GetRequiredService<ServerInfoBll>();
            _appLinkBLL = provider.GetRequiredService<AppLinkBLL>();
            _deepLinkBLL = provider.GetRequiredService<DeepLinkBLL>();
            _logger = provider.GetRequiredService<ILogger<PlayerController>>();
            _authorizeBll = provider.GetRequiredService<AuthorizeBLL>();
            _custNotificationBLL = provider.GetRequiredService<CustNotificationBLL>();
        }

        [HttpPost]
        public ApiResult<List<GetAppLinkRep>> GetAppLink()
        {
            var apiResult = new ApiResult<List<GetAppLinkRep>>();
            var serverInfo = _serverInfoBLL.GetServerInfo();
            var result = _appLinkBLL.GetAppLink(serverInfo.SiteId);
            apiResult.Data = result;
            apiResult.ErrorCode = 0;
            return apiResult;
        }

        public class GetDeepLinkReq
        {
            public int SiteId { get; set; }
            public int DeepLinkId { get; set; }
        }

        [HttpPost]
        public ApiResult<List<GetDeepLinkRep>> GetDeepLink(GetDeepLinkReq req)
        {
            var apiResult = new ApiResult<List<GetDeepLinkRep>>();
            var result = _deepLinkBLL.GetDeepLink(req.SiteId, req.DeepLinkId);
            apiResult.Data = result;
            apiResult.ErrorCode = 0;
            return apiResult;
        }

        public class CustNotificationReq
        {
            public CustNotificationType NotificationType { get; set; }

            public string TransId { get; set; }
        }

        [HttpPost]
        public async Task<ApiResult<IEnumerable<string>>> GetCustNotification(CustNotificationReq req)
        {
            var apiResult = new ApiResult<IEnumerable<string>>
            {
                Data = new List<string>(),
                ErrorCode = (int)ErrorCode.Success
            };

            try
            {

                var authData = _authorizeBll.GetAuthData();
                if (authData is { CustId: > 0 })
                {
                    apiResult.Data = await _custNotificationBLL.GetCustNotification((int)req.NotificationType, authData.CustId);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }

            return apiResult;
        }

        [HttpPost]
        public async Task<ApiResult<int>>SetCustNotification(CustNotificationReq req)
        {
            var apiResult = new ApiResult<int>();
            try
            {
                var authData = _authorizeBll.GetAuthData();
                if (authData is { CustId: > 0 })
                {
                    var result = await _custNotificationBLL.SetCustNotification((int)req.NotificationType, authData.CustId, req.TransId);
                    apiResult.Data = result;
                    apiResult.ErrorCode = result > 0 ? (int)ErrorCode.Success : (int)ErrorCode.Fail;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }

            return apiResult;
        }
    }

}
