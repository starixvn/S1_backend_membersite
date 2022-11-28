using MemberSite.Service.BLL.Interface;
using MemberSite.Service.Model;

namespace MemberSite.Service.BLL
{
    public class GeoIpBLL
    {
        private readonly IGeoIpApi _geoIpApi;
        public GeoIpBLL(IServiceProvider provider)
        {
            _geoIpApi = provider.GetRequiredService<IGeoIpApi>();
        }

        public async Task<ApiResult<IpCountryModel>> CheckIp(string clientIp)
        {
            var ipCountry = await _geoIpApi.CheckIp(clientIp);
            if (ipCountry is {Message: "Succeed"})
            {
                return new ApiResult<IpCountryModel>()
                {
                    ErrorCode = 0,
                    Data = new IpCountryModel()
                    {
                        Code = ipCountry.Code,
                        Forbidden = ipCountry.Forbidden,
                        Message = ipCountry.Message,
                        RemoteIpAddress = clientIp
                    }
                };
            }

            return new ApiResult<IpCountryModel>()
            {
                Message = ipCountry.Message,
            };
        }
    }
}