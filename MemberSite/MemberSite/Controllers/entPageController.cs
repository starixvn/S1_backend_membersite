using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MemberSite.Service.BLL;
using MemberSite.Service.Library;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace MemberSite.Controllers
{
    public class entPageController : Controller
    {
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly CourierAdapter courierAdapter;
        private readonly AuthorizeBLL authorizeBLL;
        private readonly Tools tools;
        private readonly SelLangBLL selLangBLL;
        private readonly string apiUrl;

        public entPageController(IConfiguration configuration, ThirdPartyBLL thirdPartyBLL, CourierAdapter courierAdapter, AuthorizeBLL authorizeBLL,Tools tools, SelLangBLL selLangBLL)
        {
            this.thirdPartyBLL = thirdPartyBLL;
            this.courierAdapter = courierAdapter;
            this.authorizeBLL = authorizeBLL;
            this.tools = tools;
            this.selLangBLL = selLangBLL;
            apiUrl = configuration.GetValue<string>("TpService");
        }
        public class TpInfo
        {
            public int TpId { set; get; }
            public int CustId { set; get; }
            public string LangId { set; get; }
            public int AppType { set; get; }
            public Dictionary<string, string> inputs { set; get; }
        }

        [HttpGet]
        public async Task<IActionResult> IndexAsync(int tpId, Dictionary<string, string> inputs)
        {
            if (User.Identity.IsAuthenticated)
            {
                var authData = authorizeBLL.GetAuthData();

                inputs.Add("Host", $"{Request.Scheme}://{Request.Host}{Request.PathBase}");
                inputs.Add("ClientIP", tools.GetClientIP());

                var myUrl = $"{apiUrl}GotoGame";
                var tpInfo = new TpInfo
                {
                    TpId = tpId,
                    CustId = authData.CustId,
                    LangId = selLangBLL.GetLangId(),
                    AppType = tools.IsMobile() ? 2 : 1,
                    inputs = inputs
                };

                var data = await courierAdapter.MyPostAsync<bool, TpInfo>(myUrl, tpInfo);
                if (data.ErrorCode == 0)
                {
                    string entryUrl = data.Message;
                    return Redirect(entryUrl);
                }
                else
                {
                    throw new Exception(data.RawMessage);
                }
            }
            else if (hasBf(tpId))
            {
                string entryUrl = thirdPartyBLL.getEntryUrl(tpId, inputs);

                return Redirect(entryUrl);
            }
            else
            {
                return StatusCode(400);
            }
        }

        Dictionary<int, bool> Bf = new Dictionary<int, bool>()
        {
           {1,false }
        };


        private bool hasBf(int tpId)
        {
            return Bf.ContainsKey(tpId);
        }
    }
}