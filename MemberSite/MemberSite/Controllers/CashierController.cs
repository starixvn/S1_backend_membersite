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
        private readonly CourierAdapter _courierAdapter;
        private readonly string _payApiUrl;
        private readonly Tools _tools;
        private readonly AuthorizeBLL _authorizeBll;
        private readonly ServerInfoBll _serverInfoBll;
        private readonly PaymentBLL _paymentBll;
        private readonly CustInfoBll _custInfoBll;
        private readonly TemplateBLL _templateBll;
        private readonly CryptoPayInfoBll _cryptoPayInfoBll;
        private readonly LoadCardPayInfoBLL _loadCardPayInfoBll;
        private readonly PromotionBll _promotionBll;
        private readonly PayInfoBLL _payInfoBll;
        private readonly BankInfoBLL _bankInfoBll;
        private readonly SelLangBLL _selLangBLL;

        public CashierController(IServiceProvider provider)
        {
            _courierAdapter = provider.GetRequiredService<CourierAdapter>();
            _tools = provider.GetRequiredService<Tools>();
            _authorizeBll = provider.GetRequiredService<AuthorizeBLL>();
            _serverInfoBll = provider.GetRequiredService<ServerInfoBll>();
            _paymentBll = provider.GetRequiredService<PaymentBLL>();
            _custInfoBll = provider.GetRequiredService<CustInfoBll>();
            _templateBll = provider.GetRequiredService<TemplateBLL>();
            _cryptoPayInfoBll = provider.GetRequiredService<CryptoPayInfoBll>();
            _loadCardPayInfoBll = provider.GetRequiredService<LoadCardPayInfoBLL>();
            _promotionBll = provider.GetRequiredService<PromotionBll>();
            _payInfoBll = provider.GetRequiredService<PayInfoBLL>();
            _bankInfoBll = provider.GetRequiredService<BankInfoBLL>();
            _selLangBLL = provider.GetRequiredService<SelLangBLL>();

            var configuration = provider.GetRequiredService<IConfiguration>();
            _payApiUrl = configuration.GetValue<string>("PayService");
        }
        public IActionResult Deposit()
        {
            var authData = _authorizeBll.GetAuthData();
            var custInfo = _custInfoBll.GetCustInfo(authData.CustId);

            if (!custInfo.IsTest)
                return View(_templateBll.GetTemplatePath(Url));
            else
                return BadRequest("This account does not allow deposit. Please contact CSD for more information!");
        }
        public IActionResult Withdrawal()
        {
            var authData = _authorizeBll.GetAuthData();
            var custInfo = _custInfoBll.GetCustInfo(authData.CustId);

            if (!custInfo.IsTest)
                return View(_templateBll.GetTemplatePath(Url));
            else
                return BadRequest("This account does not allow withdrawal. Please contact CSD for more information!");
        }
        public IActionResult Transfer()
        {
            return View(_templateBll.GetTemplatePath(Url));
        }

        [HttpGet]
        public async Task<IActionResult> DepositReqOnlineBankingAsync(int sysId, int payId, int ChannelId, decimal Amount, string BonusCode)
        {
            var serverInfo = _serverInfoBll.GetServerInfo();
            var authData = _authorizeBll.GetAuthData();
            var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
            var model = new ErrorData();
            var result = _paymentBll.DepositReqOnlineBanking(authData.CustId, sysId, payId, ChannelId, Amount, out var transId, out var onlineBank);

            var apiResult = new ApiResult<PayResult>();
            if (result == 0)
            {
                if (!string.IsNullOrEmpty(BonusCode))
                    _promotionBll.SetDepositPromotionQueue(transId, BonusCode, authData.CustId, custInfo.SiteId, custInfo.UserName, custInfo.CurrencyId, 1, 0);

                var reqDepositBank = new ReqDepositBank
                {
                    TransId = transId,
                    CustId = authData.CustId,
                    Amount = Amount,
                    PayId = onlineBank.PayId,
                    SysId = onlineBank.SysId,
                    ClientIP = _tools.GetClientIP(),
                    ReturnUrl = Url.RouteUrl("desktop", new { action = "Deposit", controller = "Cashier", custInfo.UserLang }, protocol: Request.Scheme, host: Request.Host.Port == 80 ? Request.Host.Host : $"{Request.Host.Host}:{Request.Host.Port}")
                };

                var myUrl = $"{_payApiUrl}ReqDepositBank";
                apiResult = await _courierAdapter.MyPostAsync<PayResult, ReqDepositBank>(myUrl, reqDepositBank);
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
                
                model.Message = apiResult.RawMessage;
            }
            else
            {
                apiResult.ErrorCode = result;
                apiResult.Message = "Error.";
            }

            return View($"~/Views/entPage/{serverInfo.SiteName}PaymentErrorPage.cshtml", model);
        }
        public async Task<IActionResult> DepositReqMobilePayAsync(int ChannelId, decimal Amount, string BonusCode, string PayAccount)
        {
            var serverInfo = _serverInfoBll.GetServerInfo();
            var apiResult = new ApiResult<PayResult>();
            var authData = _authorizeBll.GetAuthData();
            var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);

            var model = new ErrorData();
            var result = _payInfoBll.DepositReqPay(authData.CustId, ChannelId, Amount, out var transId, out var payInfo, DepositType.MobilePayment);
            if (result == 0)
            {
                if (!string.IsNullOrEmpty(BonusCode))
                    _promotionBll.SetDepositPromotionQueue(transId, BonusCode, authData.CustId, custInfo.SiteId, custInfo.UserName, custInfo.CurrencyId, 1, 0);
                var reqDepositMobile = new ReqDepositMobile
                {
                    TransId = transId,
                    CustId = authData.CustId,
                    Amount = Amount,
                    ChannelId = payInfo.ChannelId,
                    PayId = payInfo.PayId,
                    ClientIP = _tools.GetClientIP(),
                    ReturnUrl = Url.RouteUrl("desktop", new { action = "Deposit", controller = "Cashier", custInfo.UserLang }, protocol: Request.Scheme, host: Request.Host.Port == 80 ? Request.Host.Host : $"{Request.Host.Host}:{Request.Host.Port}"),
                    PayAccount = PayAccount
                };

                var myUrl = $"{_payApiUrl}ReqDepositMobile";
                apiResult = await _courierAdapter.MyPostAsync<PayResult, ReqDepositMobile>(myUrl, reqDepositMobile);
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
            var serverInfo = _serverInfoBll.GetServerInfo();
            var apiResult = new ApiResult<PayResult>();
            var authData = _authorizeBll.GetAuthData();
            var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
            var model = new ErrorData();
            var result = _cryptoPayInfoBll.DepositReqCryptoPay(authData.CustId, ChannelId, Amount, out var transId, out var cryptoPayInfo);
            if (result == 0)
            {
                if (!string.IsNullOrEmpty(BonusCode))
                    _promotionBll.SetDepositPromotionQueue(transId, BonusCode, authData.CustId, custInfo.SiteId, custInfo.UserName, custInfo.CurrencyId, 1, 0);
                var reqDepositCrypto = new ReqDepositCrypto
                {
                    TransId = transId,
                    CustId = authData.CustId,
                    Amount = Amount,
                    ChannelId = cryptoPayInfo.ChannelId,
                    PayId = cryptoPayInfo.PayId,
                    ClientIP = _tools.GetClientIP(),
                    DepositInfo = Newtonsoft.Json.JsonConvert.SerializeObject(cryptoPayInfo),
                    ReturnUrl = Url.RouteUrl("desktop", new { action = "Deposit", controller = "Cashier", custInfo.UserLang }, protocol: Request.Scheme, host: Request.Host.Port == 80 ? Request.Host.Host : $"{Request.Host.Host}:{Request.Host.Port}")
                };

                var myUrl = $"{_payApiUrl}ReqDepositCrpyto";
                apiResult = await _courierAdapter.MyPostAsync<PayResult, ReqDepositCrypto>(myUrl, reqDepositCrypto);
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
            }

            return View($"~/Views/entPage/{serverInfo.SiteName}PaymentErrorPage.cshtml", model);
        }

        public async Task<IActionResult> DepositReqLoadCardPayAsync(int ChannelId, string Code, string Serial, decimal Amount, string BonusCode)
        {
            var serverInfo = _serverInfoBll.GetServerInfo();
            var apiResult = new ApiResult<PayResult>();
            var authData = _authorizeBll.GetAuthData();
            var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
            var model = new ErrorData();
            var result = _loadCardPayInfoBll.DepositReqLoadCardPay(authData.CustId, ChannelId, Code, Serial, Amount, out var transId, out var loadCardPayInfo);
            if (result == 0)
            {
                if (!string.IsNullOrEmpty(BonusCode))
                    _promotionBll.SetDepositPromotionQueue(transId, BonusCode, authData.CustId, custInfo.SiteId, custInfo.UserName, custInfo.CurrencyId, 1, 0);
                var reqDeposit = new
                {
                    TransId = transId,
                    authData.CustId,
                    Amount,
                    Code,
                    Serial,
                    loadCardPayInfo.ChannelId,
                    loadCardPayInfo.PayId,
                    ClientIP = _tools.GetClientIP(),
                    ReturnUrl = Url.RouteUrl("desktop", new { action = "Deposit", controller = "Cashier", custInfo.UserLang }, protocol: Request.Scheme, host: Request.Host.Port == 80 ? Request.Host.Host : $"{Request.Host.Host}:{Request.Host.Port}")
                };

                var myUrl = $"{_payApiUrl}ReqDepositLoadCard";
                apiResult = await _courierAdapter.MyPostAsync<PayResult, dynamic>(myUrl, reqDeposit);
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
            }

            return View($"~/Views/entPage/{serverInfo.SiteName}PaymentErrorPage.cshtml", model);
        }

        [Route("[controller]/DepositReceip.jpg")]
        [HttpGet]
        public IActionResult GetDepositReceiptPic(string transId)
        {
            var authData = _authorizeBll.GetAuthData();
            var depositReceipt = _paymentBll.GetDepositReceipt(transId, authData.CustId);
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
            var authData = _authorizeBll.GetAuthData();
            var depositReceipt = _paymentBll.GetDepositReceipt(transId, authData.CustId);
            if (depositReceipt == null)
            {
                return StatusCode(404);
            }
            var image = depositReceipt.DepositPic;
            return File(image, "image/jpeg");
        }

        public async Task<IActionResult> DepositReqQRPayAsync(int ChannelId, decimal Amount, string BonusCode)
        {
            var serverInfo = _serverInfoBll.GetServerInfo();
            var authData = _authorizeBll.GetAuthData();
            var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
            var result = _payInfoBll.DepositReqPay(authData.CustId, ChannelId, Amount, out var transId, out var payInfo, DepositType.QRPayPayment);
            if (result != 0)
            {
                return View($"~/Views/entPage/{serverInfo.SiteName}PaymentErrorPage.cshtml", new ErrorData
                {
                    Message = string.Empty
                });
            }

            if (!string.IsNullOrEmpty(BonusCode))
            {
                _promotionBll.SetDepositPromotionQueue(
                    transId,
                    BonusCode,
                    authData.CustId,
                    custInfo.SiteId,
                    custInfo.UserName,
                    custInfo.CurrencyId,
                    1,
                    0);
            }

            var apiResult = await _courierAdapter.MyPostAsync<PayResult, dynamic>($"{_payApiUrl}ReqDepositQR", new
            {
                TransId = transId,
                authData.CustId,
                Amount,
                payInfo.ChannelId,
                payInfo.PayId,
                ClientIP = _tools.GetClientIP(),
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
            var serverInfo = _serverInfoBll.GetServerInfo();
            var authData = _authorizeBll.GetAuthData();
            var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
            var vipLevel = _custInfoBll.GetPointLevelWithCache(authData.CustId);
            var depositType = _bankInfoBll.GetAutoChannelDepositType(custInfo.SiteId);
            var url = depositType == DepositType.BankAutoChannel ? $"{_payApiUrl}BankAutoChannel" : $"{_payApiUrl}ReqDepositBank";
            
            //取得可用銀行列表
            var bankList = _bankInfoBll.GetAutoChannelBankList(custInfo, vipLevel)
                .Where(i => i.SysId == sysId && Amount <= i.MaxAmount && Amount >= i.MinAmount);
          
            foreach (var bank in bankList.OrderBy(i => i.SSort))
            {
                var result = _paymentBll.DepositReqOnlineBanking(authData.CustId, sysId, bank.PayId, bank.ChannelId, Amount, out var transId, out var onlineBank, depositType);
                if (result != 0)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(BonusCode))
                {
                    _promotionBll.SetDepositPromotionQueue(transId, BonusCode, authData.CustId, custInfo.SiteId, custInfo.UserName, custInfo.CurrencyId, 1, 0);
                }

                var apiResult = await _courierAdapter.MyPostAsync<PayResult, ReqDepositBank>(url, new ReqDepositBank
                {
                    TransId = transId,
                    CustId = authData.CustId,
                    Amount = Amount,
                    PayId = onlineBank.PayId,
                    SysId = onlineBank.SysId,
                    ClientIP = _tools.GetClientIP(),
                    ReturnUrl = Url.RouteUrl("desktop", new { action = "Deposit", controller = "Cashier", custInfo.UserLang }, protocol: Request.Scheme, host: Request.Host.Port == 80 ? Request.Host.Host : $"{Request.Host.Host}:{Request.Host.Port}")
                });
                if (apiResult.ErrorCode == 0 && apiResult.Data.Mode == 1)
                {
                    if (string.IsNullOrEmpty(apiResult.Data.Message))
                    {
                        return View("DepositReqOnlineBanking",apiResult);
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