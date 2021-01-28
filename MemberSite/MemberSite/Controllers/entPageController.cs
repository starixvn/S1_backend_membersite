using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MemberSite.Service.BLL;
using MemberSite.Service.Library;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using MemberSite.Service.Model.enums;
using Microsoft.Extensions.Hosting;

namespace MemberSite.Controllers
{
    public class entPageController : Controller
    {
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly CourierAdapter courierAdapter;
        private readonly AuthorizeBLL authorizeBLL;
        private readonly Tools tools;
        private readonly SelLangBLL selLangBLL;
        private readonly WalletBLL walletBLL;
        private readonly CustInfoBLL custInfoBLL;
        private readonly ServerInfoBLL serverInfoBLL;
        private readonly string apiUrl;
        private readonly IHostEnvironment environment;

        public entPageController(IHostEnvironment environment, IConfiguration configuration, ThirdPartyBLL thirdPartyBLL, CourierAdapter courierAdapter, AuthorizeBLL authorizeBLL, Tools tools, SelLangBLL selLangBLL, WalletBLL walletBLL, CustInfoBLL custInfoBLL, ServerInfoBLL serverInfoBLL)
        {
            this.thirdPartyBLL = thirdPartyBLL;
            this.courierAdapter = courierAdapter;
            this.authorizeBLL = authorizeBLL;
            this.tools = tools;
            this.selLangBLL = selLangBLL;
            this.walletBLL = walletBLL;
            this.custInfoBLL = custInfoBLL;
            this.serverInfoBLL = serverInfoBLL;
            apiUrl = configuration.GetValue<string>("TpService");
            this.environment = environment;
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
            int SiteId = 0;
            if (User.Identity.IsAuthenticated)
            {
                var authData = authorizeBLL.GetAuthData();
                inputs.Add("Host", $"{Request.Scheme}://{Request.Host}{Request.PathBase}");
                inputs.Add("ClientIP", tools.GetClientIP());
                var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);
                SiteId = custInfo.SiteId;
                if (tpId == (int)Tp.AG)
                {

                    var tpCurrency = thirdPartyBLL.GetTpCurrency(tpId, custInfo.CurrencyId);
                    var balance = walletBLL.GetBalance(authData.CustId);
                    var bonusAmount = walletBLL.GetBonus(authData.CustId, 2);

                    balance += bonusAmount;

                    if (tpCurrency.ExchangeRate != 0 && tpCurrency.ExchangeRate != 1)
                    {
                        balance /= tpCurrency.ExchangeRate;
                    }

                    inputs.Add("Balance", tools.Floor(balance, 2).ToString());
                    inputs.Add("Token", authData.Token);
                }
                else
                {
                    inputs.Add("Token", authData.Token);
                }

                var myUrl = $"{apiUrl}GotoGame";
                var tpInfo = new TpInfo
                {
                    TpId = tpId,
                    CustId = authData.CustId,
                    LangId = custInfo.UserLang,
                    AppType = tools.IsMobile() ? 2 : 1,
                    inputs = inputs
                };

                if (inputs.ContainsKey("AppType") && int.TryParse(inputs["AppType"], out int appType))
                {
                    tpInfo.AppType = appType;
                }

                try
                {
                    var data = await courierAdapter.MyPostAsync<bool, TpInfo>(myUrl, tpInfo);
                    if (data.ErrorCode == 0)
                    {
                        string entryUrl = data.Message;
                        return Redirect(entryUrl);
                    }
                    else if (tpId == 1 && data.RawErrorCode == "10")
                    {
                        string entryUrl = thirdPartyBLL.getEntryUrl(tpId, inputs);
                        return Redirect(entryUrl);
                    }
                    else
                    {
                        ErrorData model = new ErrorData() { Message = data.RawMessage };

                        return GetErrorPage(SiteId, model);
                    }
                }
                catch (Exception ex)
                {
                    ErrorData model = new ErrorData() { Message = ex.Message };

                    return GetErrorPage(SiteId, model);
                }
            }
            else if (hasBf(tpId))
            {
                var serverInfo = serverInfoBLL.GetServerInfo();
                SiteId = serverInfo.SiteId;
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

        public class SNameSubTable
        {
            public int tpId { set; get; }
            public string gameId { set; get; }
        }

        [HttpGet]
        [Route("[controller]/{SportName}")]

        
        public async Task<IActionResult> OneworksAsync(string SportName)
        {
            Dictionary<string, SNameSubTable> SNameTable = new Dictionary<string, SNameSubTable>
            {
                {"sports",        new SNameSubTable { tpId = 1, gameId = "1" } },
                {"eSport",        new SNameSubTable { tpId = 1, gameId = "43" } },
                {"numberGame",    new SNameSubTable { tpId = 1, gameId = "161" } },
                {"virtualSports", new SNameSubTable { tpId = 1, gameId = "180" } },
                {"lottery",       new SNameSubTable { tpId = 1, gameId = "220" } },
                {"keno",          new SNameSubTable { tpId = 1, gameId = "202" } },
                {"fishing",       new SNameSubTable { tpId = 7, gameId = "10" } },
                {"cockFighting",  new SNameSubTable { tpId = 17, gameId = "" } },
                {"sabaClub",      new SNameSubTable { tpId = 1, gameId = "200" } },
                {"soccer",        new SNameSubTable { tpId = 1, gameId = "1" } },
                {"basketball",    new SNameSubTable { tpId = 1, gameId = "2" } },
                {"badminton",     new SNameSubTable { tpId = 1, gameId = "9" } },
                {"football",      new SNameSubTable { tpId = 1, gameId = "3" } },
                {"tennis",        new SNameSubTable { tpId = 1, gameId = "5" } },
                {"hockey",        new SNameSubTable { tpId = 1, gameId = "4" } },
                {"baseball",      new SNameSubTable { tpId = 1, gameId = "8" } }

            };
            var sNameSubTable = new SNameSubTable();
            SNameTable.TryGetValue(SportName, out sNameSubTable);
            if(sNameSubTable != null)
            {
                Dictionary<string, string> inputs = new Dictionary<string, string>();
                inputs.Add("gameId", sNameSubTable.gameId);
                return await IndexAsync(sNameSubTable.tpId, inputs);//.ConfigureAwait(false)
            }
            return StatusCode(400);
        }


        private bool hasBf(int tpId)
        {
            return Bf.ContainsKey(tpId);
        }

        public class ErrorData
        {
            public string Message { get; set; }
        }

        private ActionResult GetErrorPage(int SiteId, ErrorData model)
        {
            switch (SiteId)
            {
                case 1:
                    return View("~/Views/entPage/NovaErrorPage.cshtml", model);
                case 2:
                    return View("~/Views/entPage/BwingErrorPage.cshtml", model);
                default:
                    return View("~/Views/entPage/GoalErrorPage.cshtml", model);
            }
        }
    }
}