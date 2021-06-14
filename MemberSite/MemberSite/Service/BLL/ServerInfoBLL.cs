using MemberSite.Service.DAL;
using MemberSite.Service.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemberSite.Service.BLL
{
    public class ServerInfoBLL
    {
        private readonly ServerInfoService serverInfoService;
        //private ConcurrentDictionary<int, ServerInfo> rm = new ConcurrentDictionary<int, ResourceManager>();
        private ServerInfo serverInfo;/*Single Site*/
        private readonly IMemoryCache cache;
        private readonly IHttpContextAccessor httpContextAccessor;
        private ConcurrentDictionary<string, int> SiteInfoDic;
        private ConcurrentDictionary<int, ServerInfo> ServerInfoDic;
        private readonly int fixSiteID = 0;

        public ServerInfoBLL(IHttpContextAccessor httpContextAccessor, IMemoryCache cache, ServerInfoService serverInfoService, IConfiguration configuration)
        {
            this.serverInfoService = serverInfoService;
            this.serverInfo = null;
            this.cache = cache;
            this.httpContextAccessor = httpContextAccessor;
            SiteInfoDic = new ConcurrentDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            ServerInfoDic = new ConcurrentDictionary<int, ServerInfo>();
            fixSiteID = configuration.GetValue<int>("SiteId");
        }

        public ServerInfo GetServerInfo()
        {
            int siteId = 1;
            if (fixSiteID != 0)
            {
                siteId = fixSiteID;
            }
            else
            {
                string host = httpContextAccessor.HttpContext.Request.Host.Host;
                int dotIndex = host.IndexOf(".");
                if (dotIndex > -1)
                {
                    host = host.Substring(dotIndex + 1);
                }

                string key = $"{host}";
                if (cache.Get<ServerInfo>(key) != null)
                {
                    return cache.Get<ServerInfo>(key);
                }

                if (!SiteInfoDic.ContainsKey(host))
                {
                    int _siteId = serverInfoService.GetSite(host);
                    if (_siteId == 0)
                    {
                        cache.Set(key, GetServerInfo(1), new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(30)));
                        return GetServerInfo(1);
                    }
                    else
                    {
                        SiteInfoDic.TryAdd(host, _siteId);
                    }
                }

                siteId = SiteInfoDic[host];
            }

            serverInfo = GetServerInfo(siteId);
            return serverInfo;
        }

        private ServerInfo GetServerInfo(int siteId)
        {
            if (!ServerInfoDic.ContainsKey(siteId))
            {
                var serverInfo = serverInfoService.GetModel(siteId);
                if (serverInfo != null)
                {
                    ServerInfoDic.TryAdd(siteId, serverInfo);
                }
                else
                {
                    return null;
                }
            }

            return ServerInfoDic[siteId];
        }
        public IEnumerable<Country> GetCountryList()
        {
            var countryList = serverInfoService.GetCountryList();
            return countryList;
        }

        public void ResetServerInfo()
        {
            SiteInfoDic.Clear();
        }

        public string GetDomain()
        {
            return httpContextAccessor.HttpContext.Request.Host.Host;
        }
        public IEnumerable<Domainlist> GetSupportDomainList(int siteId, int currencyId)
        {
            return serverInfoService.GetSupportDomainList(siteId, currencyId);
        }

        public IEnumerable<Domainlist> GetSupportDomainListWithCache(int siteId, int currencyId)
        {
            string key = $"GetSupportDomainListWithCache_{siteId}_{currencyId}";
            var supportDomainList = cache.Get<IEnumerable<Domainlist>>(key);
            if (supportDomainList != null && supportDomainList.Any())
            {
                return supportDomainList;
            }

            supportDomainList = GetSupportDomainList(siteId, currencyId);
            if (supportDomainList != null && supportDomainList.Any())
            {
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(5));
                cache.Set(key, supportDomainList, cacheEntryOptions);
            }

            return supportDomainList;
        }
    }
}
