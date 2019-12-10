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
                    if (tools.IsMobile())
                    {
                        entryUrl = $"{entryUrl}?lang={tpLanId}&homeUrl={parameters["homeUrl"]}&signupUrl={parameters["signupUrl"]}&LoginUrl={parameters["LoginUrl"]}";
                    }
                    else
                    {
                        entryUrl = $"{entryUrl}?lang={tpLanId}";//&market=[market column]&act=[act column]&menutype=[menu column]
                    }

                    return entryUrl;
                default:
                    return null;
            }
        }
    }
}
