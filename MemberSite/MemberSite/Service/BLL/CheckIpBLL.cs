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
        private readonly List<IPCountry> _IPCountryList;
        private readonly ICacheHelper _cacheHelper;
        public readonly List<CountryCode> CountryList;
        private readonly IpHelper _ipHelper;

        public CheckIpBLL(IServiceProvider provider)
        {
            _tools = provider.GetRequiredService<Tools>();
            _logger = provider.GetRequiredService<ILogger<CheckIpBLL>>();
            _IPCountryList = provider.GetRequiredService<IEnumerable<IPCountry>>().ToList();
            _forbiddenCountries = provider.GetRequiredService<ConfigBLL>()
                .GetConfigValue<List<string>>("BlockCountry").ToDictionary(i => i, i => i);
            _cacheHelper = provider.GetRequiredService<ICacheHelper>();
            CountryList = _IPCountryList.Select(i => new CountryCode(i))
                .Where(i => !string.IsNullOrWhiteSpace(i.Country))
                .DistinctBy(p => p.Country)
                .OrderBy(i => i.Country).ToList();
            _ipHelper = provider.GetRequiredService<IpHelper>();
        }

        public async Task<IpCountryWithAccess> CheckIp(string clientIp)
        {
            try
            {
                var uip = _tools.GetIpToLong(clientIp);
                var ipCountry = _IPCountryList.Find(x => x.IpFrom < uip && x.IpTo > uip);

                // IP exception cannot find country, return block
                if (ipCountry == null)
                {
                    return new IpCountryWithAccess(clientIp, "not found in IPCountryList");
                }

                // 1. Check the whitelist
                if (await _cacheHelper.KeyExistsAsync($"AllowedIp:{clientIp}", RedisDataBase.IP) || _ipHelper.IpInRange(clientIp))
                {
                    return new IpCountryWithAccess(clientIp, "in allow ip list.")
                    {
                        Code = ipCountry.Code,
                        Forbidden = false //access
                    };
                }

                // 2. Check the blacklist
                if (await CheckBlockIp(clientIp))
                {
                    return new IpCountryWithAccess(clientIp, "in block ip list.")
                    {
                        Code = ipCountry.Code,
                        Forbidden = true //block
                    };
                }

                // 3. Check blacklist countries
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
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return new IpCountryWithAccess(clientIp, e.Message)
                {
                    //Do not block if other exceptions occur, avoid being inaccessible at the front desk
                    Forbidden = false
                };
            }
        }

        public async Task<bool> CheckBlockIp(string clientIp)
        {
            return await _cacheHelper.KeyExistsAsync($"BlockIp:{clientIp}", RedisDataBase.IP);
        }

        public string GetIpCountryName(string clientIp)
        {
            try
            {
                var uip = _tools.GetIpToLong(clientIp);
                var ipCountry = _IPCountryList.Find(x => x.IpFrom < uip && x.IpTo > uip);
                return ipCountry.Country;
            }
            catch (IPFormatException e)
            {
                _logger.LogError(e, e.Message);
                return "";
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return "";
            }
        }
    }
}
