using MemberSite.Service.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Resources;
using MemberSite.Service.Library;
using MemberSite.Service.Model.enums;
using Microsoft.Extensions.DependencyInjection;

namespace MemberSite.Service.BLL
{
    public class SelLangBLL : ISelLang
    {
        private readonly Dictionary<string, int> _langMapCurr =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            {"th", 4}, {"th_en", 4 }, {"cs", 13}, {"ch", 2}, {"id", 15}, {"id_en", 15},{"jp", 32},
            {"ko", 45}, {"vn", 51},{"hi", 61}, {"hi_en", 61}
        };

        private readonly ServerInfoBll _serverInfoBll;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ConcurrentDictionary<string, ResourceManager> _rm = new ConcurrentDictionary<string, ResourceManager>();
        private string _sLangId;
        private readonly CacheHelper _cacheHelper;

        public SelLangBLL(IServiceProvider provider)
        {
            _serverInfoBll = provider.GetRequiredService<ServerInfoBll>();
            _httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
            _sLangId = null;
            _cacheHelper = provider.GetRequiredService<CacheHelper>();
        }

        public IEnumerable<SiteLanguage> GetLangIdList()
        {
            var list = new List<SiteLanguage>();
            var serverInfo = _serverInfoBll.GetServerInfo();

            foreach (var langId in serverInfo.ILangList)
            {
                var siteLanguage = new SiteLanguage
                {
                    LangId = langId,
                    Language = GetLan_Info(langId)
                };
                list.Add(siteLanguage);
            }

            return list;
        }

        /// <summary>
        /// Get Language ID
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public string GetLangId()
        {
            string langId = null;

            var serverInfo = _serverInfoBll.GetServerInfo();
            var context = _httpContextAccessor.HttpContext;
            if (context.User.Identity.IsAuthenticated)
            {
                langId = context.User.Claims.FirstOrDefault(m => m.Type == "LangId")?.Value;
            }
            else
            {
                //langId = context.Request.Cookies[$"LangId_{serverInfo.SiteId}"];
                langId = context.Request.Cookies[$"{serverInfo.SiteName.ToLower()}LangId"];
            }

            if (string.IsNullOrEmpty(langId))
            {
                var routes = _httpContextAccessor.HttpContext.GetRouteData();
                var routeDataLandId = routes.Values["LangId"] as string;
                if (!string.IsNullOrEmpty(routeDataLandId) && serverInfo.ILangList.Contains(routeDataLandId))
                {
                    langId = routeDataLandId;

                    /*CookieOptions option = new CookieOptions();
                    option.Expires = DateTime.Now.AddDays(10);
                    context.Response.Cookies.Append($"LangId_{serverInfo.SiteId}", langId, option);*/
                }
            }

            var userLangs = context.Request.Headers["Accept-Language"].ToString();
            var firstLang = userLangs.Split(',').FirstOrDefault();

            if (string.IsNullOrEmpty(langId) && !string.IsNullOrEmpty(firstLang))
            {
                if (firstLang.StartsWith("ms", StringComparison.OrdinalIgnoreCase) || firstLang.StartsWith("zh-tw", StringComparison.OrdinalIgnoreCase) || firstLang.StartsWith("zh-hk", StringComparison.OrdinalIgnoreCase))
                {
                    langId = "ch";
                }
                else if (firstLang.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
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

        public string GetLan_Info(string key)
        {
            //
            var labelString = string.Empty;
            var sKey = $"{Assembly.GetExecutingAssembly().GetName().Name}.App_GlobalResources.Lan_Info";
            if (!_rm.ContainsKey(sKey))
            {
                _rm.TryAdd(sKey, new ResourceManager(sKey, Assembly.GetExecutingAssembly()));
            }
            labelString = _rm[sKey].GetString(key);

            return labelString;
        }

        private string GetResourceValue(string key, string categoryId, string langId)
        {
            //
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
            catch (MissingManifestResourceException mme)
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
                catch (Exception ex)
                {

                }

                if (string.IsNullOrEmpty(labelString))
                {
                    labelString = GetResourceValue(key, "MemberSite_lan", GetLangId());
                }
            }
            //

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
                catch (Exception ex)
                {

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

            var userLangs = _httpContextAccessor.HttpContext.Request.Headers["Accept-Language"].ToString();

            if (userLangs.StartsWith("ms", StringComparison.OrdinalIgnoreCase) && serverInfo.ICurrencyList.Contains(2))/*Malay (Malaysia)*/
            {
                return 2;
            }
            else if (_langMapCurr.ContainsKey(langId) && serverInfo.ICurrencyList.Contains(_langMapCurr[langId]))
            {
                return _langMapCurr[langId];
            }
            else if (defCurrencyId != 0 && serverInfo.ICurrencyList.Contains(defCurrencyId))
            {
                return defCurrencyId;
            }
            else if (serverInfo.ICurrencyList.Any())
            {
                foreach (var currencyId in serverInfo.ICurrencyList)
                {
                    return currencyId;
                }
            }

            return 0;
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
