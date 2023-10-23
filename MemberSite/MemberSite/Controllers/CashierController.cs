using MemberSite.Extensions;
using MemberSite.Service.BLL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
        private readonly CryptoPayInfoBll _cryptoPayInfoBll;
        private readonly LoadCardPayInfoBLL _loadCardPayInfoBll;
        private readonly PromotionBll _promotionBll;
        private readonly PayInfoBLL _payInfoBll;
        private readonly BankInfoBLL _bankInfoBll;
        private readonly BankInfoOffBLL _bankInfoOffBll;
        private readonly ILogger<CashierController> _logger;

        public CashierController(IServiceProvider provider)
        {
            _courierAdapter = provider.GetRequiredService<CourierAdapter>();
            _tools = provider.GetRequiredService<Tools>();
            _authorizeBll = provider.GetRequiredService<AuthorizeBLL>();
            _serverInfoBll = provider.GetRequiredService<ServerInfoBll>();
            _paymentBll = provider.GetRequiredService<PaymentBLL>();
            _custInfoBll = provider.GetRequiredService<CustInfoBll>();
            _cryptoPayInfoBll = provider.GetRequiredService<CryptoPayInfoBll>();
            _loadCardPayInfoBll = provider.GetRequiredService<LoadCardPayInfoBLL>();
            _promotionBll = provider.GetRequiredService<PromotionBll>();
            _payInfoBll = provider.GetRequiredService<PayInfoBLL>();
            _bankInfoBll = provider.GetRequiredService<BankInfoBLL>();
            _bankInfoOffBll = provider.GetRequiredService<BankInfoOffBLL>();
            _logger = provider.GetRequiredService<ILogger<CashierController>>();
            var configuration = provider.GetRequiredService<IConfiguration>();
            _payApiUrl = configuration.GetValue<string>("PayService");
        }


        [HttpGet]
        public async Task<IActionResult> DepositReqOnlineBankingAsync(int sysId, int payId, int ChannelId, decimal Amount, string BonusCode)
        {
            var model = new ErrorData();
            var authData = _authorizeBll.GetAuthData();
            if (authData is not { CustId: > 0 }) return RedirectErrorPage(model);
            var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
            var result = _paymentBll.DepositReqOnlineBanking(authData.CustId, sysId, payId, ChannelId, Amount,
                out var transId, out var onlineBank);

            if (result == (int)ErrorCode.Success)
            {
                if (_promotionBll.VerifyBonusCode(BonusCode))
                    _promotionBll.SetDepositPromotionQueue(transId, BonusCode, authData.CustId, custInfo.SiteId,
                        custInfo.UserName, custInfo.CurrencyId, 1, 0);

                var reqDepositBank = new ReqDepositBank
                {
                    TransId = transId,
                    CustId = authData.CustId,
                    Amount = Amount,
                    PayId = onlineBank.PayId,
                    SysId = onlineBank.SysId,
                    ClientIP = _tools.GetClientIP(),
                    ReturnUrl = $"{Request.Scheme}://{Request.Host.Host}/{custInfo.UserLang}/deposit"
                };

                var myUrl = $"{_payApiUrl}ReqDepositBank";
                var apiResult = await _courierAdapter.MyPostAsync<PayResult, ReqDepositBank>(myUrl, reqDepositBank);
                if (ActionResult(apiResult, transId, model, PaymentView.DepositReqOnlineBanking,
                        out var actionResult))
                    return actionResult;
            }
            return RedirectErrorPage(model);
        }

        public async Task<IActionResult> DepositReqMobilePayAsync(int ChannelId, decimal Amount, string BonusCode, string PayAccount)
        {
            var model = new ErrorData();
            var authData = _authorizeBll.GetAuthData();
            if (authData is not { CustId: > 0 }) return RedirectErrorPage(model);
            var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
            var result = _payInfoBll.DepositReqPay(authData.CustId, ChannelId, Amount, out var transId, out var payInfo, DepositType.MobilePayment);
            if (result == (int)ErrorCode.Success)
            {
                if (_promotionBll.VerifyBonusCode(BonusCode))
                    _promotionBll.SetDepositPromotionQueue(transId, BonusCode, authData.CustId, custInfo.SiteId, custInfo.UserName, custInfo.CurrencyId, 1, 0);
                var reqDepositMobile = new ReqDepositMobile
                {
                    TransId = transId,
                    CustId = authData.CustId,
                    Amount = Amount,
                    ChannelId = payInfo.ChannelId,
                    PayId = payInfo.PayId,
                    ClientIP = _tools.GetClientIP(),
                    ReturnUrl = $"{Request.Scheme}://{Request.Host.Host}/{custInfo.UserLang}/deposit",
                    PayAccount = PayAccount
                };

                var myUrl = $"{_payApiUrl}ReqDepositMobile";
                var apiResult = await _courierAdapter.MyPostAsync<PayResult, ReqDepositMobile>(myUrl, reqDepositMobile);
                if (ActionResult(apiResult, transId, model, PaymentView.DepositReqMobilePay, out var actionResult))
                    return actionResult;
            }

            return RedirectErrorPage(model);
        }

        public class ReqDepositCrypto : ReqDeposit
        {
            public string DepositInfo { get; set; }
            public int ChannelId { get; set; }
        }

        private static readonly IEnumerable<int> NoExchangeInfoPay
            = new List<int> { (int)PayEnum.Coin2Pay };

        public async Task<IActionResult> DepositReqCryptoPayAsync(int ChannelId, decimal Amount, string BonusCode)
        {
            var model = new ErrorData();
            var authData = _authorizeBll.GetAuthData();
            if (authData is not { CustId: > 0 }) return RedirectErrorPage(model);
            var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
            var result = _cryptoPayInfoBll.DepositReqCryptoPay(authData.CustId, ChannelId, Amount, out var transId, out var cryptoPayInfo);
            if (result == (int)ErrorCode.Success)
            {
                var depositInfo = JsonConvert.SerializeObject(cryptoPayInfo);
                // 三方存款不含金額、匯率等必須資訊，需先取別家的匯率
                if (NoExchangeInfoPay.Contains(cryptoPayInfo.PayId))
                {
                    var exchangeInfo =
                        await _cryptoPayInfoBll.GetExchangeInfoAsync(custInfo.SiteId, custInfo.CurrencyId, Amount, cryptoPayInfo.ChannelName, ExchangeTypeEnum.Buy);

                    if (exchangeInfo.ErrorCode != (int)ErrorCode.Success)
                    {
                        _logger.LogError($"{transId}, {cryptoPayInfo.PayName} get exchange info failed.");
                        return RedirectErrorPage(model);
                    }

                    var obj = JObject.Parse(depositInfo);
                    obj.AddOrSet("ExchangeRate", exchangeInfo.Data.Rate);
                    obj.AddOrSet("ExchangeAmount", exchangeInfo.Data.ExchangeAmount);
                    depositInfo = JsonConvert.SerializeObject(obj);
                }

                if (_promotionBll.VerifyBonusCode(BonusCode))
                    _promotionBll.SetDepositPromotionQueue(transId, BonusCode, authData.CustId, custInfo.SiteId, custInfo.UserName, custInfo.CurrencyId, 1, 0);
                var reqDepositCrypto = new ReqDepositCrypto
                {
                    TransId = transId,
                    CustId = authData.CustId,
                    Amount = Amount,
                    ChannelId = cryptoPayInfo.ChannelId,
                    PayId = cryptoPayInfo.PayId,
                    ClientIP = _tools.GetClientIP(),
                    DepositInfo = depositInfo,
                    ReturnUrl = $"{Request.Scheme}://{Request.Host.Host}/{custInfo.UserLang}/deposit",
                };

                var myUrl = $"{_payApiUrl}ReqDepositCrpyto";
                var apiResult = await _courierAdapter.MyPostAsync<PayResult, ReqDepositCrypto>(myUrl, reqDepositCrypto);
                if (ActionResult(apiResult, transId, model, PaymentView.DepositReqCryptoPay, out var actionResult))
                    return actionResult;
            }

            return RedirectErrorPage(model);
        }


        public async Task<IActionResult> DepositReqCryptoLocalPayAsync(int ChannelId, decimal Amount, string BonusCode)
        {
            var model = new ErrorData();
            var authData = _authorizeBll.GetAuthData();

            if (authData is not { CustId: > 0 })
                return RedirectErrorPage(new ErrorData());

            var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
            var cryptoInfos = _bankInfoOffBll.GetFilteredOfflineCryptoPayInfo(custInfo.CustId)
                .Where(x => (int)x.CryptoCurrencyId * -1 == ChannelId
                            && (x.MinAmount == 0 || Amount >= x.MinAmount)
                            && (x.MaxAmount == 0 || Amount <= x.MaxAmount))
                .ToList();

            if (!cryptoInfos.Any())
            {
                _logger.LogError("no channels available");
                return RedirectErrorPage(model);
            }

            // 隨機配卡
            var random = new Random();
            var cryptoInfo = cryptoInfos[random.Next(cryptoInfos.Count - 1)];

            var (errorCode, message, payResult) = await _cryptoPayInfoBll.DepositReqCryptoLocalPayAsync(
                custInfo, Amount, cryptoInfo);

            if (errorCode != (int)ErrorCode.Success)
            {
                _logger.LogError($"({errorCode}) {message}");
                return RedirectErrorPage(model);
            }

            var transId = message;
            if (_promotionBll.VerifyBonusCode(BonusCode))
                _promotionBll.SetDepositPromotionQueue(transId, BonusCode, authData.CustId, custInfo.SiteId, custInfo.UserName, custInfo.CurrencyId, 1, 0);

            var apiResult = new ApiResult<PayResult>
            {
                Data = payResult,
                ErrorCode = errorCode
            };

            return ActionResult(apiResult, transId, model, PaymentView.DepositReqCryptoPay, out var actionResult)
                ? actionResult : RedirectErrorPage(model);
        }

        public async Task<IActionResult> DepositReqLoadCardPayAsync(int ChannelId, string Code, string Serial, decimal Amount, string BonusCode)
        {
            var model = new ErrorData();
            var authData = _authorizeBll.GetAuthData();
            if (authData is not { CustId: > 0 }) return RedirectErrorPage(model);
            var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
            var result = _loadCardPayInfoBll.DepositReqLoadCardPay(authData.CustId, ChannelId, Code, Serial, Amount,
                out var transId, out var loadCardPayInfo);
            if (result == (int)ErrorCode.Success)
            {
                if (_promotionBll.VerifyBonusCode(BonusCode))
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
                    ReturnUrl = $"{Request.Scheme}://{Request.Host.Host}/{custInfo.UserLang}/deposit"
                };

                var myUrl = $"{_payApiUrl}ReqDepositLoadCard";
                var apiResult = await _courierAdapter.MyPostAsync<PayResult, dynamic>(myUrl, reqDeposit);
                if (ActionResult(apiResult, transId, model, PaymentView.DepositReqLoadCardPay, out var actionResult))
                    return actionResult;
            }

            return RedirectErrorPage(model);
        }

        [Route("[controller]/DepositReceip.jpg")]
        [HttpGet]
        public List<DepositReceiptModel> GetDepositReceiptPic(string transId)
        {
            var authData = _authorizeBll.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                return null;
            }
            var depositReceipt = _paymentBll.GetDepositReceipt(transId, authData.CustId);
            if (depositReceipt == null)
            {
                return null;
            }

            var result = new List<DepositReceiptModel>();
            foreach (var receipt in depositReceipt)
            {
                var model = new DepositReceiptModel
                {
                    PicFileName = receipt.PicFileName,
                    ReceiptNo = receipt.ReceiptNo,
                    Receipt = _paymentBll.GetDepositReceiptUrl(transId, receipt.ReceiptNo)
                };
                result.Add(model);
            }
            return result;
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
        public List<DepositReceiptModel> GetDepositReceiptPicV2(string transId)
        {
            var authData = _authorizeBll.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                return null;
            }

            var result = new List<DepositReceiptModel>();
            var depositReceipt = _paymentBll.GetDepositReceipt(transId, authData.CustId);
            if (depositReceipt == null)
            {
                return null;
            }

            foreach (var receipt in depositReceipt)
            {
                var model = new DepositReceiptModel
                {
                    PicFileName = receipt.PicFileName,
                    ReceiptNo = receipt.ReceiptNo,
                    Receipt = _paymentBll.GetDepositReceiptUrl(transId, receipt.ReceiptNo)
                };
                result.Add(model);
            }
            return result;
        }

        public async Task<IActionResult> DepositReqQRPayAsync(int ChannelId, decimal Amount, string BonusCode)
        {
            var model = new ErrorData();
            var authData = _authorizeBll.GetAuthData();
            if (authData is not { CustId: > 0 }) return RedirectErrorPage(model);
            var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
            var result = _payInfoBll.DepositReqPay(authData.CustId, ChannelId, Amount, out var transId, out var payInfo, DepositType.QRPayPayment);
            if (result != (int)ErrorCode.Success)
            {
                return RedirectErrorPage(model);
            }

            if (_promotionBll.VerifyBonusCode(BonusCode))
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
                ReturnUrl = $"{Request.Scheme}://{Request.Host.Host}/{custInfo.UserLang}/deposit"
            });

            return ActionResult(apiResult, transId, model, PaymentView.DepositReqMobilePay, out var actionResult)
                ? actionResult
                : RedirectErrorPage(model);
        }


        public async Task<IActionResult> DepositReqBankAutoChannelAsync(int sysId, decimal Amount, string BonusCode)
        {
            var authData = _authorizeBll.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                return RedirectErrorPage(new ErrorData());
            }
            var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
            var vipLevel = _custInfoBll.GetPointLevelWithCache(authData.CustId);
            var depositType = _bankInfoBll.GetAutoChannelDepositType(custInfo.SiteId);
            var url = depositType == DepositType.BankAutoChannel ? $"{_payApiUrl}BankAutoChannel" : $"{_payApiUrl}ReqDepositBank";
            var transId = "";

            //取得可用銀行列表
            var bankList = _bankInfoBll.GetAutoChannelBankList(custInfo, vipLevel)
                .Where(i => i.SysId == sysId && Amount <= i.MaxAmount && Amount >= i.MinAmount);

            foreach (var bank in bankList.OrderBy(i => i.SSort))
            {
                var result = _paymentBll.DepositReqOnlineBanking(authData.CustId, sysId, bank.PayId, bank.ChannelId, Amount, out transId, out var onlineBank, depositType);
                if (result != (int)ErrorCode.Success)
                {
                    continue;
                }

                if (_promotionBll.VerifyBonusCode(BonusCode))
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
                    ReturnUrl = $"{Request.Scheme}://{Request.Host.Host}/{custInfo.UserLang}/deposit"
                });

                if (ActionResult(apiResult, transId, new ErrorData(), PaymentView.DepositReqOnlineBanking, out var actionResult))
                    return actionResult;
            }

            return RedirectErrorPage(new ErrorData { Message = $"{transId} All channel unavailable." });
        }

        private bool ActionResult(ApiResult<PayResult> apiResult, string transId, ErrorData model, PaymentView paymentView, out IActionResult actionResult)
        {
            switch (apiResult.ErrorCode)
            {
                case (int)ErrorCode.Success when apiResult.Data.Mode == (int)PayChannelMode.OpenView:
                    {

                        if (paymentView == PaymentView.DepositReqOnlineBanking &&
                            !string.IsNullOrEmpty(apiResult.Data.Message))
                        {
                            actionResult = Content(apiResult.Data.Message, "text/html");
                        }
                        else
                        {
                            actionResult = View(paymentView.ToString(), apiResult);
                        }

                        return true;
                    }
                case (int)ErrorCode.Success when apiResult.Data.Mode == (int)PayChannelMode.Redirect:
                    {
                        actionResult = Redirect(apiResult.Data.Url);
                        return true;
                    }
                case (int)ErrorCode.Success when apiResult.Data.Mode == (int)PayChannelMode.ShowQrCode:
                    {
                        actionResult = Json(apiResult);
                        return true;
                    }
                default:
                    model.Message = $"{transId} {apiResult.RawMessage}";
                    actionResult = null;
                    return false;
            }
        }


        private IActionResult RedirectErrorPage(ErrorData model)
        {
            var serverInfo = _serverInfoBll.GetServerInfo();
            if (serverInfo.SiteId is 1 or 2 or 3) //Nova88,Goal123,Bwing
            {
                return View($"~/Views/entPage/{serverInfo.SiteName}PaymentErrorPage.cshtml", model);
            }
            else
            {
                return Json(model);
            }
        }
    }
}