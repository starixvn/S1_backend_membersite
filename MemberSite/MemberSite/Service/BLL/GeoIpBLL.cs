using System;
using System.Threading.Tasks;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MemberSite.Service.BLL
{
    public class GeoIpBLL
    {
        private readonly CourierAdapter _courierAdapter;
        private readonly string _geoIpUrl;
        public GeoIpBLL(IServiceProvider provider, IConfiguration configuration)
        {
            _courierAdapter = provider.GetRequiredService<CourierAdapter>();
            _geoIpUrl = configuration.GetValue<string>("GeoIpService");
        }

        public async Task<ApiResult<IpCountryModel>> CheckIp(string clientIp)
        {
            var url = $"{_geoIpUrl}CheckIp/{clientIp}";
            var result = await _courierAdapter.GetAsync<IpCountryModel>(url);
            result.Data.RemoteIpAddress = clientIp;
            return result;
        }
    }
}