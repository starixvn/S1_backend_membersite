using System;
using System.Threading.Tasks;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MemberSite.Service.BLL
{
    public class GeoIpApiBLL : IGeoIpApiBLL
    {
        private readonly IHttpHelper _httpHelper;
        private readonly ILogger<GeoIpApiBLL> _logger;
        private readonly string _geoIpUrl;
        public GeoIpApiBLL(IServiceProvider provider, IConfiguration configuration)
        {
            _httpHelper = provider.GetRequiredService<IHttpHelper>();
            _logger = provider.GetRequiredService<ILogger<GeoIpApiBLL>>();
            _geoIpUrl = configuration.GetValue<string>("GeoIpService");
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
                _logger.LogWarning(e, $"query {clientIp} failed {e.Message}");
                return new IpCountry()
                {
                    Message = e.Message
                };
            }
        }
    }
}