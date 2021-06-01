using System.Collections.Generic;

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
    }
}