using System;
using System.Reflection;
using System.Threading.Tasks;
using MemberSite.Service.BLL;
using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace MemberSite.Controllers
{
    public class TestController : ControllerBase
    {
        private readonly ServerInfoService serverInfoService;
        private readonly AgentInfoService agentInfoService;
        private readonly CustInfoService custInfoService;
        private readonly CustInfoBll custInfoBLL;
        private readonly AuthenticateService userLoginLogService;
        private readonly Tools tools;
        private readonly IWebHostEnvironment environment;
        private readonly DepositService _depositService;
        private readonly BankInfoOffService bankInfoService;
        private readonly WalletService walletService;
        private readonly WalletBLL walletBLL;
        private readonly SelLangBLL selLangBLL;
        private readonly MessageService messageService;
        private readonly PromotionBll promotionBLL;

        public TestController(PromotionBll promotionBLL, WalletBLL walletBLL, WalletService walletService, IWebHostEnvironment environment, ServerInfoService serverInfoService, AgentInfoService agentInfoService, CustInfoService custInfoService, CustInfoBll custInfoBLL, AuthenticateService userLoginLogService, Tools tools, DepositService depositService, BankInfoOffService bankInfoService, SelLangBLL selLangBLL, MessageService messageService)
        {
            this.serverInfoService = serverInfoService;
            this.agentInfoService = agentInfoService;
            this.custInfoService = custInfoService;
            this.custInfoBLL = custInfoBLL;
            this.userLoginLogService = userLoginLogService;
            this.tools = tools;
            this.environment = environment;
            this._depositService = depositService;
            this.bankInfoService = bankInfoService;
            this.walletService = walletService;
            this.walletBLL = walletBLL;
            this.selLangBLL = selLangBLL;
            this.messageService = messageService;
            this.promotionBLL = promotionBLL;
        }

        [HttpGet]
        [Route("API/test")]
        public async Task<dynamic> testAsync()
        {
            
            //await custInfoBLL.ForgetPwdAsync(8);
            var a = 1;
            var b = 0;
            //var cc = messageService.GetMsgList(3, 1, ref a, out b);

            return custInfoBLL.GetCustRollOver(18, "FTDP");
            var asm = Assembly.GetCallingAssembly();
            var rsFileName = asm.GetName().Name;
            //return System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
            //System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en");
            /*System.Globalization.CultureInfo.DefaultThreadCurrentCulture = new System.Globalization.CultureInfo("en");
            System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = new System.Globalization.CultureInfo("en");
            return System.Globalization.CultureInfo.CurrentCulture.DisplayName;*/
            //System.Globalization.CultureInfo.GetCultureInfo
            var currencyId = 51;
            var serverInfo = serverInfoService.GetModel(1);

            var customer = new Customer
            {
                UserName = "scott010812",
                CurrencyId = currencyId,
                Userpwd = "scott010812",
                SiteId = serverInfo.SiteId,
                UserLang = "en"
            };
            var token = "";
            //var a = custInfoBLL.UserLogin(customer.UserName, customer.SiteId, customer.Userpwd, tools.GetClientIP(this.HttpContext), tools.GetFromUrl(this.HttpContext), Request.Headers["User-Agent"], ref customer, out token);
            var depositDetailInfo = new DepositDetailInfo
            {
                DepositType = 2,
                CustId = customer.CustId,
                SiteId = customer.SiteId,
                UserName = customer.UserName,
                Amount = -500
            };
            //depositDetailInfo.DispositInfo = Newtonsoft.Json.JsonConvert.SerializeObject(bankInfoService.GetBankAccountOff(1, customer.CustId, depositDetailInfo.Amount));
            //@DispositType,@CustId,@UserName,@SiteId,@DispositInfo,@Amount,@Memo,@PostScript,@Status
            var TT = "";
            var agent = agentInfoService.GetSuperAgent(customer.CurrencyId, customer.SiteId);
            var custid = 0;
            //custInfoBLL.UserLogin(customer.UserName, customer.SiteId, customer.Userpwd, tools.GetClientIP(this.HttpContext), tools.GetFromUrl(this.HttpContext), Request.Headers["User-Agent"], ref customer, out token);
            //walletService.SetBalance(customer.CustId, customer.SiteId, customer.UserName, depositDetailInfo.Amount, 1, null, null, false);
            //custInfoBLL.AddCustInfo(customer, agent, serverInfo, out custid);
            //depositService.DepositRequest(depositDetailInfo, out TT);
            //userLoginLogService.UpdateOnline(customer.CustId, true, "SSS", "SSSS");
            ////
            //return Newtonsoft.Json.JsonConvert.SerializeObject(selLangBLL.GetLangId());
        }
    }
    public abstract class BaseController : Controller
    {
        public override ViewResult View(string viewName, object model)
        {
            if (!IsDevelopment())
            {
                var action = ControllerContext.RouteData.Values["action"].ToString();

                if (string.IsNullOrEmpty(viewName))
                {
                    viewName = $"{action}.min";
                }
                else if (viewName == action)
                {
                    viewName += ".min";
                }
            }

            return base.View(viewName, model);
        }

        private bool IsDevelopment()
        {
            return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == EnvironmentName.Development;
        }
    }
}