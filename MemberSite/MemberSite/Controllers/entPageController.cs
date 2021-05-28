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
using Microsoft.Extensions.Logging;
using Serilog.Context;

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
        private readonly ILogger _logger;

        public entPageController(IHostEnvironment environment, IConfiguration configuration,
            ThirdPartyBLL thirdPartyBLL, CourierAdapter courierAdapter, AuthorizeBLL authorizeBLL, Tools tools,
            SelLangBLL selLangBLL, WalletBLL walletBLL, CustInfoBLL custInfoBLL, ServerInfoBLL serverInfoBLL,
            ILogger<entPageController> logger)
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
            _logger = logger;
        }

        public class TpInfo
        {
            public int TpId { set; get; }
            public int CustId { set; get; }
            public string LangId { set; get; }
            public int AppType { set; get; }
            public int SiteId { set; get; }
            public Dictionary<string, string> inputs { set; get; }
        }

        [HttpGet]
        public async Task<IActionResult> IndexAsync(int tpId, Dictionary<string, string> inputs, [FromRoute] string LangId = null)
        {
            var serverInfo = serverInfoBLL.GetServerInfo();
            bool isNewVersion = true;
            var myUrl = $"{apiUrl}GotoGame";
            var tpInfo = new TpInfo
            {
                TpId = tpId,
                AppType = tools.IsMobile() ? 2 : 1,
                SiteId = serverInfo.SiteId,
            };

            inputs.Add("Host", $"{Request.Scheme}://{Request.Host}{Request.PathBase}");
            inputs.Add("ClientIP", tools.GetClientIP());

            if (inputs.ContainsKey("AppType") && int.TryParse(inputs["AppType"], out int appType))
            {
                tpInfo.AppType = appType;
            }

            if (IsFunGame(inputs))
            {
                tpInfo.CustId = 0;
                tpInfo.LangId = selLangBLL.GetLangId();
            }
            else if (User.Identity.IsAuthenticated)
            {
                var authData = authorizeBLL.GetAuthData();
                var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);

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
                tpInfo.CustId = authData.CustId;
                tpInfo.LangId = CheckLangId(LangId) ? LangId : custInfo.UserLang;
            }
            else if (hasBf(tpId))
            {
                LogContext.PushProperty("TpInfo", JsonConvert.SerializeObject(tpInfo));
                string entryUrl = thirdPartyBLL.getEntryUrl(tpId, inputs);
                _logger.LogInformation($"tpId={tpId}, Url={entryUrl}");
                return Redirect(entryUrl);
            }
            else
            {
                return StatusCode(400);
            }

            tpInfo.inputs = inputs;

            try
            {
                LogContext.PushProperty("TpInfo", JsonConvert.SerializeObject(tpInfo));
                var data = await courierAdapter.MyPostAsync<bool, TpInfo>(myUrl, tpInfo);
                if (data.ErrorCode == 0)
                {
                    string entryUrl = data.Message;
                    _logger.LogInformation($"ErrorCode=0, Url={entryUrl}");
                    return Redirect(entryUrl);
                }
                else if (tpId == 1 && data.RawErrorCode == "10")
                {
                    string entryUrl = thirdPartyBLL.getEntryUrl(tpId, inputs);
                    _logger.LogInformation($"tpId=1, RawErrorCode=10, Url={entryUrl}");
                    return Redirect(entryUrl);
                }
                else
                {
                    ErrorData model = new ErrorData() { Message = data.RawMessage };
                    if (isNewVersion)
                        return View($"~/Views/entPage/{serverInfo.SiteName}ErrorPage.cshtml", model);
                    else
                        return Content(model.Message);
                }
            }
            catch (Exception ex)
            {
                ErrorData model = new ErrorData() { Message = ex.Message };
                if (isNewVersion)
                    return View($"~/Views/entPage/{serverInfo.SiteName}ErrorPage.cshtml", model);
                else
                    return Content(model.Message);
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
            if (sNameSubTable != null)
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

        private bool IsFunGame(Dictionary<string, string> inputs)
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

        private bool CheckLangId(string LangId)
        {
            return selLangBLL.GetLangIdList().Any(item => item.LangId == LangId);
        }
    }
}
