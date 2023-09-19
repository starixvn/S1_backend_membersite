using System.Collections.Concurrent;
using System.Reflection;
using System.Resources;

using MemberSite.Service.Library;
using MemberSite.Service.Model;

using Microsoft.Extensions.Caching.Memory;

namespace MemberSite.Service.BLL
{
    public class SelLangBLL : ISelLang
    {
        private readonly Dictionary<string, int> _langMapCurr =
        new(StringComparer.OrdinalIgnoreCase)
        {
            {"th", 4}, {"th_en", 4 }, {"cs", 13}, {"ch", 2}, {"id", 15}, {"id_en", 15},{"jp", 32},
            {"ko", 45}, {"vn", 51},{"hi", 61}, {"hi_en", 61}, {"kh", 72}
        };

        private readonly ServerInfoBll _serverInfoBll;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ConcurrentDictionary<string, ResourceManager> _rm = new();
        private readonly CacheHelper _cacheHelper;

        public SelLangBLL(IServiceProvider provider)
        {
            _serverInfoBll = provider.GetRequiredService<ServerInfoBll>();
            _httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
            _cacheHelper = provider.GetRequiredService<CacheHelper>();
        }

        public IEnumerable<SiteLanguage> GetLangIdList()
        {
            var serverInfo = _serverInfoBll.GetServerInfo();

            return serverInfo.ILangList.Select(langId => new SiteLanguage { LangId = langId, Language = GetLan_Info(langId) }).ToList();
        }

        public bool CheckLangId(string langId)
        {
            return GetLangIdList().Any(item => item.LangId == langId);
        }

