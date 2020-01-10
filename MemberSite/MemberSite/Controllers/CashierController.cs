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
        public async Task<IActionResult> DepositReqOnlineBankingAsync(int sysId, int payId, int ChannelId, decimal Amount)
        {
            var authData = authorizeBLL.GetAuthData();
            string transId = null;
            BankInfoPay onlineBank = null;
            var result = paymentBLL.DepositReqOnlineBanking(authData.CustId, sysId, payId, ChannelId, Amount, out transId, out onlineBank);
            var apiResult = new ApiResult<PayResult>();
            if (result == 0)
            {
                var reqDepositBank = new ReqDepositBank
                {
                    TransId = transId,
                    CustId = authData.CustId,
                    Amount = Amount,
                    PayId = onlineBank.PayId,
                    SysId = onlineBank.SysId,
                    ClientIP = tools.GetClientIP(),
                    ReturnUrl = Url.RouteUrl("desktop", new { action = "Deposit", controller = "Cashier", authData.LangId }, protocol: Request.Scheme, host: Request.Host.Port == 80 ? Request.Host.Host : $"{Request.Host.Host}:{Request.Host.Port}")
                };

                var myUrl = $"{payApiUrl}ReqDepositBank";
                apiResult = await courierAdapter.MyPostAsync<PayResult, ReqDepositBank>(myUrl, reqDepositBank);
                if (apiResult.ErrorCode == 0 && apiResult.Data.Mode == 2)
                {
                    return Redirect(apiResult.Data.Url);
                }
            }
            else
            {
                apiResult.ErrorCode = result;
                apiResult.Message = "Error.";
            }

            return View(apiResult);
        }

        [Route("[controller]/DepositReceip.jpg")]
        [HttpGet]
        public IActionResult GetDepositReceiptPic(string transId)
        {
            var authData = authorizeBLL.GetAuthData();
            var depositReceipt = paymentBLL.GetDepositReceipt(transId, authData.CustId);
            if (depositReceipt == null)
            {
                return StatusCode(404);
            }
            var image = depositReceipt.DepositPic;
            return File(image, "image/jpeg");
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