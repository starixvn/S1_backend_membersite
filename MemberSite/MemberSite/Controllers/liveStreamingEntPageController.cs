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
        private readonly CourierAdapter courierAdapter;
        private readonly AuthorizeBLL authorizeBLL;
        private readonly ServerInfoBLL serverInfoBLL;

        public liveStreamingEntPageController(IConfiguration configuration, CourierAdapter courierAdapter, AuthorizeBLL authorizeBLL, ServerInfoBLL serverInfoBLL)
        {
            this.courierAdapter = courierAdapter;
            this.authorizeBLL = authorizeBLL;
            this.serverInfoBLL = serverInfoBLL;
            this.configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> IndexAsync(int ch)
        {
            var serverInfo = serverInfoBLL.GetServerInfo();
            string commonServiceUrl = configuration.GetValue<string>("CommonService");
            string apiUrl = $"{commonServiceUrl}LiveStreaming/GetLiveStreamingLink";
            var liveStreamingLinkReq = new LiveStreamingLinkReq
            {
                Ch = ch,
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
                    string entryUrl = data.Message;
                    return Redirect(entryUrl);
                }
                else
                {
                    ErrorData model = new ErrorData() { Message = data.Message };
                    return Content(model.Message);
                }
            }
            catch (Exception ex)
            {
                ErrorData model = new ErrorData() { Message = ex.Message };
                return Content(model.Message);
            }
        }


        public class ErrorData
        {
            public string Message { get; set; }
        }
    }
}