        /// <summary>
        /// Get Language ID
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public string GetLangId()
        {
            var serverInfo = _serverInfoBll.GetServerInfo();
            var context = _httpContextAccessor.HttpContext;

            var langId = context?.User.Identity?.IsAuthenticated == true
                ? context.User.Claims.FirstOrDefault(m => m.Type == "LangId")?.Value
                : context?.Request.Cookies[$"{serverInfo.SiteName.ToLower()}LangId"];

            if (string.IsNullOrEmpty(langId))
            {
                var routeDataLandId = context?.GetRouteData().Values["LangId"] as string;
                if (!string.IsNullOrEmpty(routeDataLandId) && serverInfo.ILangList.Contains(routeDataLandId))
                {
                    langId = routeDataLandId;
                }
            }

            var userLangs = context?.Request.Headers["Accept-Language"].ToString();
            var firstLang = userLangs?.Split(',').FirstOrDefault();

            if (string.IsNullOrEmpty(langId) && !string.IsNullOrEmpty(firstLang))
            {
                if (firstLang.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
                {
                    langId = "cs";
                }
                else if (firstLang.StartsWith("th", StringComparison.OrdinalIgnoreCase))
                {
                    langId = "th";
                }
                else if (firstLang.StartsWith("vi", StringComparison.OrdinalIgnoreCase))
                {
                    langId = "vn";
                }
                else if (firstLang.StartsWith("id", StringComparison.OrdinalIgnoreCase))
                {
                    langId = "id";
                }
                else if (firstLang.StartsWith("jp", StringComparison.OrdinalIgnoreCase))
                {
                    langId = "jp";
                }
                else if (firstLang.StartsWith("ko", StringComparison.OrdinalIgnoreCase))
                {
                    langId = "ko";
                }
                else if (firstLang.StartsWith("pa", StringComparison.OrdinalIgnoreCase))
                {
                    langId = "hi";
                }
                else if (firstLang.StartsWith("ta", StringComparison.OrdinalIgnoreCase))
                {
                    langId = "hi";
                }
                else if (serverInfo.DefLang.Equals("vn", StringComparison.OrdinalIgnoreCase))
                {
                    return serverInfo.DefLang;
                }
                else if (firstLang.StartsWith("en", StringComparison.OrdinalIgnoreCase))
                {
                    langId = "en";
                }
                else if (firstLang.StartsWith("kh", StringComparison.OrdinalIgnoreCase))
                {
                    langId = "kh";
                }
                else
                {
                    langId = serverInfo.DefLang;
                }
            }

            if (string.IsNullOrEmpty(langId))
            {
                langId = serverInfo.DefLang;
            }
            else if (!serverInfo.ILangList.Contains(langId))
            {
                langId = serverInfo.DefLang;
            }

            return langId;
        }

        private string GetLan_Info(string key)
        {
            var sKey = $"{Assembly.GetExecutingAssembly().GetName().Name}.App_GlobalResources.Lan_Info";
            if (!_rm.ContainsKey(sKey))
            {
                _rm.TryAdd(sKey, new ResourceManager(sKey, Assembly.GetExecutingAssembly()));
            }
            var labelString = _rm[sKey].GetString(key);

            return labelString;
        }

        private string GetResourceValue(string key, string categoryId, string langId)
        {
            var labelString = string.Empty;
            var sKey = $"{Assembly.GetExecutingAssembly().GetName().Name}.App_GlobalResources.{categoryId}_{langId}";
            if (!_rm.ContainsKey(sKey))
            {
                _rm.TryAdd(sKey, new ResourceManager(sKey, Assembly.GetExecutingAssembly()));
            }

            try
            {
                labelString = _rm[sKey].GetString(key);
            }
            catch (MissingManifestResourceException)
            {

            }

            if (string.IsNullOrEmpty(labelString) && langId != "en")
            {
                labelString = GetResourceValue(key, categoryId, "en");
            }

            return labelString;
        }
        public string GetResourceValue(string key)
        {
            var labelString = "";
            var serverInfo = _serverInfoBll.GetServerInfo();
            if (serverInfo.SiteId == 1)
            {
                labelString = GetResourceValue(key, "MemberSite_lan", GetLangId());
            }
            else
            {
                try
                {
                    labelString = GetResourceValue(key, $"{serverInfo.SiteName}_lan", GetLangId());
                }
                catch (Exception)
                {
                    // ignored
                }

                if (string.IsNullOrEmpty(labelString))
                {
                    labelString = GetResourceValue(key, "MemberSite_lan", GetLangId());
                }
            }

            return labelString;
        }
        public string GetMsg(string key)
        {
            var msgString = "";
            var serverInfo = _serverInfoBll.GetServerInfo();
            if (serverInfo.SiteId == 1)
            {
                msgString = GetResourceValue(key, "msg", GetLangId());
            }
            else
            {
                try
                {
                    msgString = GetResourceValue(key, $"{serverInfo.SiteName}_msg", GetLangId());
                }
                catch (Exception)
                {
                    // ignored
                }

                if (string.IsNullOrEmpty(msgString))
                {
                    msgString = GetResourceValue(key, "msg", GetLangId());
                }
            }

            return msgString;
        }
        public string GetErrMsg(int code)
        {
            var labelString = GetResourceValue($"Code_{code}", "GameMsg_lan", GetLangId());

            return labelString;
        }

        public int GetPreferCurrencyId()
        {
            var serverInfo = _serverInfoBll.GetServerInfo();
            if (serverInfo.ICurrencyList == null)
            {
                return 0;
            }

            var langId = GetLangId();
            var defCurrencyId = serverInfo.DefCurrency;

            var userLangs = _httpContextAccessor.HttpContext?.Request.Headers["Accept-Language"].ToString();

            if (userLangs != null && userLangs.StartsWith("ms", StringComparison.OrdinalIgnoreCase) && serverInfo.ICurrencyList.Contains(2))/*Malay (Malaysia)*/
            {
                return 2;
            }

            if (_langMapCurr.ContainsKey(langId) && serverInfo.ICurrencyList.Contains(_langMapCurr[langId]))
            {
                return _langMapCurr[langId];
            }

            if (defCurrencyId != 0 && serverInfo.ICurrencyList.Contains(defCurrencyId))
            {
                return defCurrencyId;
            }

            return serverInfo.ICurrencyList.Any() ? serverInfo.ICurrencyList.FirstOrDefault() : 0;
        }

        public IEnumerable<Country> GetCountryList()
        {
            var key = $"CountryList_{GetLangId()}";
            return _cacheHelper.TryGetOrCreate(key,
                new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromHours(1)),
                i => GetCountryListImplement());
        }

        private IEnumerable<Country> GetCountryListImplement()
        {
            var countryList = _serverInfoBll.GetCountryList().ToList();
            foreach (var country in countryList)
            {
                country.CountryName = GetResourceValue($"lbl_Country{country.Code}");
            }

            return countryList;
        }
    }
}
