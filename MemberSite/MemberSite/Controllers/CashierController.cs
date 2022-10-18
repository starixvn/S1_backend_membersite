using MemberSite.Service.BLL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static MemberSite.Controllers.entPageController;

namespace MemberSite.Controllers
{
    [Authorize]
    public class CashierController : Controller
    {
        private readonly CourierAdapter courierAdapter;
        private readonly string payApiUrl;
        private readonly Tools tools;
        private readonly AuthorizeBLL authorizeBLL;
        private readonly ServerInfoBll serverInfoBLL;
        private readonly PaymentBLL paymentBLL;
        private readonly CustInfoBll custInfoBLL;
        private readonly TemplateBLL templateBLL;
        private readonly CryptoPayInfoBll cryptoPayInfoBLL;
        private readonly LoadCardPayInfoBLL loadCardPayInfoBLL;
        private readonly PromotionBll promotionBLL;
        private readonly PayInfoBLL _payInfoBLL;
        private readonly BankInfoBLL _bankInfoBLL;
        private readonly SelLangBLL _selLangBLL;

        public CashierController(IServiceProvider provider)
        {
            courierAdapter = provider.GetRequiredService<CourierAdapter>();
            tools = provider.GetRequiredService<Tools>();
            authorizeBLL = provider.GetRequiredService<AuthorizeBLL>();
            serverInfoBLL = provider.GetRequiredService<ServerInfoBll>();
            paymentBLL = provider.GetRequiredService<PaymentBLL>();
            custInfoBLL = provider.GetRequiredService<CustInfoBll>();
            templateBLL = provider.GetRequiredService<TemplateBLL>();
            cryptoPayInfoBLL = provider.GetRequiredService<CryptoPayInfoBll>();
            loadCardPayInfoBLL = provider.GetRequiredService<LoadCardPayInfoBLL>();
            promotionBLL = provider.GetRequiredService<PromotionBll>();
            _payInfoBLL = provider.GetRequiredService<PayInfoBLL>();
            _bankInfoBLL = provider.GetRequiredService<BankInfoBLL>();
            _selLangBLL = provider.GetRequiredService<SelLangBLL>();

            var configuration = provider.GetRequiredService<IConfiguration>();
            payApiUrl = configuration.GetValue<string>("PayService");
        }
        public IActionResult Deposit()
        {
            var authData = authorizeBLL.GetAuthData();
            var custInfo = custInfoBLL.GetCustInfo(authData.CustId);

            if (!custInfo.IsTest)
                return View(templateBLL.GetTemplatePath(Url));
            else
                return BadRequest("This account does not allow deposit. Please contact CSD for more information!");
        }
        public IActionResult Withdrawal()
        {
            var authData = authorizeBLL.GetAuthData();
            var custInfo = custInfoBLL.GetCustInfo(authData.CustId);

            if (!custInfo.IsTest)
                return View(templateBLL.GetTemplatePath(Url));
            else
                return BadRequest("This account does not allow withdrawal. Please contact CSD for more information!");
        }
        public IActionResult Transfer()
        {
            return View(templateBLL.GetTemplatePath(Url));
        }

        [HttpGet]
        public async Task<IActionResult> DepositReqOnlineBankingAsync(int sysId, int payId, int ChannelId, decimal Amount, string BonusCode)
        {
            var serverInfo = serverInfoBLL.GetServerInfo();
            var authData = authorizeBLL.GetAuthData();
            var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);
            string transId = null;
            BankInfoPay onlineBank = null;
            var model = new ErrorData();
            var result = paymentBLL.DepositReqOnlineBanking(authData.CustId, sysId, payId, ChannelId, Amount, out transId, out onlineBank);

            var apiResult = new ApiResult<PayResult>();
            if (result == 0)
            {
                if (!string.IsNullOrEmpty(BonusCode))
                    promotionBLL.SetDepositPromotionQueue(transId, BonusCode, authData.CustId, custInfo.SiteId, custInfo.UserName, custInfo.CurrencyId, 1, 0);

                var reqDepositBank = new ReqDepositBank
                {
                    TransId = transId,
                    CustId = authData.CustId,
                    Amount = Amount,
                    PayId = onlineBank.PayId,
                    SysId = onlineBank.SysId,
                    ClientIP = tools.GetClientIP(),
                    ReturnUrl = Url.RouteUrl("desktop", new { action = "Deposit", controller = "Cashier", custInfo.UserLang }, protocol: Request.Scheme, host: Request.Host.Port == 80 ? Request.Host.Host : $"{Request.Host.Host}:{Request.Host.Port}")
                };

                var myUrl = $"{payApiUrl}ReqDepositBank";
                apiResult = await courierAdapter.MyPostAsync<PayResult, ReqDepositBank>(myUrl, reqDepositBank);
                if (apiResult.ErrorCode == 0 && apiResult.Data.Mode == 1)
                {
                    if (string.IsNullOrEmpty(apiResult.Data.Message))
                    {
                        return View(apiResult);
                    }
                    else
                    {
                        return Content(apiResult.Data.Message, "text/html");
                    }
                }
                else if (apiResult.ErrorCode == 0 && apiResult.Data.Mode == 2)
                {
                    return Redirect(apiResult.Data.Url);
                }
                else if (apiResult.ErrorCode == 0 && apiResult.Data.Mode == 3)
                {
                    return Json(apiResult);
                }
            }
            else
            {
                apiResult.ErrorCode = result;
                apiResult.Message = "Error.";
                model.Message = "DepositReqOnlineBanking Error";
            }

