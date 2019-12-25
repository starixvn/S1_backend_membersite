using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MemberSite.Service.BLL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace MemberSite.Controllers
{
    [Authorize]
    public class CashierController : Controller
    {
        private readonly CourierAdapter courierAdapter;
        private readonly string payApiUrl;
        private readonly Tools tools;
        private readonly AuthorizeBLL authorizeBLL;
        private readonly PaymentBLL paymentBLL;

        public CashierController(IConfiguration configuration, CourierAdapter courierAdapter, Tools tools, AuthorizeBLL authorizeBLL, PaymentBLL paymentBLL)
        {
            this.courierAdapter = courierAdapter;
            payApiUrl = configuration.GetValue<string>("PayService");
            this.tools = tools;
            this.authorizeBLL = authorizeBLL;
            this.paymentBLL = paymentBLL;
        }
        public IActionResult Deposit()
        {
            return View();
        }
        public IActionResult Withdrawal()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> DepositReqOnlineBankingAsync(int ChannelId, decimal Amount)
        {
            var authData = authorizeBLL.GetAuthData();
            string transId = null;
            var result = paymentBLL.DepositReqOnlineBanking(authData.CustId, ChannelId, Amount, out transId);
            if (result == 0)
            {
                var reqDepositBank = new ReqDepositBank
                {
                    TransId = transId,
                    CustId = authData.CustId,
                    Amount = Amount,
                    ClientIP = tools.GetClientIP(),
                    ReturnUrl = Url.RouteUrl("desktop", new { action = "Deposit", controller = "Cashier", authData.LangId })
                };

                var myUrl = $"{payApiUrl}ReqDepositBank";
                var apiResult = await courierAdapter.MyPostAsync<PayResult, ReqDepositBank>(myUrl, reqDepositBank);
                return View(apiResult);
            }

            return View();
        }

        public class ReqDeposit
        {
            public string TransId { get; set; }
            public int PayId { get; set; }
            public int CustId { get; set; }
            public decimal Amount { get; set; }
            public string ClientIP { set; get; }
            public string ReturnUrl { set; get; }
        }

        public class ReqDepositBank : ReqDeposit
        {
            public int SysId { get; set; }
        }
        public class ReqDepositMobile : ReqDeposit
        {
            public int ChannelId { get; set; }
        }
    }
}