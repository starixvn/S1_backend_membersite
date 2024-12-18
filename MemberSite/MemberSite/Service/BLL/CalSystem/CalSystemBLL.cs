using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using Newtonsoft.Json;

namespace MemberSite.Service.BLL.CalSystem
{
    // ReSharper disable once InconsistentNaming
    public class CalSystemBLL : ICalSystemBLL
    {
        private readonly ILogger<CalSystemBLL> _logger;
        private readonly CalSystemConfig _calSystemConfig;
        private readonly CourierAdapter _courierAdapter;
        private static readonly string SuccessValue = "1";

        public CalSystemBLL(IServiceProvider provider)
        {
            var configBll = provider.GetRequiredService<ConfigBLL>();
            _calSystemConfig = configBll.GetConfigValue<CalSystemConfig>("CalSystemConfig", 101);
            _courierAdapter = provider.GetRequiredService<CourierAdapter>();
            _logger = provider.GetRequiredService<ILogger<CalSystemBLL>>();
        }

        public async Task CreateActivity(CreateActivityRequest request)
        {
            var url = $"{_calSystemConfig.Url}/api/CreateActivity";
            var result = await _courierAdapter.PostAsync<BaseApiResponse<RegisterPromotionData>>(url, null, request);
            if (!result.Success.Equals(SuccessValue))
            {
                _logger.LogError($"CalSystem Api CreateActivity response error. postData: {JsonConvert.SerializeObject(request)}, response: {JsonConvert.SerializeObject(result)}");
            }
        }
    }
}