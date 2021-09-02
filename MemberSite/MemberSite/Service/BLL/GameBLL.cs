using MemberSite.Service.Model;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MemberSite.Service.BLL
{
    public class GameBLL
    {

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

        public bool IsMaintain(IEnumerable<TpMaintain> tpMaintainList, Dictionary<string, string> inputs , out TpMaintain result)
        {
            result = null;
            try
            {           
                if (!tpMaintainList.Any())
                {
                    return false;
                }

                if (!inputs.TryGetValue("tpGameType", out var tpGameType))
                {
                    return false;
                }

                var gameType = int.Parse(tpGameType);
                foreach (var item in tpMaintainList)
                {
                    if (item.TpGameType == 0 || item.TpGameType == gameType)
                    {
                        result = item;
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