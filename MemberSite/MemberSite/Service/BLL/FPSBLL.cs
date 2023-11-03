using MemberSite.Service.Library;
using MemberSite.Service.Model;

namespace MemberSite.Service.BLL
{
    public class FPSBLL
    {
        private readonly CourierAdapter courierAdapter;
        private readonly string _commonServiceUrl;

        public FPSBLL(CourierAdapter courierAdapter, ConfigBLL configBll)
        {
            this.courierAdapter = courierAdapter;
            this._commonServiceUrl = configBll.GetConfigValueByEnvironment<ServiceHostConfig>("ServiceHost").CommonService;
        }
        public async Task<ApiResult> FPSChecksAsync(int custId, string action, string actionResult, string url, string platform, string userAgent, string screen, string di, string ip)
        {
            var apiUrl = $"{_commonServiceUrl}FPS/FPSChecks";

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

            ApiResult apiResult = await courierAdapter.MyPostAsync<bool, dynamic>(apiUrl, req);

            return apiResult;

        }
    }
}
