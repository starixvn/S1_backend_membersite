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
        private ConcurrentDictionary<string, string> ServerInfoDic;
        private readonly int fixSiteID = 0;

        public ServerInfoBLL(IHttpContextAccessor httpContextAccessor, IMemoryCache cache, ServerInfoService serverInfoService, IConfiguration configuration)
        {
            this.serverInfoService = serverInfoService;
            this.serverInfo = null;
            this.cache = cache;
            this.httpContextAccessor = httpContextAccessor;
            this.ServerInfoDic = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            fixSiteID = configuration.GetValue<int>("SiteId");
        }

        public ServerInfo GetServerInfo()
        {
            string siteId = "1";
            if (fixSiteID != 0)
                siteId = fixSiteID.ToString();
            else
            { 
                string host = httpContextAccessor.HttpContext.Request.Host.Host;

                if (!ServerInfoDic.ContainsKey(host))
                {
                    string _siteId = serverInfoService.GetSite(host);
                    if (_siteId != null)
                        ServerInfoDic.TryAdd(host, _siteId);
                }
                try
                {
                    ServerInfoDic.TryGetValue(host, out siteId);
                }
                catch (Exception ex)
                {
                }
            }
            if (siteId == null) siteId = "1";
            //if (serverInfo != null)
            //    return serverInfo;

            serverInfo = serverInfoService.GetModel(Int32.Parse(siteId));
            return serverInfo;
        }

        public IEnumerable<Country> GetCountryList()
        {
            var countryList = serverInfoService.GetCountryList();
            return countryList;
        }

        public void ResetServerInfo()
        {
            ServerInfoDic.Clear();
        }
    }
}
