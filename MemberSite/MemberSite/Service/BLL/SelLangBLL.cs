using MemberSite.Service.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Threading.Tasks;

namespace MemberSite.Service.BLL
{
    public class SelLangBLL : ISelLang
    {
        private readonly ServerInfoBLL serverInfoBLL;
        private readonly IHttpContextAccessor httpContextAccessor;
        private ConcurrentDictionary<string, ResourceManager> rm = new ConcurrentDictionary<string, ResourceManager>();
        private string sLangId;

        public SelLangBLL(IHttpContextAccessor httpContextAccessor, ServerInfoBLL serverInfoBLL)
        {
            this.serverInfoBLL = serverInfoBLL;
            this.httpContextAccessor = httpContextAccessor;
            this.sLangId = null;
        }

        public IEnumerable<SiteLanguage> GetLangIdList()
        {
            List<SiteLanguage> list = new List<SiteLanguage>();
            var serverInfo = serverInfoBLL.GetServerInfo();

            foreach (var langId in serverInfo.ILangList)
            {
                SiteLanguage siteLanguage = new SiteLanguage();
                siteLanguage.LangId = langId;
                siteLanguage.Language = GetLan_Info(langId);
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

            var serverInfo = serverInfoBLL.GetServerInfo();
            var context = httpContextAccessor.HttpContext;
            if (context.User.Identity.IsAuthenticated)
            {
                langId = context.User.Claims.FirstOrDefault(m => m.Type == "LangId").Value;
            }
            else
            {
                langId = context.Request.Cookies[$"LangId_{serverInfo.SiteId}"];
            }

            if (string.IsNullOrEmpty(langId))
            {
                var routes = httpContextAccessor.HttpContext.GetRouteData();
                var routeDataLandId = routes.Values["LangId"] as string;
                if (!string.IsNullOrEmpty(routeDataLandId) && serverInfo.ILangList.Contains(routeDataLandId))
                {
                    langId = routeDataLandId;

                    /*CookieOptions option = new CookieOptions();
                    option.Expires = DateTime.Now.AddDays(10);
                    context.Response.Cookies.Append($"LangId_{serverInfo.SiteId}", langId, option);*/
                }
            }

            string userLangs = context.Request.Headers["Accept-Language"].ToString();
            string firstLang = userLangs.Split(',').FirstOrDefault();

            if (string.IsNullOrEmpty(langId) && !string.IsNullOrEmpty(firstLang))
            {
                if (firstLang.StartsWith("th", StringComparison.OrdinalIgnoreCase))
                {
                    langId = "th";
                }
                else if (firstLang.StartsWith("vi", StringComparison.OrdinalIgnoreCase))
                {
                    langId = "vn";
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

        private string GetLan_Info(string key)
        {
            //
            string labelString = string.Empty;
            string sKey = $"{Assembly.GetExecutingAssembly().GetName().Name}.App_GlobalResources.Lan_Info";
            if (!rm.ContainsKey(sKey))
            {
                rm.TryAdd(sKey, new ResourceManager(sKey, Assembly.GetExecutingAssembly()));
            }
            labelString = rm[sKey].GetString(key);

            return labelString;
        }

        private string GetResourceValue(string key, string categoryId, string langId)
        {
            //
            string labelString = string.Empty;
            string sKey = $"{Assembly.GetExecutingAssembly().GetName().Name}.App_GlobalResources.{categoryId}_{langId}";
            if (!rm.ContainsKey(sKey))
            {
                rm.TryAdd(sKey, new ResourceManager(sKey, Assembly.GetExecutingAssembly()));
            }
            labelString = rm[sKey].GetString(key);

            if (string.IsNullOrEmpty(labelString) && langId != "en")
            {
                labelString = GetResourceValue(key, categoryId, "en");
            }

            return labelString;
        }
        public string GetResourceValue(string key)
        {
            string labelString = GetResourceValue(key, "MemberSite_lan", GetLangId());

            return labelString;
        }
        public string GetMsg(string key)
        {
            string labelString = GetResourceValue(key, "msg", GetLangId());

            return labelString;
        }
        public string GetErrMsg(int code)
        {
            string labelString = GetResourceValue($"Code_{code}", "GameMsg_lan", GetLangId());

            return labelString;
        }
    }
}
