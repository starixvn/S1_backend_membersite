using MemberSite.Service.DAL;
using MemberSite.Service.Model;
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
        public ServerInfoBLL(ServerInfoService serverInfoService)
        {
            this.serverInfoService = serverInfoService;
            this.serverInfo = null;
        }

        public ServerInfo GetServerInfo()
        {
            if (serverInfo != null)
                return serverInfo;

            serverInfo = serverInfoService.GetModel();
            return serverInfo;
        }
    }
}
