using System.Collections.Generic;
using System.Collections.Immutable;

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
        public readonly List<string> CountryList;

        public CheckIpBLL(IServiceProvider provider)
        {
            _tools = provider.GetRequiredService<Tools>();
            _logger = provider.GetRequiredService<ILogger<CheckIpBLL>>();
            _IPCountryList = provider.GetRequiredService<IEnumerable<IPCountry>>().ToList();
            _forbiddenCountries = provider.GetRequiredService<ConfigBLL>()
                .GetConfigValue<List<string>>("BlockCountry").ToDictionary(i => i, i => i);
            _cacheHelper = provider.GetRequiredService<ICacheHelper>();
            var countryList = _IPCountryList.Select(i => i.Country.Replace("-", "").Replace("\r", "")).Where(s => !string.IsNullOrWhiteSpace(s));
            CountryList = new(new HashSet<string>(countryList).OrderBy(i => i));
        }

        public async Task<IpCountryWithAccess> CheckIp(string clientIp)
        {
            try
            {
                var uip = _tools.GetIpToLong(clientIp);
                var ipCountry = _IPCountryList.Find(x => x.IpFrom < uip && x.IpTo > uip);

                // 1.檢查白名單
                if (await _cacheHelper.KeyExistsAsync($"AllowedIp:{clientIp}", RedisDataBase.IP))
                {
                    return new IpCountryWithAccess(clientIp, "in allow ip list.")
                    {
                        Code = ipCountry.Code,
                        Forbidden = false //access
                    };
                }

                // ip異常找不到國家回傳block
                if (ipCountry == null)
                {
                    return new IpCountryWithAccess(clientIp, "not found in IPCountryList");
                }

                // 2.檢查黑名單
                if (await CheckBlockIp(clientIp))
                {
                    return new IpCountryWithAccess(clientIp, "in block ip list.")
                    {
                        Code = ipCountry.Code,
                        Forbidden = true //block
                    };
                }

                // 3.檢查黑名單國家
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
                    //發生其他例外不阻擋，避免前台無法進入
                    Forbidden = false
                };
            }
        }

        public async Task<bool> CheckBlockIp(string clientIp)
        {
            return await _cacheHelper.KeyExistsAsync($"BlockIp:{clientIp}", RedisDataBase.IP);
        }



    }
}
