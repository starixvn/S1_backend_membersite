using MemberSite.Service.Model;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MemberSite.Service.BLL
{
    public class GameBLL
    {
        private readonly ServerInfoBLL _serverInfoBLL;
        private readonly ThirdPartyBLL _thirdPartyBll;
        public GameBLL(IServiceProvider provider)
        {
            _serverInfoBLL = provider.GetRequiredService<ServerInfoBLL>();
            _thirdPartyBll = provider.GetRequiredService<ThirdPartyBLL>();
        }

        public bool IsFunGame(Dictionary<string, string> inputs)
        {
            if (inputs.ContainsKey("gameType") && inputs["gameType"] == "slots" && inputs.ContainsKey("fun") && inputs["fun"] == "1")
            {
                return true;
            }

            if (inputs.ContainsKey("gameType") && inputs["gameType"] == "sports" && inputs.ContainsKey("fun") && inputs["fun"] == "1")
            {
                return true;
            }

            return false;
        }

        public bool IsMaintain(int tpId, Dictionary<string, string> inputs)
        {
            try
            {          
                var siteId = _serverInfoBLL.GetServerInfo().SiteId;
                var tpMaintains = _thirdPartyBll.GetTpMaintainListWithCache(siteId).Where(i => i.TpId == tpId);

                if (!tpMaintains.Any())
                {
                    return false;
                }

                if (!inputs.TryGetValue("tpGameType", out var tpGameType))
                {
                    return false;
                }

                foreach (var item in tpMaintains)
                {
                    if (item.TpGameType == 0 || item.TpGameType == int.Parse(tpGameType))
                    {
                        return true;
                    }
                }

                return false;
            }
            catch (Exception)
            {

                return false;
            }
        }
    }
}