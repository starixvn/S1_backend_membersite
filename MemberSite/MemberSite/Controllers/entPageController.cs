using MemberSite.Service.BLL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Serilog.Context;

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
        private readonly string _tpServiceApiUrl;
        private readonly ILogger _logger;

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
            var configBll = provider.GetRequiredService<ConfigBLL>();
            _tpServiceApiUrl = configBll.GetConfigValueByEnvironment<ServiceHostConfig>("ServiceHost").TpService;
            _logger = provider.GetRequiredService<ILogger<entPageController>>();
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

            //針對BTI強制走https避免js找不到
            var host = tpId == 29 ? $"https://{Request.Host}{Request.PathBase}" : $"{Request.Scheme}://{Request.Host}{Request.PathBase}";

            var authData = _authorizeBll.GetAuthData();
            if (authData is { CustId: > 0 })
            {
                var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
                if (custInfo.IsTest == false && _gameBll.IsMaintain(serverInfo.SiteId, tpId, inputs.GetValueOrDefault("tpGameType"), out var tpMaintainForNormalCust))
                {
                    var entPageUrl = new Uri($"{Request.Scheme}://{Request.Host}{Request.Path}{Request.QueryString}").AbsoluteUri;
                    var maintainUrl = _gameBll.GetMaintainUrl(tpMaintainForNormalCust, host, _selLangBll.GetLangId() ?? "en", entPageUrl);
                    return Redirect(maintainUrl);
                }
            }
            else
            {
                if (_gameBll.IsMaintain(serverInfo.SiteId, tpId, inputs.GetValueOrDefault("tpGameType"), out var tpMaintain))
                {
                    var entPageUrl = new Uri($"{Request.Scheme}://{Request.Host}{Request.Path}{Request.QueryString}").AbsoluteUri;
                    var maintainUrl = _gameBll.GetMaintainUrl(tpMaintain, host, _selLangBll.GetLangId() ?? "en", entPageUrl);
                    return Redirect(maintainUrl);
                }
            }

            var myUrl = $"{_tpServiceApiUrl}GotoGame";
            var errorUrl = string.Empty;
            var tpInfo = new TpInfo
            {
                TpId = tpId,
                AppType = _tools.IsMobile() ? 2 : 1,
                SiteId = serverInfo.SiteId,
            };

            inputs.Add("Host", host);
            inputs.Add("ClientIP", _tools.GetClientIP());

            if (inputs.ContainsKey("AppType") && int.TryParse(inputs["AppType"], out var appType))
            {
                tpInfo.AppType = appType;
            }

            if (inputs.ContainsKey("errorUrl"))
            {
                errorUrl = inputs["errorUrl"];
            }
            try
            {
                if (authData is { CustId: > 0 })
                {
                    var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);

                    if (tpId == (int)Tp.TCG && string.CompareOrdinal(custInfo.AffCode, "67253") == 0)
                    {
                        return RedirectErrorPage(errorUrl, new ErrorData("Access Denied"));
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

                return RedirectErrorPage(errorUrl, new ErrorData(data.Message ?? data.RawMessage));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return RedirectErrorPage(errorUrl, new ErrorData(ex.Message));
            }
        }

        public class ErrorData
        {
            public ErrorData() { }

            public ErrorData(string msg)
            {
                Message = msg;
            }

            public string Message { get; set; }
        }

        private IActionResult RedirectErrorPage(string errorUrl, ErrorData model)
        {
            var serverInfo = _serverInfoBll.GetServerInfo();
            if (serverInfo.SiteId is 1 or 2 or 3 or 5) //Nova88,Goal123,Bwing
            {
                return View($"~/Views/entPage/{serverInfo.SiteName}ErrorPage.cshtml", model);
            }
            else
            {
                return Redirect($"{errorUrl}?Message={model.Message}");
            }
        }
    }
}
