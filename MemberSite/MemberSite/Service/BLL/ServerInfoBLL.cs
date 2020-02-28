using MemberSite.Service.DAL;
using MemberSite.Service.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
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
        private ConcurrentDictionary<string, ServerInfo> ServerInfoDic;

        public ServerInfoBLL(IHttpContextAccessor httpContextAccessor, IMemoryCache cache, ServerInfoService serverInfoService)
        {
            this.serverInfoService = serverInfoService;
            this.serverInfo = null;
            this.cache = cache;
            this.httpContextAccessor = httpContextAccessor;
            this.ServerInfoDic = new ConcurrentDictionary<string, ServerInfo>(StringComparer.OrdinalIgnoreCase);
        }

        public ServerInfo GetServerInfo()
        {
            //httpContextAccessor.HttpContext.Request.Host.Host
            //ServerInfoDic.ContainsKey
            if (serverInfo != null)
                return serverInfo;
            //httpContextAccessor.HttpContext.Request.Host.Host

            serverInfo = serverInfoService.GetModel();
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
