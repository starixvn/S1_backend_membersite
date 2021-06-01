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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace MemberSite.Controllers
{
    public class entPageController : Controller
    {
        private readonly ThirdPartyBLL _thirdPartyBll;
        private readonly CourierAdapter _courierAdapter;
        private readonly AuthorizeBLL _authorizeBll;
        private readonly Tools _tools;
        private readonly SelLangBLL _selLangBll;
        private readonly WalletBLL _walletBll;
        private readonly CustInfoBLL _custInfoBll;
        private readonly ServerInfoBLL _serverInfoBll;
        private readonly GameBLL _gameBll;
        private readonly string _apiUrl;
        private readonly ILogger _logger;

        public entPageController(IServiceProvider provider)
        {
            _thirdPartyBll = provider.GetRequiredService<ThirdPartyBLL>();
            _courierAdapter = provider.GetRequiredService<CourierAdapter>();
            _authorizeBll = provider.GetRequiredService<AuthorizeBLL>();
            _tools = provider.GetRequiredService<Tools>();
            _selLangBll = provider.GetRequiredService<SelLangBLL>();
            _walletBll = provider.GetRequiredService<WalletBLL>();
            _custInfoBll = provider.GetRequiredService<CustInfoBLL>();
            _serverInfoBll = provider.GetRequiredService<ServerInfoBLL>();
            _gameBll =  provider.GetRequiredService<GameBLL>();
            _apiUrl = provider.GetRequiredService<IConfiguration>().GetValue<string>("TpService");
            _logger =  provider.GetRequiredService<ILogger<entPageController>>();
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
            var serverInfo = _serverInfoBll.GetServerInfo();
            bool isNewVersion = true;
            var myUrl = $"{_apiUrl}GotoGame";
            var tpInfo = new TpInfo
            {
                TpId = tpId,
                AppType = _tools.IsMobile() ? 2 : 1,
                SiteId = serverInfo.SiteId,
            };

            inputs.Add("Host", $"{Request.Scheme}://{Request.Host}{Request.PathBase}");
            inputs.Add("ClientIP", _tools.GetClientIP());

            if (inputs.ContainsKey("AppType") && int.TryParse(inputs["AppType"], out int appType))
            {
                tpInfo.AppType = appType;
            }

            if (_gameBll.IsFunGame(inputs))
            {
                tpInfo.CustId = 0;
                tpInfo.LangId = _selLangBll.GetLangId();
            }
            else if (User.Identity.IsAuthenticated)
            {
                var authData = _authorizeBll.GetAuthData();
                var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);

                if (tpId == (int)Tp.AG)
                {

                    var tpCurrency = _thirdPartyBll.GetTpCurrency(tpId, custInfo.CurrencyId);
                    var balance = _walletBll.GetBalance(authData.CustId);
                    var bonusAmount = _walletBll.GetBonus(authData.CustId, 2);

                    balance += bonusAmount;

                    if (tpCurrency.ExchangeRate != 0 && tpCurrency.ExchangeRate != 1)
                    {
                        balance /= tpCurrency.ExchangeRate;
                    }

                    inputs.Add("Balance", _tools.Floor(balance, 2).ToString());
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
                string entryUrl = _thirdPartyBll.getEntryUrl(tpId, inputs);
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
                var data = await _courierAdapter.MyPostAsync<bool, TpInfo>(myUrl, tpInfo);
                if (data.ErrorCode == 0)
                {
                    string entryUrl = data.Message;
                    _logger.LogInformation($"ErrorCode=0, Url={entryUrl}");
                    return Redirect(entryUrl);
                }
                else if (tpId == 1 && data.RawErrorCode == "10")
                {
                    string entryUrl = _thirdPartyBll.getEntryUrl(tpId, inputs);
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

        private bool CheckLangId(string LangId)
        {
            return _selLangBll.GetLangIdList().Any(item => item.LangId == LangId);
        }
    }
}
