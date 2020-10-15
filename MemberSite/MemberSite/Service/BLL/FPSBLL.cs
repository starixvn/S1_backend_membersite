using MemberSite.Service.Library;
using MemberSite.Service.Model;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemberSite.Service.BLL
{
    public class FPSBLL
    {
        private readonly CourierAdapter courierAdapter;
        private readonly IConfiguration configuration;
        private readonly Tools tools;

        public FPSBLL(IConfiguration configuration,CourierAdapter courierAdapter, Tools tools)
        {
            this.courierAdapter = courierAdapter;
            this.configuration = configuration;
            this.tools = tools;

        }
        public async Task<ApiResult> FPSChecksAsync(int custId, string action, string actionResult, string url, string platform, string userAgent, string screen,string di)
        {
            ApiResult apiResult = new ApiResult();
           

            string commonServiceUrl = configuration.GetValue<string>("CommonService");
            string apiUrl = $"{commonServiceUrl}Common/FPS/FPSChecks";

            dynamic req = new
            {
                account = custId,
                action = action,
                actionResult = actionResult,
                ip = tools.GetClientIP(),
                url = url,
                platform = platform,
                userAgent = userAgent,
                screen = screen,
                di = di
            };

            apiResult = await courierAdapter.MyPostAsync<bool, dynamic>(apiUrl, req);

            return apiResult;

        }
    }
}