            return View($"~/Views/entPage/{serverInfo.SiteName}PaymentErrorPage.cshtml", model);
        }
        public async Task<IActionResult> DepositReqMobilePayAsync(int ChannelId, decimal Amount, string BonusCode, string PayAccount)
        {
            var serverInfo = serverInfoBLL.GetServerInfo();
            var apiResult = new ApiResult<PayResult>();
            var authData = authorizeBLL.GetAuthData();
            var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);

            var model = new ErrorData();
            var result = _payInfoBLL.DepositReqPay(authData.CustId, ChannelId, Amount, out var transId, out var payInfo, DepositType.MobilePayment);
            if (result == 0)
            {
                if (!string.IsNullOrEmpty(BonusCode))
                    promotionBLL.SetDepositPromotionQueue(transId, BonusCode, authData.CustId, custInfo.SiteId, custInfo.UserName, custInfo.CurrencyId, 1, 0);
                var reqDepositMobile = new ReqDepositMobile
                {
                    TransId = transId,
                    CustId = authData.CustId,
                    Amount = Amount,
                    ChannelId = payInfo.ChannelId,
                    PayId = payInfo.PayId,
                    ClientIP = tools.GetClientIP(),
                    ReturnUrl = Url.RouteUrl("desktop", new { action = "Deposit", controller = "Cashier", custInfo.UserLang }, protocol: Request.Scheme, host: Request.Host.Port == 80 ? Request.Host.Host : $"{Request.Host.Host}:{Request.Host.Port}"),
                    PayAccount = PayAccount
                };

                var myUrl = $"{payApiUrl}ReqDepositMobile";
                apiResult = await courierAdapter.MyPostAsync<PayResult, ReqDepositMobile>(myUrl, reqDepositMobile);
                if (apiResult.ErrorCode == 0 && apiResult.Data.Mode == 1)
                {
                    return View(apiResult);
                }
                else if (apiResult.ErrorCode == 0 && apiResult.Data.Mode == 2)
                {
                    return Redirect(apiResult.Data.Url);
                }
                else if (apiResult.ErrorCode == 0 && apiResult.Data.Mode == 3)
                {
                    return Json(apiResult);
                }
                else
                {
                    model.Message = apiResult.RawMessage;
                }
            }
            else
            {
                apiResult.ErrorCode = result;
                apiResult.Message = "Error.";
                model.Message = "DepositReqMobilePay Error";
            }

