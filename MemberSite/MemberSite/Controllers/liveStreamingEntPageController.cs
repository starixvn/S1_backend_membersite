using System;
using System.Threading.Tasks;
using MemberSite.Service.BLL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace MemberSite.Controllers
{
    public class liveStreamingEntPageController : Controller
    {
        private readonly IConfiguration configuration;
        private readonly Tools tools;
        private readonly CourierAdapter courierAdapter;
        private readonly AuthorizeBLL authorizeBLL;
        private readonly ServerInfoBLL serverInfoBLL;

        public liveStreamingEntPageController(IConfiguration configuration, Tools tools, CourierAdapter courierAdapter, AuthorizeBLL authorizeBLL, ServerInfoBLL serverInfoBLL)
        {
            this.courierAdapter = courierAdapter;
            this.tools = tools;
            this.authorizeBLL = authorizeBLL;
            this.serverInfoBLL = serverInfoBLL;
            this.configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> IndexAsync(int ch)
        {
            var serverInfo = serverInfoBLL.GetServerInfo();
            var commonServiceUrl = configuration.GetValue<string>("CommonService");
            var apiUrl = $"{commonServiceUrl}LiveStreaming/GetLiveStreamingLink";
            var ip = tools.GetClientIP();
            var liveStreamingLinkReq = new LiveStreamingLinkReq
            {
                Ch = ch,
                Ip = ip,
                SiteName = serverInfo.SiteName
            };
            if (User.Identity.IsAuthenticated)
            {
                var authData = authorizeBLL.GetAuthData();
                var uid = $"{serverInfo.SiteName}_{authData.CustId}";
                liveStreamingLinkReq.Uid = uid;
            }
            else
            {
                liveStreamingLinkReq.Uid = "NoLogin";
            }
            try
            {
                var data = await courierAdapter.MyPostAsync<bool, dynamic>(apiUrl, liveStreamingLinkReq);

                if (data.ErrorCode == 0)
                {
                    var entryUrl = data.Message;
                    return Redirect(entryUrl);
                }
                else
                {
                    var model = new ErrorData() { Message = data.Message };
                    return Content(model.Message);
                }
            }
            catch (Exception ex)
            {
                var model = new ErrorData() { Message = ex.Message };
                return Content(model.Message);
            }
        }


        public class ErrorData
        {
            public string Message { get; set; }
        }
    }
}