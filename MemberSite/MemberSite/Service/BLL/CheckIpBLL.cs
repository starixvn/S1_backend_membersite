using MemberSite.Service.BLL.Interface;
using Newtonsoft.Json;
using MemberSite.Service.Model;

namespace MemberSite.Service.BLL
{
    public class CheckIpBLL : ICheckIpBLL
    {
        private readonly ILogger<CheckIpBLL> _logger;
        private readonly IGeoIpApi _geoIpApi;

        public CheckIpBLL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<CheckIpBLL>>();
            _geoIpApi = provider.GetRequiredService<IGeoIpApi>();
            var configBll = provider.GetRequiredService<ConfigBLL>();
        }

        public async Task<IpCountryWithAccess> CheckIp(string clientIp, bool isReturnClientIp = false)
        {
            var country = await _geoIpApi.CheckIp(clientIp);
            _logger.LogInformation($"GeoCheckIp={JsonConvert.SerializeObject(country)}.");

            return new IpCountryWithAccess(country, clientIp);
        }
    }
}
