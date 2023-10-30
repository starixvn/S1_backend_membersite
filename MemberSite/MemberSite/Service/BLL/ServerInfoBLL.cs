using System.Collections.Concurrent;

using MemberSite.Service.DAL.Interface;
using MemberSite.Service.Library;
using MemberSite.Service.Model;

using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;

using Newtonsoft.Json;

namespace MemberSite.Service.BLL
{
    public class ServerInfoBll
    {
        private readonly IServerInfoService _serverInfoService;
        private ServerInfo _serverInfo;
        private readonly IMemoryCache _cache;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ConcurrentDictionary<string, int> _siteInfoDic;
        private readonly ConcurrentDictionary<int, ServerInfo> _serverInfoDic;
        private readonly int _fixSiteId;
        private readonly CacheHelper _cacheHelper;
        private readonly List<string> _seoDomain = new List<string>() { };
        private readonly string[] _appHost = { "app.n17881788.com", "app.bwing.com", "app.goal123.com" };
        private readonly Dictionary<int, Dictionary<string, string>> _socialChannelLogoLinks;

        public ServerInfoBll(IServiceProvider provider)
        {
            _serverInfoService = provider.GetRequiredService<IServerInfoService>();
            _cache = provider.GetRequiredService<IMemoryCache>();
            _httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
            _siteInfoDic = new ConcurrentDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            _serverInfoDic = new ConcurrentDictionary<int, ServerInfo>();
            _fixSiteId = provider.GetRequiredService<IConfiguration>().GetValue<int>("SiteId");
            _cacheHelper = provider.GetRequiredService<CacheHelper>();
            _socialChannelLogoLinks = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<Dictionary<string, string>>("SocialChannelLogoLink");
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
            // 先拿Header的ai-host
            if (_httpContextAccessor.HttpContext.Request.Headers.TryGetValue("ai-host", out var aiHost))
            {
                return new Uri(aiHost).Host;
            }

            // 都拿不到直接用Request的Host
            return _httpContextAccessor.HttpContext.Request.Host.Host;
        }

        public string GetRequestUrl()
        {
            return _httpContextAccessor.HttpContext?.Request.GetDisplayUrl();
        }

        public string GetMainDomain()
        {
            var host = GetDomain();
            var dotIndex = host.IndexOf(".");
            if (dotIndex > -1)
            {
                host = host.Substring(dotIndex + 1);
            }

            return host;
        }

        public string GetSubDomain()
        {
            var host = GetDomain();
            var dotIndex = host.IndexOf(".");
            if (dotIndex > -1)
            {
                host = host.Substring(0, dotIndex);
            }

            return host;
        }

        public string GetHttpDomain()
        {
            var http = _httpContextAccessor.HttpContext.Request.Scheme;
            var host = _httpContextAccessor.HttpContext.Request.Host.Host;

            if (_appHost.Any(a => a == host))
                host = $"m{host[host.IndexOf('.')..]}";

            return $"{http}://{host}";
        }

        public IEnumerable<Domainlist> GetSupportDomainList(int siteId, int currencyId)
        {
            return _serverInfoService.GetSupportDomainList(siteId, currencyId);
        }

        public IEnumerable<Domainlist> GetSupportDomainListWithCache(int siteId, int currencyId)
        {
            var key = $"GetSupportDomainListWithCache_{siteId}_{currencyId}";
            return _cacheHelper.TryGetOrCreate(key,
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(5)),
                i => GetSupportDomainList(siteId, currencyId));
        }

        public SeoModel GetSeoWithCache(string domain)
        {
            var key = $"GetSeoWithCache_{domain}";
            return _cacheHelper.TryGetOrCreate(key,
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(10)),
                i => _serverInfoService.GetSeo(domain));
        }

        public async Task<IEnumerable<MenuOfGame>> GetMenuOfGames(int siteId, string langId)
        {
            var menuOfGamesData = await _serverInfoService.GetMenuOfGames(siteId, langId);
            var menuOfGames = menuOfGamesData
                .GroupBy(a => a.MenuId)
                .Select(b => new MenuOfGame()
                {
                    MenuId = b.Key,
                    MenuItems = b.Select(c => new MenuItem()
                    {
                        TpId = c.TpId,
                        GameId = c.GameId,
                        TpGameType = c.TpGameType,
                        Sort = c.Sort
                    }).ToList()
                });
            return menuOfGames;
        }

        public bool IsSeoDomain(string domain)
        {
            return _seoDomain.Exists(seoDomain => seoDomain == domain);
        }

        public async Task<IEnumerable<SocialChannelResponse>> GetSocialChannel(SocialChannelRequest request)
        {
            var result = await _serverInfoService.GetSocialChannel(request);
            var socialChannelLogoLink = _socialChannelLogoLinks[request.SiteId];
            var items = JsonConvert.DeserializeObject<IEnumerable<SocialChannelResponse>>(JsonConvert.SerializeObject(result));

            foreach (var item in items)
            {
                item.SocialChannelLogoLink = socialChannelLogoLink.TryGetValue(item.ChannelName, out var logoLink) ? logoLink : string.Empty;
                item.Scheme = new SocialChannelApp
                {
                    Android = result.First(i => i.Id == item.Id).Android,
                    Ios = result.First(i => i.Id == item.Id).Ios
                };
            }
            return items;
        }
    }
}
