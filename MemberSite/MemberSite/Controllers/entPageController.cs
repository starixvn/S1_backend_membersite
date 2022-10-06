using MemberSite.Service.BLL;
using MemberSite.Service.Library;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using MemberSite.Service.Model.enums;
using Serilog.Context;
using MemberSite.Service.DAL;
using Microsoft.Extensions.Options;
using MemberSite.Service.Model;
using System.Web;

namespace MemberSite.Controllers
{
    public class entPageController : Controller
    {
        private readonly ThirdPartyBll _thirdPartyBll;
        private readonly CourierAdapter _courierAdapter;
        private readonly AuthorizeBLL _authorizeBll;
        private readonly Tools _tools;
        private readonly SelLangBLL _selLangBll;
        private readonly WalletBLL _walletBll;
        private readonly CustInfoBll _custInfoBll;
        private readonly ServerInfoBll _serverInfoBll;
        private readonly GameBLL _gameBll;
        private readonly string _apiUrl;
        private readonly ILogger _logger;
        private readonly StatementService _statementService;
        private readonly IOptions<Dictionary<string, SiteConfig>> _siteConfig;

        public entPageController(IServiceProvider provider)
        {
            _thirdPartyBll = provider.GetRequiredService<ThirdPartyBll>();
            _courierAdapter = provider.GetRequiredService<CourierAdapter>();
            _authorizeBll = provider.GetRequiredService<AuthorizeBLL>();
            _tools = provider.GetRequiredService<Tools>();
            _selLangBll = provider.GetRequiredService<SelLangBLL>();
            _walletBll = provider.GetRequiredService<WalletBLL>();
            _custInfoBll = provider.GetRequiredService<CustInfoBll>();
            _serverInfoBll = provider.GetRequiredService<ServerInfoBll>();
            _gameBll = provider.GetRequiredService<GameBLL>();
            _apiUrl = provider.GetRequiredService<IConfiguration>().GetValue<string>("TpService");
            _logger = provider.GetRequiredService<ILogger<entPageController>>();
            _statementService = provider.GetRequiredService<StatementService>();
            _siteConfig = provider.GetRequiredService<IOptions<Dictionary<string, SiteConfig>>>();
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
            var tpMaintainList = _thirdPartyBll.GetTpMaintainListWithCache(serverInfo.SiteId);
            if (_gameBll.IsMaintain(tpMaintainList, tpId, inputs.GetValueOrDefault("tpGameType"), out var tpMaintain))
            {
                var tpName = _statementService.GetTpListWithCache()?.First(i => i.Id == tpMaintain.TpId)?.TpName;
                var host = _siteConfig.Value[serverInfo.SiteName].MaintainURL;
                var maintainUrl = $"{host}/index_{_selLangBll.GetLangId() ?? "en"}.html" +
                    $"?t={HttpUtility.UrlEncode(tpName)}&e={HttpUtility.UrlEncode($"{tpMaintain.EndDate:u}")}";
                return Redirect(maintainUrl);
            }

            var myUrl = $"{_apiUrl}GotoGame";
            var tpInfo = new TpInfo
            {
                TpId = tpId,
                AppType = _tools.IsMobile() ? 2 : 1,
                SiteId = serverInfo.SiteId,
            };

            inputs.Add("Host", $"{Request.Scheme}://{Request.Host}{Request.PathBase}");
            inputs.Add("ClientIP", _tools.GetClientIP());

            if (inputs.ContainsKey("AppType") && int.TryParse(inputs["AppType"], out var appType))
            {
                tpInfo.AppType = appType;
            }

            try
            {
                var authData = _authorizeBll.GetAuthData();
                if (authData is { CustId: > 0 })
                {
                    var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);

                    if (tpId == (int)Tp.TCG && string.CompareOrdinal(custInfo.AffCode, "67253") == 0)
                    {
                        return View($"~/Views/entPage/{serverInfo.SiteName}ErrorPage.cshtml",
                            new ErrorData() { Message = "Access Denied" });
                    }

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
                    tpInfo.LangId = _selLangBll.CheckLangId(LangId) ? LangId : custInfo.UserLang;
                }
                else
                {
                    tpInfo.CustId = 0;
                    tpInfo.LangId = _selLangBll.GetLangId();
                    inputs.TryAdd("fun", "1");
                }

                LogContext.PushProperty("TpInfo", JsonConvert.SerializeObject(tpInfo));
                tpInfo.inputs = inputs;
                var data = await _courierAdapter.MyPostAsync<bool, TpInfo>(myUrl, tpInfo);
                if (data.ErrorCode == 0)
                {
                    var entryUrl = data.Message;
                    return Redirect(entryUrl);
                }

                var model = new ErrorData() { Message = data.RawMessage };
                return View($"~/Views/entPage/{serverInfo.SiteName}ErrorPage.cshtml", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                var model = new ErrorData() { Message = ex.Message };
                return View($"~/Views/entPage/{serverInfo.SiteName}ErrorPage.cshtml", model);
            }
        }

        public class SNameSubTable
        {
            public int tpId { set; get; }
            public string gameId { set; get; }
        }

        [HttpGet]
        [Route("[controller]/{SportName}")]
        public async Task<IActionResult> OneworksAsync(string SportName)
        {
            var SNameTable = new Dictionary<string, SNameSubTable>
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
                var inputs = new Dictionary<string, string>();
                inputs.Add("gameId", sNameSubTable.gameId);
                return await IndexAsync(sNameSubTable.tpId, inputs);//.ConfigureAwait(false)
            }
            return StatusCode(400);
        }


        public class ErrorData
        {
            public string Message { get; set; }
        }
    }
}
