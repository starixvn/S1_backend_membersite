using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using Dapper;
using MemberSite.Service.BLL;
using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace MemberSite.Controllers
{
    public class TestController : ControllerBase
    {
        private readonly ServerInfoService serverInfoService;
        private readonly AgentInfoService agentInfoService;
        private readonly CustInfoService custInfoService;
        private readonly CustInfoBLL custInfoBLL;
        private readonly AuthenticateService userLoginLogService;
        private readonly Tools tools;
        private readonly IWebHostEnvironment environment;
        private readonly DepoistService depoistService;
        private readonly BankInfoOffService bankInfoService;
        private readonly IBCDBLibrary iBCDBLibrary;
        private readonly WalletService walletService;
        private readonly WalletBLL walletBLL;
        private readonly SelLangBLL selLangBLL;
        private readonly MessageService messageService;

        public TestController(WalletBLL walletBLL, WalletService walletService, IBCDBLibrary iBCDBLibrary, IWebHostEnvironment environment, ServerInfoService serverInfoService, AgentInfoService agentInfoService, CustInfoService custInfoService, CustInfoBLL custInfoBLL, AuthenticateService userLoginLogService, Tools tools, DepoistService depoistService, BankInfoOffService bankInfoService, SelLangBLL selLangBLL, MessageService messageService)
        {
            this.serverInfoService = serverInfoService;
            this.agentInfoService = agentInfoService;
            this.custInfoService = custInfoService;
            this.custInfoBLL = custInfoBLL;
            this.userLoginLogService = userLoginLogService;
            this.tools = tools;
            this.environment = environment;
            this.depoistService = depoistService;
            this.bankInfoService = bankInfoService;
            this.iBCDBLibrary = iBCDBLibrary;
            this.walletService = walletService;
            this.walletBLL = walletBLL;
            this.selLangBLL = selLangBLL;
            this.messageService = messageService;
        }

        [HttpGet]
        [Route("API/test")]
        public IEnumerable<Message> test()
        {

            int a = 1;
            int b = 0;
            var cc = messageService.GetMsgList(3, 1, ref a, out b);
            return cc;
            Assembly asm = Assembly.GetCallingAssembly();
            string rsFileName = asm.GetName().Name;
            //return System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
            //System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en");
            /*System.Globalization.CultureInfo.DefaultThreadCurrentCulture = new System.Globalization.CultureInfo("en");
            System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = new System.Globalization.CultureInfo("en");
            return System.Globalization.CultureInfo.CurrentCulture.DisplayName;*/
            //System.Globalization.CultureInfo.GetCultureInfo
            int currencyId = 51;
            var serverInfo = serverInfoService.GetModel();

            Customer customer = new Customer();
            customer.UserName = "scott010812";
            customer.CurrencyId = currencyId;
            customer.Userpwd = "scott010812";
            customer.SiteId = serverInfo.SiteId;
            customer.UserLang = "en";
            string token = "";
            //var a = custInfoBLL.UserLogin(customer.UserName, customer.SiteId, customer.Userpwd, tools.GetClientIP(this.HttpContext), tools.GetFromUrl(this.HttpContext), Request.Headers["User-Agent"], ref customer, out token);
            DepositDetailInfo depositDetailInfo = new DepositDetailInfo();
            depositDetailInfo.DispositType = 2;
            depositDetailInfo.CustId = customer.CustId;
            depositDetailInfo.SiteId = customer.SiteId;
            depositDetailInfo.UserName = customer.UserName;
            depositDetailInfo.Amount = -500;
            //depositDetailInfo.DispositInfo = Newtonsoft.Json.JsonConvert.SerializeObject(bankInfoService.GetBankAccountOff(1, customer.CustId, depositDetailInfo.Amount));
            //@DispositType,@CustId,@UserName,@SiteId,@DispositInfo,@Amount,@Memo,@PostScript,@Status
            string TT = "";
            var agent = agentInfoService.GetSuperAgent(customer.CurrencyId, customer.SiteId);
            int custid = 0;
            //custInfoBLL.UserLogin(customer.UserName, customer.SiteId, customer.Userpwd, tools.GetClientIP(this.HttpContext), tools.GetFromUrl(this.HttpContext), Request.Headers["User-Agent"], ref customer, out token);
            //walletService.SetBalance(customer.CustId, customer.SiteId, customer.UserName, depositDetailInfo.Amount, 1, null, null, false);
            //custInfoBLL.AddCustInfo(customer, agent, serverInfo, out custid);
            //depoistService.DepositRequest(depositDetailInfo, out TT);
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
                string action = ControllerContext.RouteData.Values["action"].ToString();

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