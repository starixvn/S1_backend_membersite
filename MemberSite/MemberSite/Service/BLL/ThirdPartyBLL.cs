using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemberSite.Service.BLL
{
    public class ThirdPartyBLL
    {
        private readonly ThirdPartyService thirdPartyService;
        private readonly SelLangBLL selLangBLL;
        private readonly Tools tools;

        Dictionary<string, string> sportTypeDic = new Dictionary<string, string>() {
            {"1","soccer" },{"2","basketball" },{"3","football" },{"4","icehockey" },{"5","tennis" },{"8","baseball" } ,
            {"43","esports" },{"161","numbergame" },{"180","virtualsports" }
        };
        public ThirdPartyBLL(ThirdPartyService thirdPartyService, SelLangBLL selLangBLL, Tools tools)
        {
            this.thirdPartyService = thirdPartyService;
            this.selLangBLL = selLangBLL;
            this.tools = tools;
        }

        public string GetTpLanId(int tpId)
        {
            return thirdPartyService.GetTpLanId(tpId, selLangBLL.GetLangId());
        }

        public ThirdPartyModel GetThirdPartyModel(int tpId)
        {
            return thirdPartyService.GetThirdPartyModel(tpId);
        }

        public string getEntryUrl(int tpId, Dictionary<string, string> parameters)
        {
            Dictionary<string, string> inputs = JsonConvert.DeserializeObject<Dictionary<string, string>>(thirdPartyService.GetThirdPartyModel(tpId).Setting);
            string urlKey = "DesktopBfUrl";
            if (tools.IsMobile())
            {
                urlKey = "MobileBfUrl";
            }
            string entryUrl = inputs[urlKey];
            var tpLanId = thirdPartyService.GetTpLanId(tpId, selLangBLL.GetLangId());

            switch (tpId)
            {
                case 1:
                    string SportType = null;
                    parameters.TryGetValue("gameId", out SportType);

                    if (tools.IsMobile())
                    {
                        string types = $"{SportType},0,l";

                        entryUrl = $"{entryUrl}?lang={tpLanId}&homeUrl={parameters["homeUrl"]}&signupUrl={parameters["signupUrl"]}&LoginUrl={parameters["LoginUrl"]}#Sports/s={SportType}_0&d=l";
                    }
                    else
                    {
                        string act = null;
                        string gameId = null;

                        if (parameters.TryGetValue("gameId", out gameId))
                        {
                            sportTypeDic.TryGetValue(gameId, out act);
                        }

                        entryUrl = $"{entryUrl}?lang={tpLanId}&act={act}";
                    }

                    return entryUrl;
                default:
                    return null;
            }
        }
    }
}
