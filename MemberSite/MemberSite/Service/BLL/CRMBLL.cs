using MemberSite.Service.Library;
using MemberSite.Service.Model;
using Org.BouncyCastle.Asn1.Crmf;

namespace MemberSite.Service.BLL
{
    public class CRMBLL
    {
        private readonly string _commonServiceUrl;
        private readonly CourierAdapter _courierAdapter;
        public CRMBLL(IServiceProvider provider)
        {
            var configBll = provider.GetRequiredService<ConfigBLL>();
            _commonServiceUrl = configBll.GetConfigValueByEnvironment<ServiceHostConfig>("serviceHosts").CommonService;
            _courierAdapter = provider.GetRequiredService<CourierAdapter>();
        }

        public async Task<object> Send(CRMRequest req)
        {
            var url = _commonServiceUrl + "CRM/CRMEventSend";
            var result = await _courierAdapter.MyPostAsync<ApiResult, CRMRequest>(url, req);

            return result;
        }
    }
}
