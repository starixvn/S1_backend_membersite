using MemberSite.Service.Model;
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

        public bool IsMaintain(IEnumerable<TpMaintain> tpMaintainList, int tpId, string tpGameType, out TpMaintain result)
        {
            result = null;
            try
            {
                if (!tpMaintainList.Any())
                {
                    return false;
                }

                if (string.IsNullOrEmpty(tpGameType))
                {
                    return false;
                }

                var gameType = int.Parse(tpGameType);
                foreach (var item in tpMaintainList)
                {
                    if ((item.TpId == tpId && item.TpGameType == 0) || (item.TpId == tpId && item.TpGameType == gameType))
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