using MemberSite.Service.DAL;
using MemberSite.Service.Model;
using Microsoft.Extensions.Caching.Memory;
using System;
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

        public ServerInfoBLL(IMemoryCache cache, ServerInfoService serverInfoService)
        {
            this.serverInfoService = serverInfoService;
            this.serverInfo = null;
            this.cache = cache;
        }

        public ServerInfo GetServerInfo()
        {
            if (serverInfo != null)
                return serverInfo;

            serverInfo = serverInfoService.GetModel();
            return serverInfo;
        }

        public IEnumerable<Country> GetCountryList()
        {
            var countryList = serverInfoService.GetCountryList();
            return countryList;
        }
    }
}
