using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MemberSite.Service.BLL
{
    public class ServerInfoBll
    {
        private readonly ServerInfoService _serverInfoService;
        private ServerInfo _serverInfo;
        private readonly IMemoryCache _cache;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ConcurrentDictionary<string, int> _siteInfoDic;
        private readonly ConcurrentDictionary<int, ServerInfo> _serverInfoDic;
        private readonly int _fixSiteId;
        private readonly CacheHelper _cacheHelper;

        public ServerInfoBll(IServiceProvider provider)
        {
            _serverInfoService = provider.GetRequiredService<ServerInfoService>();
            _cache = provider.GetRequiredService<IMemoryCache>();
            _httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
            _siteInfoDic = new ConcurrentDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            _serverInfoDic = new ConcurrentDictionary<int, ServerInfo>();
            _fixSiteId = provider.GetRequiredService<IConfiguration>().GetValue<int>("SiteId");
            _cacheHelper = provider.GetRequiredService<CacheHelper>();
        }

        public ServerInfo GetServerInfo()
        {
            var host = GetMainDomain();
            var siteId = 1;
            if (_fixSiteId != 0)
            {
                siteId = _fixSiteId;
            }
            else
            {
                var key = $"{host}";
                if (_cache.TryGetValue<ServerInfo>(key, out var serverInfo))
                {
                    return serverInfo;
                }

                if (!_siteInfoDic.ContainsKey(host))
                {
                    var _siteId = _serverInfoService.GetSite(host);
                    if (_siteId == 0)
                    {
                        _cache.Set(key, GetServerInfo(1), new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(30)));
                        return GetServerInfo(1);
                    }

                    _siteInfoDic.TryAdd(host, _siteId);
                }

                siteId = _siteInfoDic[host];
            }

            _serverInfo = GetServerInfo(siteId);
            _serverInfo.host = host;
            return _serverInfo;
        }

        private ServerInfo GetServerInfo(int siteId)
        {
            if (!_serverInfoDic.ContainsKey(siteId))
            {
                var serverInfo = _serverInfoService.GetModel(siteId);
                if (serverInfo != null)
                {
                    _serverInfoDic.TryAdd(siteId, serverInfo);
                }
                else
                {
                    return null;
                }
            }

            return _serverInfoDic[siteId];
        }
        public IEnumerable<Country> GetCountryList()
        {
            var countryList = _serverInfoService.GetCountryList();
            return countryList;
        }

        public string GetDomain()
        {
            return _httpContextAccessor.HttpContext.Request.Host.Host;
        }
        
        public string GetMainDomain()
        {
            var match = Regex.Match(GetDomain(), "([^.]+\\.[^.]{1,3}(\\.[^.]{1,3})?)$");
            var domain = match.Groups[1].Success ? match.Groups[1].Value : string.Empty;
            return domain;
        }
        
        public IEnumerable<Domainlist> GetSupportDomainList(int siteId, int currencyId)
        {
            return _serverInfoService.GetSupportDomainList(siteId, currencyId);
        }

        public IEnumerable<Domainlist> GetSupportDomainListWithCache(int siteId, int currencyId)
        {
            var key = $"GetSupportDomainListWithCache_{siteId}_{currencyId}";
            return _cacheHelper.TryGetOrCreate(key,
                new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(5)),
                i => GetSupportDomainList(siteId, currencyId));
        }

        public SeoModel GetSeoWithCache(string domain)
        {
            var key = $"GetSeoWithCache_{domain}";
            return _cacheHelper.TryGetOrCreate(key,
                new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(10)),
                i => _serverInfoService.GetSeo(domain));
        }
    }
}
