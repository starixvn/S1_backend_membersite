using MemberSite.Service.BLL.Interface;
using MemberSite.Service.Library;
using MemberSite.Service.Model;

namespace MemberSite.Service.BLL
{
    public class GeoIpApi : IGeoIpApi
    {
        private readonly IHttpHelper _httpHelper;
        private readonly ILogger<GeoIpApi> _logger;
        private readonly string _geoIpUrl;
        public GeoIpApi(IServiceProvider provider)
        {
            _httpHelper = provider.GetRequiredService<IHttpHelper>();
            _logger = provider.GetRequiredService<ILogger<GeoIpApi>>();
            var configBll = provider.GetRequiredService<ConfigBLL>();
            _geoIpUrl = configBll.GetConfigValueByEnvironment<ServiceHostConfig>("ServiceHost").GeoIpService;
        }

        public async Task<IpCountry> CheckIp(string clientIp)
        {
            try
            {
                var url = $"{_geoIpUrl}CheckIp/{clientIp}";
                var (result, htpStatusCode) = await _httpHelper.GetAsync<IpCountry>(url);
                return result;
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"query {clientIp} failed {e.Message}");
                return new IpCountry()
                {
                    Message = e.Message
                };
            }
        }
    }
}