using MemberSite.Service.BLL;
using MemberSite.Service.DAL.Interface;
using MemberSite.Service.Library;
using MemberSite.Service.Model;

namespace MemberSite.Service.DAL
{
    public class VerificationRewardService : IVerificationRewardService
    {
        private readonly CourierAdapter _courierAdapter;
        private readonly string _bonusServiceUrl;
        public VerificationRewardService(IServiceProvider provider)
        {
            _courierAdapter = provider.GetService<CourierAdapter>();
            var configBll = provider.GetRequiredService<ConfigBLL>();
            _bonusServiceUrl = configBll.GetServiceHostsConfigService<ServiceHostConfig>("serviceHosts").BonusService;
        }

        public async Task<ApiResult> SetVerificationReward(int custId, int verificationType)
        {
            var apiUrl = $"{_bonusServiceUrl}SetVerificationReward";

            var req = new VerificationRewardObj
            {
                CustId = custId,
                OTPType = verificationType
            };

            var result = await _courierAdapter.MyPostAsync<ApiResult, VerificationRewardObj>(apiUrl, req);

            return result;
        }

        private class VerificationRewardObj
        {
            public int CustId { get; set; }
            public int OTPType { get; set; }
        }
    }
}