using MemberSite.Exceptions;
using MemberSite.Extensions;
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
        private readonly PayInfoBLL _payInfoBll;
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
            _payInfoBll = provider.GetRequiredService<PayInfoBLL>();
            _logger = provider.GetRequiredService<ILogger<CashierController>>();
            var configBll = provider.GetRequiredService<ConfigBLL>();
            _payApiUrl = configBll.GetConfigValueByEnvironment<ServiceHostConfig>("ServiceHost").PayService;
        }

        [HttpGet]
        public async Task<IActionResult> DepositReqOnlineBankingAsync(int sysId, int payId, int ChannelId, decimal Amount, string BonusCode, string ErrorUrl)
        {
            var model = new ErrorData();
            var authData = _authorizeBll.GetAuthData();

            if (authData is not { CustId: > 0 })
                return RedirectErrorPage(ErrorUrl, new ErrorData("Not Login."));

            try
            {
                return await DepositOnlineBankingAsync(authData.CustId, sysId, ChannelId, Amount, BonusCode, null, ErrorUrl);
            }
            catch (CustomException ex)
            {
                _logger.LogInformation($"({ex.Code}) {ex.Message}");
                model.Message = $"({ex.Code}) {ex.Message}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                model.Message = $"Exception-{ex.Message}";
            }

            return RedirectErrorPage(ErrorUrl, model);
        }

        public async Task<IActionResult> DepositReqMobilePayAsync(int ChannelId, decimal Amount, string BonusCode, string PayAccount, string ErrorUrl)
        {
            var authData = _authorizeBll.GetAuthData();

            if (authData is not { CustId: > 0 })
                return RedirectErrorPage(ErrorUrl, new ErrorData("Not Login."));

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
            }, BonusCode, ErrorUrl);
        }

        public async Task<IActionResult> DepositReqCryptoPayAsync(int ChannelId, decimal Amount, string BonusCode, string ErrorUrl)
        {
            var authData = _authorizeBll.GetAuthData();

            if (authData is not { CustId: > 0 })
                return RedirectErrorPage(ErrorUrl, new ErrorData("Not Login."));

            return await CreateDeposit(new DepositReqParams
            {
                Amount = Amount,
                ChannelId = ChannelId,
                CustId = authData.CustId,
                DepositType = DepositType.CryptoPayment,
                ExtraData = new Dictionary<string, string>()
            }, BonusCode, ErrorUrl);
        }


        public async Task<IActionResult> DepositReqCryptoLocalPayAsync(int ChannelId, decimal Amount, string BonusCode, string ErrorUrl)
        {
            var authData = _authorizeBll.GetAuthData();

            if (authData is not { CustId: > 0 })
                return RedirectErrorPage(ErrorUrl, new ErrorData("Not Login."));

            return await DepositCryptoWalletAsync(authData.CustId, ChannelId, Amount, BonusCode, ErrorUrl);
        }

        public async Task<IActionResult> DepositReqLoadCardPayAsync(int ChannelId, string Code, string Serial, decimal Amount, string BonusCode, string ErrorUrl)
        {
            var authData = _authorizeBll.GetAuthData();
            if (authData is not { CustId: > 0 })
                return RedirectErrorPage(ErrorUrl, new ErrorData("Not Login."));

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
            }, BonusCode, ErrorUrl);
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

        public async Task<IActionResult> DepositReqQRPayAsync(int ChannelId, decimal Amount, string BonusCode, string ErrorUrl)
        {
            var authData = _authorizeBll.GetAuthData();

            if (authData is not { CustId: > 0 })
                return RedirectErrorPage(ErrorUrl, new ErrorData("Not Login."));

            return await CreateDeposit(new DepositReqParams
            {
                Amount = Amount,
                ChannelId = ChannelId,
                CustId = authData.CustId,
                DepositType = DepositType.QrPayPayment,
                ExtraData = new Dictionary<string, string>()
            }, BonusCode, ErrorUrl);
        }

        public async Task<IActionResult> DepositReqBankAutoChannelAsync(int sysId, decimal Amount, string BonusCode, string ErrorUrl)
        {
            var model = new ErrorData();
            var authData = _authorizeBll.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                return RedirectErrorPage(ErrorUrl, new ErrorData("Not Login."));
            }

            try
            {
                return await DepositBankAutoSelectChannelAsync(authData.CustId, sysId, Amount, BonusCode, ErrorUrl);
            }
            catch (CustomException ex)
            {
                _logger.LogInformation($"({ex.Code}) {ex.Message}");
                model.Message = $"({ex.Code}) {ex.Message}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                model.Message = $"Exception-{ex.Message}";
            }

            return RedirectErrorPage(ErrorUrl, model);
        }

        public async Task<IActionResult> DepositReqAsync(int channelId, int sysId, decimal amount, string bonusCode,
            string payAccount, string code, string serial, string errorUrl)
        {
            var authData = _authorizeBll.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                return RedirectErrorPage(errorUrl, new ErrorData("Not Login."));
            }

            try
            {
                if (channelId == 0 && sysId > 0)
                {
                    return await DepositBankAutoSelectChannelAsync(authData.CustId, sysId, amount, bonusCode, errorUrl);
                }

                var channels = _payInfoBll.GetPayChannelWithCache();
                var channel = channels.FirstOrDefault(x => x.ChannelId == channelId);

                if (channel == null)
                {
                    _logger.LogError("No channel data, channelId=" + channelId);
                    return RedirectErrorPage(errorUrl, new ErrorData("No channel data."));
                }

                var p = new DepositReqParams
                {
                    Amount = amount,
                    ChannelId = channelId,
                    CustId = authData.CustId,
                    DepositType = channel.Type
                };

                switch (channel.Type)
                {
                    case DepositType.BankAutoChannel:
                    case DepositType.OnlineBanking:
                    case DepositType.QuickTransfer:
                        return await DepositOnlineBankingAsync(authData.CustId, sysId, channelId, amount, bonusCode, new Dictionary<string, string>
                        {
                            {"PayAccount", payAccount}
                        }, errorUrl, channel.Type);

                    case DepositType.MobilePayment:
                        p.ExtraData = new Dictionary<string, string>
                        {
                            {"PayAccount", payAccount}
                        };
                        break;

                    case DepositType.CryptoPayment:
                        p.ExtraData = new Dictionary<string, string>();
                        break;

                    case DepositType.LoadCardPayment:
                        p.ExtraData = new Dictionary<string, string>
                        {
                            {"Code", code},
                            {"Serial", serial}
                        };
                        break;
                    case DepositType.ChatTransfer:
                        p.ExtraData = new Dictionary<string, string>();
                        break;

                    default:
                        _logger.LogError($"No supported type={channel.Type}, channelId={channelId}");
                        return RedirectErrorPage(errorUrl, new ErrorData("Not supported."));
                }

                return await CreateDeposit(p, bonusCode, errorUrl);
            }
            catch (CustomException ex)
            {
                _logger.LogInformation($"({ex.Code}) {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
            }

            return RedirectErrorPage(errorUrl, new ErrorData());
        }

        private async Task<IActionResult> DepositCryptoWalletAsync(int custId, int channelId, decimal amount, string bonusCode, string errorUrl)
        {
            var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
            var cryptoInfos = _bankInfoOffBll.GetFilteredOfflineCryptoPayInfo(custInfo.CustId)
                .Where(x => (int)x.CryptoCurrencyId * -1 == channelId
                            && (x.MinAmount == 0 || amount >= x.MinAmount)
                            && (x.MaxAmount == 0 || amount <= x.MaxAmount))
                .ToList();

            if (!cryptoInfos.Any())
            {
                var msg = "no channels available";
                _logger.LogError(msg);
                return RedirectErrorPage(errorUrl, new ErrorData { Message = msg });
            }

            // 隨機配卡
            var random = new Random();
            var cryptoInfo = cryptoInfos[random.Next(cryptoInfos.Count - 1)];

            var (errorCode, message, payResult) = await _cryptoPayInfoBll.DepositReqCryptoLocalPayAsync(
                custInfo, amount, cryptoInfo);

            if (errorCode != (int)ErrorCode.Success)
            {
                _logger.LogError($"({errorCode}) {message}");
                return RedirectErrorPage(errorUrl, new ErrorData { Message = message });
            }

            var transId = message;
            if (_promotionBll.VerifyBonusCode(bonusCode))
                _promotionBll.SetDepositPromotionQueue(transId, bonusCode, custId, custInfo.SiteId, custInfo.UserName, custInfo.CurrencyId, 1, 0);

            var apiResult = new ApiResult<PayResult>
            {
                Data = payResult,
                ErrorCode = errorCode
            };

            return ActionResult(apiResult, transId, new ErrorData(), PaymentView.DepositReqCryptoPay, out var actionResult)
                ? actionResult : RedirectErrorPage(errorUrl, new ErrorData());
        }

        private async Task<IActionResult> DepositBankAutoSelectChannelAsync(int custId, int sysId, decimal amount, string bonusCode, string errorUrl)
        {
            var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
            var vipLevel = _custInfoBll.GetPointLevelWithCache(custId);
            var transId = string.Empty;
            const DepositType type = DepositType.OnlineBanking;

            //取得可用銀行列表
            var bankList = _bankInfoBll.GetSelectBankBankList(custInfo, vipLevel, type)
                .Where(i => i.SysId == sysId && amount <= i.MaxAmount && amount >= i.MinAmount);

            foreach (var bank in bankList.OrderBy(i => i.SSort))
            {
                var result = _paymentBll.DepositReqOnlineBanking(custId, sysId, bank.ChannelId, amount, type, null, out transId);
                if (result != (int)ErrorCode.Success)
                {
                    continue;
                }

                var apiResult = await CreateDepositAndSetPromotionAsync(custId, transId, bonusCode);

                if (ActionResult(apiResult, transId, new ErrorData(), PaymentView.DepositReqOnlineBanking, out var actionResult))
                    return actionResult;
            }

            return RedirectErrorPage(errorUrl, new ErrorData($"{transId} All channel unavailable."));
        }

        private async Task<IActionResult> DepositOnlineBankingAsync(
            int custId, int sysId, int channelId, decimal amount, string bonusCode,
            IDictionary<string, string> extraData, string errorUrl, DepositType depositType = DepositType.OnlineBanking)
        {
            var result = _paymentBll.DepositReqOnlineBanking(custId, sysId, channelId, amount, depositType, extraData, 
                out var transId);

            if (result != (int)ErrorCode.Success)
                return RedirectErrorPage(errorUrl, new ErrorData($"Deposit{depositType.GetDescription()} Failed."));

            var apiResult = await CreateDepositAndSetPromotionAsync(custId, transId, bonusCode);

            return ActionResult(apiResult, transId, new ErrorData(), PaymentView.DepositReqOnlineBanking, out var actionResult)
                ? actionResult : RedirectErrorPage(errorUrl, new ErrorData());
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

        private IActionResult RedirectErrorPage(string errorUrl, ErrorData model)
        {
            var serverInfo = _serverInfoBll.GetServerInfo();
            if (serverInfo.SiteId is 1 or 2 or 3 or 5) //Nova88,Goal123,Bwing
            {
                return View($"~/Views/entPage/{serverInfo.SiteName}PaymentErrorPage.cshtml", model);
            }
            else
            {
                return Redirect($"{errorUrl}?Message={model.Message}");
            }
        }

        private class DepositReq
        {
            public string TransId { get; set; }
            public string ClientIP { get; set; }
            public string ReturnUrl { get; set; }

            public DeviceId DeviceType { get; set; }
        }

        private readonly IDictionary<DepositType, PaymentView> _paymentViewMapper =
            new Dictionary<DepositType, PaymentView>
            {
                {DepositType.OnlineBanking, PaymentView.DepositReqOnlineBanking},
                {DepositType.MobilePayment, PaymentView.DepositReqMobilePay},
                {DepositType.CryptoPayment, PaymentView.DepositReqCryptoPay},
                {DepositType.CryptoWallet, PaymentView.DepositReqCryptoPay},
                {DepositType.QrPayPayment, PaymentView.DepositReqMobilePay},
                {DepositType.LoadCardPayment, PaymentView.DepositReqLoadCardPay},
                {DepositType.BankAutoChannel, PaymentView.DepositReqOnlineBanking},
                {DepositType.QuickTransfer, PaymentView.DepositReqOnlineBanking},
                {DepositType.ChatTransfer, PaymentView.DepositReqOnlineBanking}
            };

        private async Task<IActionResult> CreateDeposit(DepositReqParams input, string bonusCode, string errorUrl)
        {
            var model = new ErrorData();
            try
            {
                var transId = await _depositBll.DepositReqAsync(input);
                var apiResult = await CreateDepositAndSetPromotionAsync(input.CustId, transId, bonusCode);

                if (ActionResult(apiResult, transId, new ErrorData(), _paymentViewMapper[input.DepositType], out var actionResult))
                    return actionResult;
            }
            catch (CustomException ex)
            {
                _logger.LogInformation($"({ex.Code}) {ex.Message}");
                model.Message = $"({ex.Code}) {ex.Message}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                model.Message = $"Exception-{ex.Message}";
            }

            return RedirectErrorPage(errorUrl, model);
        }

        private async Task<ApiResult<PayResult>> CreateDepositAndSetPromotionAsync(int custId, string transId, string bonusCode)
        {
            var custInfo = _custInfoBll.GetCustInfoWithCache(custId);

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

            return await CreateDeposit(transId, custInfo);
        }

        private async Task<ApiResult<PayResult>> CreateDeposit(string transId, Customer custInfo)
        {
            var url = $"{_payApiUrl}Deposit/Create";
            var apiResult = await _courierAdapter.MyPostAsync<PayResult, DepositReq>(url, new DepositReq
            {
                TransId = transId,
                ClientIP = _tools.GetClientIP(),
                DeviceType = _tools.GetDeviceId(_tools.GetFromUrl()),
                ReturnUrl = $"{Request.Scheme}://{Request.Host.Host}/{custInfo.UserLang}/deposit"
            });

            return apiResult;
        }
    }
}