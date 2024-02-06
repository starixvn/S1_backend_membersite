using MemberSite.Service.BLL.Interface;
using Newtonsoft.Json;
using MemberSite.Service.Model;
using MongoDB.Bson.IO;

namespace MemberSite.Service.BLL
{
    public class CheckIpBLL : ICheckIpBLL
    {
        private readonly ILogger<CheckIpBLL> _logger;
        private readonly IGeoIpApi _geoIpApi;
        private readonly IEnumerable<string> _blockCountries;
        private readonly IEnumerable<string> _officeIps;
        private readonly IEnumerable<string> _blockIps;

        public CheckIpBLL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<CheckIpBLL>>();
            _geoIpApi = provider.GetRequiredService<IGeoIpApi>();
            var configBll = provider.GetRequiredService<ConfigBLL>();
            var ipBll = provider.GetRequiredService<IPBLL>();
            _blockCountries = configBll.GetConfigValue<IEnumerable<string>>("BlockCountry");
            _officeIps = configBll.GetConfigValueByEnvironment<IEnumerable<string>>("OfficeIP");
            _blockIps = ipBll.GetBlockIp<BlockIPForUpload>(false).Select(i => i.IP);
        }

        public async Task<IpCountryWithAccess> CheckIp(string clientIp, bool isReturnClientIp = false)
        {
            var country = await _geoIpApi.CheckIp(clientIp);
            isReturnClientIp = true; //for debug
            _logger.LogInformation($"GeoCheckIp={Newtonsoft.Json.JsonConvert.SerializeObject(country)}.");
            //判斷的規則順序：
            //1.是否為白名單IP=>是則 return true, 不是則繼續下一個判斷
            if (_officeIps.Any(x => x == clientIp))
            {
                return new IpCountryWithAccess(true, country, isReturnClientIp, clientIp);
            }
            //2.是否為黑名單國家IP=>是則 return false, 不是則繼續下一個判斷
            if (_blockCountries.Contains(country.Code))
            {
                return new IpCountryWithAccess(false, country, isReturnClientIp, clientIp);
            }
            _logger.LogInformation($"BlockIpList={Newtonsoft.Json.JsonConvert.SerializeObject(_blockIps)}.");
            //3.是否為黑名單IP=>是則 return false, 不是則return true
            if (_blockIps.Contains(clientIp))
            {
                return new IpCountryWithAccess(false, country, isReturnClientIp, clientIp);
            }

            return new IpCountryWithAccess(true, country, isReturnClientIp, clientIp);
        }
    }
}
