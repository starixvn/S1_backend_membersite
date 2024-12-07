using MemberSite.Service.BLL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

using Microsoft.AspNetCore.Mvc;

namespace MemberSite.Controllers
{
    public class liveStreamingEntPageController : Controller
    {
        private readonly Tools _tools;
        private readonly CourierAdapter _courierAdapter;
        private readonly AuthorizeBLL _authorizeBll;
        private readonly ServerInfoBll _serverInfoBll;
        private readonly CustInfoBll _custInfoBll;
        private readonly string _commonServiceUrl;

        public liveStreamingEntPageController(IServiceProvider provider)
        {
            _courierAdapter = provider.GetRequiredService<CourierAdapter>();
            _tools = provider.GetRequiredService<Tools>();
            _authorizeBll = provider.GetRequiredService<AuthorizeBLL>();
            _serverInfoBll = provider.GetRequiredService<ServerInfoBll>();
            _custInfoBll = provider.GetRequiredService<CustInfoBll>();
            var configBll = provider.GetRequiredService<ConfigBLL>();
            _commonServiceUrl = configBll.GetServiceHostsConfigService<ServiceHostConfig>("serviceHosts").CommonService;
        }

        [HttpGet]
        public async Task<IActionResult> IndexAsync(int ch)
        {
            var serverInfo = _serverInfoBll.GetServerInfo();
            var apiUrl = $"{_commonServiceUrl}LiveStreaming/GetLiveStreamingLink";
            var ip = _tools.GetClientIP();
            var liveStreamingLinkReq = new LiveStreamingLinkReq
            {
                Ch = ch,
                Ip = ip,
                SiteName = serverInfo.SiteName
            };

            var authData = _authorizeBll.GetAuthData();
            switch (authData is { CustId: > 0 })
            {
                case false:
                    return Content("Please Log In");

                case true:
                    {
                        var uid = $"{serverInfo.SiteName}_{authData.CustId}";
                        liveStreamingLinkReq.Uid = uid;
                        var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
                        if (custInfo.ActStatus == (int)ActStatus.NonDeposit)
                        {
                            return Content("Please Deposit");
                        }

                        break;
                    }
            }

            try
            {
                var data = await _courierAdapter.MyPostAsync<bool, dynamic>(apiUrl, liveStreamingLinkReq);

                if (data.ErrorCode == 0)
                {
                    var entryUrl = data.Message;
                    return Redirect(entryUrl);
                }

                var model = new ErrorData { Message = data.Message };
                return Content(model.Message);
            }
            catch (Exception ex)
            {
                var model = new ErrorData { Message = ex.Message };
                return Content(model.Message);
            }
        }


        public class ErrorData
        {
            public string Message { get; set; }
        }
    }
}