            return View($"~/Views/entPage/{serverInfo.SiteName}PaymentErrorPage.cshtml", model);
        }

        public class ReqDepositCrypto : ReqDeposit
        {
            public string DepositInfo { get; set; }
            public int ChannelId { get; set; }
        }
        public async Task<IActionResult> DepositReqCryptoPayAsync(int ChannelId, decimal Amount, string BonusCode)
        {
            var serverInfo = serverInfoBLL.GetServerInfo();
            var apiResult = new ApiResult<PayResult>();
            var authData = authorizeBLL.GetAuthData();
            var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);
            string transId = null;
            CryptoPayInfo cryptoPayInfo = null;
            var model = new ErrorData();
            var result = cryptoPayInfoBLL.DepositReqCryptoPay(authData.CustId, ChannelId, Amount, out transId, out cryptoPayInfo);
            if (result == 0)
            {
                if (!string.IsNullOrEmpty(BonusCode))
                    promotionBLL.SetDepositPromotionQueue(transId, BonusCode, authData.CustId, custInfo.SiteId, custInfo.UserName, custInfo.CurrencyId, 1, 0);
                var reqDepositCrypto = new ReqDepositCrypto
                {
                    TransId = transId,
                    CustId = authData.CustId,
                    Amount = Amount,
                    ChannelId = cryptoPayInfo.ChannelId,
                    PayId = cryptoPayInfo.PayId,
                    ClientIP = tools.GetClientIP(),
                    DepositInfo = Newtonsoft.Json.JsonConvert.SerializeObject(cryptoPayInfo),
                    ReturnUrl = Url.RouteUrl("desktop", new { action = "Deposit", controller = "Cashier", custInfo.UserLang }, protocol: Request.Scheme, host: Request.Host.Port == 80 ? Request.Host.Host : $"{Request.Host.Host}:{Request.Host.Port}")
                };

                var myUrl = $"{payApiUrl}ReqDepositCrpyto";
                apiResult = await courierAdapter.MyPostAsync<PayResult, ReqDepositCrypto>(myUrl, reqDepositCrypto);
                if (apiResult.ErrorCode == 0 && apiResult.Data.Mode == 1)
                {
                    return View(apiResult);
                }
                else if (apiResult.ErrorCode == 0 && apiResult.Data.Mode == 2)
                {
                    return Redirect(apiResult.Data.Url);
                }
                else if (apiResult.ErrorCode == 0 && apiResult.Data.Mode == 3)
                {
                    return Json(apiResult);
                }
                else
                {
                    model.Message = apiResult.RawMessage;
                }
            }
            else
            {
                apiResult.ErrorCode = result;
                apiResult.Message = "Error.";
                model.Message = "DepositReqCryptoPay Error";
            }

            return View($"~/Views/entPage/{serverInfo.SiteName}PaymentErrorPage.cshtml", model);
        }

        public async Task<IActionResult> DepositReqLoadCardPayAsync(int ChannelId, string Code, string Serial, decimal Amount, string BonusCode)
        {
            var serverInfo = serverInfoBLL.GetServerInfo();
            var apiResult = new ApiResult<PayResult>();
            var authData = authorizeBLL.GetAuthData();
            var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);
            string transId = null;
            LoadCardPayInfo loadCardPayInfo = null;
            var model = new ErrorData();
            var result = loadCardPayInfoBLL.DepositReqLoadCardPay(authData.CustId, ChannelId, Code, Serial, Amount, out transId, out loadCardPayInfo);
            if (result == 0)
            {
                if (!string.IsNullOrEmpty(BonusCode))
                    promotionBLL.SetDepositPromotionQueue(transId, BonusCode, authData.CustId, custInfo.SiteId, custInfo.UserName, custInfo.CurrencyId, 1, 0);
                var reqDeposit = new
                {
                    TransId = transId,
                    authData.CustId,
                    Amount,
                    Code,
                    Serial,
                    loadCardPayInfo.ChannelId,
                    loadCardPayInfo.PayId,
                    ClientIP = tools.GetClientIP(),
                    ReturnUrl = Url.RouteUrl("desktop", new { action = "Deposit", controller = "Cashier", custInfo.UserLang }, protocol: Request.Scheme, host: Request.Host.Port == 80 ? Request.Host.Host : $"{Request.Host.Host}:{Request.Host.Port}"),
                };

                var myUrl = $"{payApiUrl}ReqDepositLoadCard";
                apiResult = await courierAdapter.MyPostAsync<PayResult, dynamic>(myUrl, reqDeposit);

                if (apiResult.ErrorCode == 0 && apiResult.Data.Mode == 1)
                {
                    return View(apiResult);
                }
                else if (apiResult.ErrorCode == 0 && apiResult.Data.Mode == 2)
                {
                    return Redirect(apiResult.Data.Url);
                }
                else if (apiResult.ErrorCode == 0 && apiResult.Data.Mode == 3)
                {
                    return Json(apiResult);
                }
                else
                {
                    model.Message = apiResult.RawMessage;
                }
            }
            else
            {
                apiResult.ErrorCode = result;
                apiResult.Message = "Error.";
                model.Message = "DepositReqLoadCardPay Error";
            }

            return View($"~/Views/entPage/{serverInfo.SiteName}PaymentErrorPage.cshtml", model);
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
            public string PayAccount { get; set; }
        }

        [Route("{LangId}/[controller]/DepositReceip.jpg")]
        [HttpGet]
        public IActionResult GetDepositReceiptPicV2(string transId)
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

        public async Task<IActionResult> DepositReqQRPayAsync(int ChannelId, decimal Amount, string BonusCode)
        {
            var serverInfo = serverInfoBLL.GetServerInfo();
            var authData = authorizeBLL.GetAuthData();
            var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);

            var result = _payInfoBLL.DepositReqPay(authData.CustId, ChannelId, Amount, out var transId, out var payInfo, DepositType.QRPayPayment);
            if (result != 0)
            {
                return View($"~/Views/entPage/{serverInfo.SiteName}PaymentErrorPage.cshtml", new ErrorData
                {
                    Message = "DepositReqQRPayAsync Error"
                });
            }

            if (!string.IsNullOrEmpty(BonusCode))
            {
                promotionBLL.SetDepositPromotionQueue(
                    transId,
                    BonusCode,
                    authData.CustId,
                    custInfo.SiteId,
                    custInfo.UserName,
                    custInfo.CurrencyId,
                    1,
                    0);
            }

            var apiResult = await courierAdapter.MyPostAsync<PayResult, dynamic>($"{payApiUrl}ReqDepositQR", new
            {
                TransId = transId,
                authData.CustId,
                Amount,
                payInfo.ChannelId,
                payInfo.PayId,
                ClientIP = tools.GetClientIP(),
                ReturnUrl = Url.RouteUrl("desktop", new { action = "Deposit", controller = "Cashier", custInfo.UserLang }, protocol: Request.Scheme, host: Request.Host.Port == 80 ? Request.Host.Host : $"{Request.Host.Host}:{Request.Host.Port}")
            });

            if (apiResult.ErrorCode == 0 && apiResult.Data.Mode == 1)
            {
                return View("DepositReqMobilePay", apiResult);
            }
            else if (apiResult.ErrorCode == 0 && apiResult.Data.Mode == 2)
            {
                return Redirect(apiResult.Data.Url);
            }
            else if (apiResult.ErrorCode == 0 && apiResult.Data.Mode == 3)
            {
                return Json(apiResult);
            }
            else
            {
                return View($"~/Views/entPage/{serverInfo.SiteName}PaymentErrorPage.cshtml", new ErrorData
                {
                    Message = apiResult.RawMessage
                });
            }
        }


        public async Task<IActionResult> DepositReqBankAutoChannelAsync(int sysId, decimal Amount, string BonusCode)
        {
            var serverInfo = serverInfoBLL.GetServerInfo();
            var authData = authorizeBLL.GetAuthData();
            var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);
            var vipLevel = custInfoBLL.GetPointLevelWithCache(authData.CustId);
            var depositType = _bankInfoBLL.GetAutoChannelDepositType(custInfo.SiteId);
            var url = depositType == DepositType.BankAutoChannel ? $"{payApiUrl}BankAutoChannel" : $"{payApiUrl}ReqDepositBank";

            //取得可用銀行列表
            var bankList = _bankInfoBLL.GetAutoChannelBankList(custInfo, vipLevel)
                .Where(i => i.SysId == sysId && Amount <= i.MaxAmount && Amount >= i.MinAmount);

            foreach (var bank in bankList.OrderBy(i => i.SSort))
            {
                var result = paymentBLL.DepositReqOnlineBanking(authData.CustId, sysId, bank.PayId, bank.ChannelId, Amount, out var transId, out var onlineBank, depositType);
                if (result != 0)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(BonusCode))
                {
                    promotionBLL.SetDepositPromotionQueue(transId, BonusCode, authData.CustId, custInfo.SiteId, custInfo.UserName, custInfo.CurrencyId, 1, 0);
                }

                var apiResult = await courierAdapter.MyPostAsync<PayResult, ReqDepositBank>(url, new ReqDepositBank
                {
                    TransId = transId,
                    CustId = authData.CustId,
                    Amount = Amount,
                    PayId = onlineBank.PayId,
                    SysId = onlineBank.SysId,
                    ClientIP = tools.GetClientIP(),
                    ReturnUrl = Url.RouteUrl("desktop", new { action = "Deposit", controller = "Cashier", custInfo.UserLang }, protocol: Request.Scheme, host: Request.Host.Port == 80 ? Request.Host.Host : $"{Request.Host.Host}:{Request.Host.Port}")
                });
                if (apiResult.ErrorCode == 0 && apiResult.Data.Mode == 1)
                {
                    if (string.IsNullOrEmpty(apiResult.Data.Message))
                    {
                        return View("DepositReqOnlineBanking", apiResult);
                    }
                    else
                    {
                        return Content(apiResult.Data.Message, "text/html");
                    }
                }
                else if (apiResult.ErrorCode == 0 && apiResult.Data.Mode == 2)
                {
                    return Redirect(apiResult.Data.Url);
                }
                else if (apiResult.ErrorCode == 0 && apiResult.Data.Mode == 3)
                {
                    return Json(apiResult);
                }
            }

            return View($"~/Views/entPage/{serverInfo.SiteName}PaymentErrorPage.cshtml", new ErrorData
            {
                Message = "All Channel fail."
            });

        }
    }
}