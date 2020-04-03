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
        private readonly string apiUrl;
        private readonly IHostEnvironment environment;

        public entPageController(IHostEnvironment environment, IConfiguration configuration, ThirdPartyBLL thirdPartyBLL, CourierAdapter courierAdapter, AuthorizeBLL authorizeBLL, Tools tools, SelLangBLL selLangBLL, WalletBLL walletBLL, CustInfoBLL custInfoBLL)
        {
            this.thirdPartyBLL = thirdPartyBLL;
            this.courierAdapter = courierAdapter;
            this.authorizeBLL = authorizeBLL;
            this.tools = tools;
            this.selLangBLL = selLangBLL;
            this.walletBLL = walletBLL;
            this.custInfoBLL = custInfoBLL;
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
            if (User.Identity.IsAuthenticated)
            {
                var authData = authorizeBLL.GetAuthData();

                inputs.Add("Host", $"{Request.Scheme}://{Request.Host}{Request.PathBase}");
                inputs.Add("ClientIP", tools.GetClientIP());

                if (tpId == (int)Tp.AG)
                {
                    var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);
                    var tpCurrency = thirdPartyBLL.GetTpCurrency(tpId, custInfo.CurrencyId);
                    var balance = walletBLL.GetBalance(authData.CustId);
                    if (tpCurrency.ExchangeRate != 0 && tpCurrency.ExchangeRate != 1)
                    {
                        balance /= tpCurrency.ExchangeRate;
                    }

                    inputs.Add("Balance", tools.Floor(balance, 2).ToString());
                    inputs.Add("Token", authData.Token);
                }

                var myUrl = $"{apiUrl}GotoGame";
                var tpInfo = new TpInfo
                {
                    TpId = tpId,
                    CustId = authData.CustId,
                    LangId = selLangBLL.GetLangId(),
                    AppType = tools.IsMobile() ? 2 : 1,
                    inputs = inputs
                };
                try
                {
                    var data = await courierAdapter.MyPostAsync<bool, TpInfo>(myUrl, tpInfo);
                    if (data.ErrorCode == 0)
                    {
                        string entryUrl = data.Message;
                        if (tpId == 9 && environment.IsDevelopment())
                        {
                            return Content(entryUrl);
                        }
                        else
                        {
                            return Redirect(entryUrl);
                        }
                    }
                    else if (tpId == 1 && data.RawErrorCode == "10")
                    {
                        string entryUrl = thirdPartyBLL.getEntryUrl(tpId, inputs);
                        return Redirect(entryUrl);
                    }
                    else
                    {
                        return Content(data.RawMessage);
                    }
                }
                catch (Exception ex)
                {
                    return Content(ex.Message);
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