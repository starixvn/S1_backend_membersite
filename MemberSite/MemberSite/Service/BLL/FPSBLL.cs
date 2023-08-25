using MemberSite.Service.Library;
using MemberSite.Service.Model;

namespace MemberSite.Service.BLL
{
    public class FPSBLL
    {
        private readonly CourierAdapter courierAdapter;
        private readonly IConfiguration configuration;

        public FPSBLL(IConfiguration configuration, CourierAdapter courierAdapter)
        {
            this.courierAdapter = courierAdapter;
            this.configuration = configuration;
        }
        public async Task<ApiResult> FPSChecksAsync(int custId, string action, string actionResult, string url, string platform, string userAgent, string screen, string di, string ip)
        {
            var apiResult = new ApiResult();


            var commonServiceUrl = configuration.GetValue<string>("CommonService");
            var apiUrl = $"{commonServiceUrl}FPS/FPSChecks";

            dynamic req = new
            {
                account = custId,
                action,
                actionResult,
                ip,
                url,
                platform,
                userAgent,
                screen,
                di
            };

            apiResult = await courierAdapter.MyPostAsync<bool, dynamic>(apiUrl, req);

            return apiResult;

        }
    }
}
