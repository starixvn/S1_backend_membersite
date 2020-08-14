using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MemberSite.Service.BLL;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Org.BouncyCastle.Security;

namespace MemberSite.Controllers
{
    public class PromotionController : Controller
    {
        private readonly SelLangBLL selLangBLL;
        private TemplateBLL templateBLL;
        private readonly PromotionBLL promotionBLL;
        private readonly ServerInfoBLL serverInfoBLL;
        private readonly CustInfoBLL custInfoBLL;
        private readonly AuthorizeBLL authorizeBLL;

        public PromotionController(SelLangBLL selLangBLL, TemplateBLL templateBLL, PromotionBLL promotionBLL, ServerInfoBLL serverInfoBLL, CustInfoBLL custInfoBLL, AuthorizeBLL authorizeBLL)
        {
            this.selLangBLL = selLangBLL;
            this.templateBLL = templateBLL;
            this.serverInfoBLL = serverInfoBLL;
            this.promotionBLL = promotionBLL;
            this.custInfoBLL = custInfoBLL;
            this.authorizeBLL = authorizeBLL;
        }
        public IActionResult Index()
        {
            return View(templateBLL.GetTemplatePath(Url));
        }

        [HttpGet]
        public IActionResult Bonus(string BonusCode)
        {
            //return View($"./{BonusCode}/{selLangBLL.GetLangId()}");

            var serverInfo = serverInfoBLL.GetServerInfo();
            int currencyId = 0;
            if (User.Identity.IsAuthenticated)
            {
                var authData = authorizeBLL.GetAuthData();
                var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);
                currencyId = custInfo.CurrencyId;
            }
            else
            {
                currencyId = selLangBLL.GetPreferCurrencyId();
            }

            var promotion = promotionBLL.GetPromotion(serverInfo.SiteId, BonusCode, currencyId);
            var langId = selLangBLL.GetLangId();
            if (promotion == null)
            {
                return View($"./{BonusCode}/{langId}");
            }
            else
            {
                if (promotion.Details != null)
                {
                    var details = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(promotion.Details);
                    var defLang = serverInfo.DefLang;
                    Dictionary<string, string> settings = null;
                    if (details.TryGetValue(langId, out settings) || details.TryGetValue(defLang, out settings))
                    {

                    }
                    else
                    {
                        foreach (var item in details)
                        {
                            settings = item.Value;
                            break;
                        }
                    }

                    settings.TryGetValue("title", out var title);
                    settings.TryGetValue("bannerUrl", out var bannerUrl);
                    settings.TryGetValue("description", out var description);
                    settings.TryGetValue("detail", out var detail);
                    ViewData["promo_title"] = title;
                    ViewData["detail"] = detail;
                    ViewData["bannerUrl"] = bannerUrl;
                    ViewData["description"] = description;
                }

                ViewData["BonusCode"] = promotion.BonusCode;
                ViewData["PeriodTo"] = promotion.PeriodTo;
                ViewData["Currency"] = currencyId;
                ViewData["Bonus"] = $"{promotion.Bonus.ToString("#,##0")}{((int)promotion.BonusType == 1 ? "%" : "")}";
                ViewData["MaxBonus"] = promotion.MaxBonus.ToString("#,##0");
                ViewData["RollOver"] = promotion.RollOver;
                ViewData["TpId"] = promotion.TpId;

                if (promotion.BonusInfoStatus != 1)
                {
                    ViewData["ExpiryTime"] = promotion.PeriodTo;
                }
                else
                {
                    ViewData["ExpiryTime"] = new DateTimeOffset(promotion.ExpiryTime.ToUniversalTime(), TimeSpan.Zero).ToUnixTimeSeconds();
                }

                return View(templateBLL.GetTemplatePath(Url));
            }
        }
    }
}