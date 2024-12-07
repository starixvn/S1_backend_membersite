using MemberSite.Service.Library;
using MemberSite.Service.Model;

namespace MemberSite.Service.BLL
{
    public class FPSBLL
    {
        private readonly CourierAdapter _courierAdapter;
        private readonly string _commonServiceUrl;

        public FPSBLL(CourierAdapter courierAdapter, ConfigBLL configBll)
        {
            _courierAdapter = courierAdapter;
            _commonServiceUrl = configBll.GetServiceHostsConfigService<ServiceHostConfig>("serviceHosts").CommonService;
        }
        public async Task<ApiResult> FPSChecksAsync(FPSApiRequest request)
        {
            var apiUrl = $"{_commonServiceUrl}FPS/FPSChecks";

            dynamic req = new FPSApiRequest
            {
                Account = request.Account,
                DI = request.DI,
                BI = request.BI,
                IP = request.IP,
                UserAgent = request.UserAgent,
                DetecasAnalysis = request.DetecasAnalysis,
                AcceptsTypes = request.AcceptsTypes,
                SessionKey = request.SessionKey,
                Token = request.Token,
                Url = request.Url
            };

            var apiResult = await _courierAdapter.MyPostAsync<bool, dynamic>(apiUrl, req);

            return apiResult;

        }
    }
}
