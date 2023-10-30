using MemberSite.Exceptions;
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
        private readonly CryptoPayInfoBll _cryptoPayInfoBll;
        private readonly DepositBLL _depositBll;
        private readonly PromotionBll _promotionBll;
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
            _depositBll = provider.GetRequiredService<DepositBLL>();
            _promotionBll = provider.GetRequiredService<PromotionBll>();
            _bankInfoBll = provider.GetRequiredService<BankInfoBLL>();
            _bankInfoOffBll = provider.GetRequiredService<BankInfoOffBLL>();
            _logger = provider.GetRequiredService<ILogger<CashierController>>();
            var configBll = provider.GetRequiredService<ConfigBLL>();
            _payApiUrl = configBll.GetConfigValueByEnvironment<ServiceHostConfig>("ServiceHost").PayService;
        }
        
        [HttpGet]
        public async Task<IActionResult> DepositReqOnlineBankingAsync(int sysId, int payId, int ChannelId, decimal Amount, string BonusCode)
        {
            var authData = _authorizeBll.GetAuthData();
            
            if (authData is not { CustId: > 0 })
                return RedirectErrorPage(new ErrorData());
            
            var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
            var result = _paymentBll.DepositReqOnlineBanking(authData.CustId, sysId, payId, ChannelId, Amount,
                out var transId, out var onlineBank);

            if (result != (int) ErrorCode.Success)
                return RedirectErrorPage(new ErrorData());
            
            if (_promotionBll.VerifyBonusCode(BonusCode))
                _promotionBll.SetDepositPromotionQueue(transId, BonusCode, authData.CustId, custInfo.SiteId,
                    custInfo.UserName, custInfo.CurrencyId, 1, 0);

            var apiResult = await CreateDeposit(transId, custInfo);
            
            return ActionResult(apiResult, transId, new ErrorData(), PaymentView.DepositReqOnlineBanking, out var actionResult)
                ? actionResult : RedirectErrorPage(new ErrorData());
        }

        public async Task<IActionResult> DepositReqMobilePayAsync(int ChannelId, decimal Amount, string BonusCode, string PayAccount)
        {
            var authData = _authorizeBll.GetAuthData();
            
            if (authData is not { CustId: > 0 })
                return RedirectErrorPage(new ErrorData());
            
            return await CreateDeposit(new DepositReqParams
            {
                Amount = Amount,
                ChannelId = ChannelId,
                CustId = authData.CustId,
                DepositType = DepositType.MobilePayment,
                ExtraData = new Dictionary<string, string>
                {
                    {"PayAccount", PayAccount}
                }
            }, BonusCode);
        }

        public async Task<IActionResult> DepositReqCryptoPayAsync(int ChannelId, decimal Amount, string BonusCode)
        {
            var authData = _authorizeBll.GetAuthData();
            
            if (authData is not { CustId: > 0 })
                return RedirectErrorPage(new ErrorData());
            
            return await CreateDeposit(new DepositReqParams
            {
                Amount = Amount,
                ChannelId = ChannelId,
                CustId = authData.CustId,
                DepositType = DepositType.CryptoPayment,
                ExtraData = new Dictionary<string, string>()
            }, BonusCode);
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
            var authData = _authorizeBll.GetAuthData();
            if (authData is not { CustId: > 0 })
                return RedirectErrorPage(new ErrorData());
            
            return await CreateDeposit(new DepositReqParams
            {
                Amount = Amount,
                ChannelId = ChannelId,
                CustId = authData.CustId,
                DepositType = DepositType.LoadCardPayment,
                ExtraData = new Dictionary<string, string>
                {
                    {"Code", Code},
                    {"Serial", Serial}
                }
            }, BonusCode);
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
            
            return await CreateDeposit(new DepositReqParams
            {
                Amount = Amount,
                ChannelId = ChannelId,
                CustId = authData.CustId,
                DepositType = DepositType.QRPayPayment,
                ExtraData = new Dictionary<string, string>()
            }, BonusCode);
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
            var transId = "";

            //取得可用銀行列表
            var bankList = _bankInfoBll.GetAutoChannelBankList(custInfo, vipLevel)
                .Where(i => i.SysId == sysId && Amount <= i.MaxAmount && Amount >= i.MinAmount);

            foreach (var bank in bankList.OrderBy(i => i.SSort))
            {
                var result = _paymentBll.DepositReqOnlineBanking(authData.CustId, sysId, bank.PayId, bank.ChannelId, Amount, out transId, out _, depositType);
                if (result != (int)ErrorCode.Success)
                {
                    continue;
                }

                if (_promotionBll.VerifyBonusCode(BonusCode))
                {
                    _promotionBll.SetDepositPromotionQueue(transId, BonusCode, authData.CustId, custInfo.SiteId, custInfo.UserName, custInfo.CurrencyId, 1, 0);
                }

                var apiResult = await CreateDeposit(transId, custInfo);
            
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

        #region Old

        

        #endregion

        private ViewResult RedirectErrorPage(ErrorData model)
        {
            var serverInfo = _serverInfoBll.GetServerInfo();
            return View($"~/Views/entPage/{serverInfo.SiteName}PaymentErrorPage.cshtml", model);
        }

        private class DepositReq
        {
            public string TransId { get; set; }
            public string ClientIP { get; set; }
            public string ReturnUrl { get; set; }
        }
        
        private async Task<IActionResult> CreateDeposit(DepositReqParams input, string bonusCode)
        {
            try
            {
                var transId = await _depositBll.DepositReqAsync(input);
                var custInfo = _custInfoBll.GetCustInfoWithCache(input.CustId);
                
                if (_promotionBll.VerifyBonusCode(bonusCode))
                    _promotionBll.SetDepositPromotionQueue(
                        transId,
                        bonusCode,
                        custInfo.CustId,
                        custInfo.SiteId,
                        custInfo.UserName,
                        custInfo.CurrencyId,
                        1,
                        0);

                var apiResult = await CreateDeposit(transId, custInfo);
                
                if (ActionResult(apiResult, transId, new ErrorData(), PaymentView.DepositReqMobilePay, out var actionResult))
                    return actionResult;
            }
            catch (CustomException ex)
            {
                _logger.LogInformation($"({ex.Code}) {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
            }
            
            return RedirectErrorPage(new ErrorData());
        }

        private async Task<ApiResult<PayResult>> CreateDeposit(string transId, Customer custInfo)
        {
            var url = $"{_payApiUrl}Deposit/Create";
            var apiResult = await _courierAdapter.MyPostAsync<PayResult, DepositReq>(url, new DepositReq
            {
                TransId = transId,
                ClientIP = _tools.GetClientIP(),
                ReturnUrl = $"{Request.Scheme}://{Request.Host.Host}/{custInfo.UserLang}/deposit"
            });

            return apiResult;
        }
    }
}