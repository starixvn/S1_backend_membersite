using MemberSite.Exceptions;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.BLL
{
    public class CheckIpBLL
    {
        private readonly Tools _tools;
        private readonly ILogger<CheckIpBLL> _logger;
        private readonly Dictionary<string, string> _forbiddenCountries;
        private readonly IEnumerable<IPCountry> _IPCountryList;
        private readonly ICacheHelper _cacheHelper;

        public CheckIpBLL(IServiceProvider provider)
        {
            _tools = provider.GetRequiredService<Tools>();
            _logger = provider.GetRequiredService<ILogger<CheckIpBLL>>();
            _IPCountryList = provider.GetRequiredService<IEnumerable<IPCountry>>();
            _forbiddenCountries = provider.GetRequiredService<ConfigBLL>()
                .GetConfigValue<List<string>>("BlockCountry").ToDictionary(i => i, i => i);
            _cacheHelper = provider.GetRequiredService<ICacheHelper>();
        }

        public async Task<IpCountryWithAccess> CheckIp(string clientIp)
        {
            try
            {
                var uip = _tools.GetIpToLong(clientIp);
                var ipCountry = _IPCountryList.FirstOrDefault(x => x.IpFrom < uip && x.IpTo > uip);

                // ip異常找不到國家回傳block
                if (ipCountry == null)
                {
                    return new IpCountryWithAccess(clientIp, "not found in IPCountryList");
                }

                // 檢查黑名單
                if (await CheckBlockIp(clientIp))
                {
                    return new IpCountryWithAccess(clientIp, "in block ip list.")
                    {
                        Code = ipCountry.Code,
                        Forbidden = true //block
                    };
                }

                // 檢查白名單
                if (await _cacheHelper.KeyExistsAsync($"AllowedIp:{clientIp}", RedisDataBase.IP))
                {
                    return new IpCountryWithAccess(clientIp, "in allow ip list.")
                    {
                        Code = ipCountry.Code,
                        Forbidden = false //access
                    };
                }

                // 檢查黑名單國家
                if (string.IsNullOrWhiteSpace(ipCountry.Code) || _forbiddenCountries.TryGetValue(ipCountry.Code.Trim(), out _))
                {
                    return new IpCountryWithAccess(clientIp, $"CountryCode: [{ipCountry.Code}] is in block country list or can't recognize country code.")
                    {
                        Code = ipCountry.Code,
                        Forbidden = true //block
                    };
                }

                return new IpCountryWithAccess(clientIp, "Succeed")
                {
                    Code = ipCountry.Code,
                    Forbidden = false
                };
            }
            catch (IPFormatException e)
            {
                _logger.LogError(e, e.Message);
                return new IpCountryWithAccess(clientIp, e.Message);
            }
        }

        public async Task<bool> CheckBlockIp(string clientIp)
        {
            return await _cacheHelper.KeyExistsAsync($"BlockIp:{clientIp}", RedisDataBase.IP);
        }
    }
}
