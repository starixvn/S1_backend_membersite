using Amazon.S3.Model;
using MemberSite.Service.BLL;
using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using static MemberSite.Extensions.MethodExtension;

namespace MemberSite.Controllers.Api
{
    [Authorize]
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly BankInfoOffBLL bankInfoOffBLL;
        private readonly AuthorizeBLL authorizeBLL;
        private readonly PaymentBLL paymentBLL;
        private readonly CustInfoBll custInfoBLL;
        private readonly BankInfoBLL bankInfoBLL;
        private readonly SelLangBLL selLangBLL;
        private readonly WalletBLL walletBLL;
        private readonly IovationBLL iovationBLL;
        private readonly CryptoPayInfoBll cryptoPayInfoBLL;
        private readonly LoadCardPayInfoBLL loadCardPayInfoBLL;
        private readonly PayInfoBLL _payInfoBLL;
        private readonly PromotionBll _promotionBll;
        private readonly CustInfoService _custInfoService;
        private readonly ILogger<PaymentController> _logger;
        private readonly CourierAdapter _courierAdapter;
        private readonly AWSS3Helper _s3Helper;
        private readonly ICacheHelper _cacheHelper;

        private readonly string _paymentBucketName;
        private readonly string _bonusServiceUrl;

        public PaymentController(IServiceProvider provider)
        {
            bankInfoOffBLL = provider.GetRequiredService<BankInfoOffBLL>();
            authorizeBLL = provider.GetRequiredService<AuthorizeBLL>();
            paymentBLL = provider.GetRequiredService<PaymentBLL>();
            custInfoBLL = provider.GetRequiredService<CustInfoBll>();
            bankInfoBLL = provider.GetRequiredService<BankInfoBLL>();
            
            selLangBLL = provider.GetRequiredService<SelLangBLL>();
            walletBLL = provider.GetRequiredService<WalletBLL>();
            iovationBLL = provider.GetRequiredService<IovationBLL>();
            cryptoPayInfoBLL = provider.GetRequiredService<CryptoPayInfoBll>();
            loadCardPayInfoBLL = provider.GetRequiredService<LoadCardPayInfoBLL>();
            
            _payInfoBLL = provider.GetRequiredService<PayInfoBLL>();
            _promotionBll = provider.GetRequiredService<PromotionBll>();
            _custInfoService = provider.GetRequiredService<CustInfoService>();
            _logger = provider.GetRequiredService<ILogger<PaymentController>>();
            _courierAdapter = provider.GetRequiredService<CourierAdapter>();
            _s3Helper = provider.GetRequiredService<AWSS3Helper>();
            _cacheHelper = provider.GetRequiredService<ICacheHelper>();

            var configBll = provider.GetRequiredService<ConfigBLL>();
            _bonusServiceUrl = configBll.GetConfigValueByEnvironment<ServiceHostConfig>("ServiceHost").BonusService;
            _paymentBucketName = configBll.GetConfigValue("AWSPaymentBucketName");
        }

        [HttpPost]
        public ApiResult<IEnumerable<OfflineBankInfoFront>> GetBankInfoOffList()
        {
            var authData = authorizeBLL.GetAuthData();

            if (authData is not { CustId: > 0 })
            {
                return new ApiResult<IEnumerable<OfflineBankInfoFront>>
                {
                    Message = selLangBLL.GetMsg("msg_ForceLogedout")
                };
            }

            return new ApiResult<IEnumerable<OfflineBankInfoFront>>
            {
                Data = bankInfoOffBLL.GetBankInfoOffListWithCache(authData.CustId),
                ErrorCode = 0
            };
        }

        [HttpPost]
        public ApiResult<IEnumerable<OfflineBankInfoFront>> GetBankInfoOffListStatus()
        {
            var authData = authorizeBLL.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                return new ApiResult<IEnumerable<OfflineBankInfoFront>>
                {
                    Message = selLangBLL.GetMsg("msg_ForceLogedout")
                };
            }

            return new ApiResult<IEnumerable<OfflineBankInfoFront>>
            {
                Data = bankInfoOffBLL.GetBankInfoOffListWithCache(authData.CustId),
                ErrorCode = 0
            };
        }

        [HttpPost]
        public ApiResult<IEnumerable<WithdrawalHistory>> GetWithdrawalInfoRecent()
        {
            var apiResult = new ApiResult<IEnumerable<WithdrawalHistory>>();
            var authData = authorizeBLL.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                apiResult.Message = selLangBLL.GetMsg("msg_ForceLogedout");
                return apiResult;
            }

            apiResult.Data = paymentBLL.GetWithdrawalInfoRecent(authData.CustId);
            apiResult.ErrorCode = 0;
            return apiResult;
        }

        [HttpPost]
        public ApiResult<IEnumerable<DepositInfoHistory>> GetDepositInfoRecent()
        {
            var apiResult = new ApiResult<IEnumerable<DepositInfoHistory>>();
            var authData = authorizeBLL.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                apiResult.Message = selLangBLL.GetMsg("msg_ForceLogedout");
                return apiResult;
            }

            apiResult.Data = paymentBLL.GetDepositInfoRecent(authData.CustId);
            apiResult.ErrorCode = 0;
            return apiResult;
        }

        public class HistoryReq
        {
            public long StartDate { get; set; }
            public long EndDate { get; set; }
            public int PageNumber { get; set; }
            //public int PageSize { get; set; }
        }

        [HttpPost]
        public ApiListResult<DepositInfoHistory> GetDepositInfoHistory(HistoryReq historyReq)
        {
            var apiResult = new ApiListResult<DepositInfoHistory>();
            var authData = authorizeBLL.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                apiResult.Message = selLangBLL.GetMsg("msg_ForceLogedout");
                return apiResult;
            }
            var pageNumber = historyReq.PageNumber;
            apiResult.Data = paymentBLL.GetDepositInfoHistory(authData.CustId, historyReq.StartDate, historyReq.EndDate, ref pageNumber, out var totalPages, out var rowCount);
            apiResult.PageNumber = pageNumber;
            apiResult.TotalPages = totalPages;
            apiResult.RowCount = rowCount;
            apiResult.ErrorCode = 0;
            return apiResult;
        }

        [HttpPost]
        public ApiListResult<WithdrawalHistory> GetWithdrawalInfoHistory(HistoryReq historyReq)
        {
            var apiResult = new ApiListResult<WithdrawalHistory>();
            var authData = authorizeBLL.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                apiResult.Message = selLangBLL.GetMsg("msg_ForceLogedout");
                return apiResult;
            }
            var pageNumber = historyReq.PageNumber;
            apiResult.Data = paymentBLL.GetWithdrawalInfoHistory(authData.CustId, historyReq.StartDate, historyReq.EndDate, ref pageNumber, out var totalPages, out var rowCount);
            apiResult.PageNumber = pageNumber;
            apiResult.TotalPages = totalPages;
            apiResult.RowCount = rowCount;
            apiResult.ErrorCode = 0;
            return apiResult;
        }
        public class DepositReqWireTransferReq
        {
            public int sysId { set; get; }
            public decimal amount { set; get; }
            public string memo { set; get; }

            public int bankId { set; get; }
            public int fromChannel { set; get; }
            public int fromSysId { set; get; }
            public string fromAccountNum { set; get; }
            public string fromIFSCCode { set; get; }
            public bool hasPromotion { get; set; }
        }

        /// <summary>
        /// Local Bank Transfer
        /// </summary>
        /// <param name="depositReqWireTransferReq"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ApiResult<DepositRep3>> DepositReqWireTransfer(DepositReqWireTransferReq depositReqWireTransferReq)
        {
            var apiResult = new ApiResult<DepositRep3>();
            var authData = authorizeBLL.GetAuthData();
            
            if (bankInfoOffBLL.GetCustOffDepositSysId(authData.CustId).Contains(depositReqWireTransferReq.sysId))
            {
                apiResult.ErrorCode = -3;
                apiResult.Message = "A deposit with the same bank already exists. Please complete it before trying again.";
                return apiResult;
            }

            var checkResult = CheckDeposit(authData, depositReqWireTransferReq.amount,
                depositReqWireTransferReq.hasPromotion);

            if (checkResult.ErrorCode != 0)
            {
                return new ApiResult<DepositRep3>
                {
                    ErrorCode = checkResult.ErrorCode,
                    Message = checkResult.Message
                };
            }

            var key = $"Deposite:LocalBank:{authData.CustId}:{depositReqWireTransferReq.sysId}";
            if (await _cacheHelper.SetNxAsync(key, "", TimeSpan.FromSeconds(5)))
            {
                DepositRep3 depositRep3;
                var depositFromInfo = new DepositFromInfo
                {
                    Channel = depositReqWireTransferReq.fromChannel,
                    SysId = depositReqWireTransferReq.fromSysId,
                    AccountNo = depositReqWireTransferReq.fromAccountNum,
                    IFSCCode = depositReqWireTransferReq.fromIFSCCode
                };
                var fromInfo = Newtonsoft.Json.JsonConvert.SerializeObject(depositFromInfo);

                if (depositReqWireTransferReq.fromChannel == 0 && depositReqWireTransferReq.fromSysId == 0 && depositReqWireTransferReq.fromAccountNum == null)
                    apiResult.ErrorCode = paymentBLL.DepositReqLocalBank(authData.CustId, depositReqWireTransferReq.sysId,
                        depositReqWireTransferReq.bankId, depositReqWireTransferReq.amount, depositReqWireTransferReq.memo, out depositRep3);
                else
                    apiResult.ErrorCode = paymentBLL.DepositReqLocalBankWithFrom(authData.CustId, depositReqWireTransferReq.sysId,
                        depositReqWireTransferReq.bankId, depositReqWireTransferReq.amount, depositReqWireTransferReq.memo, fromInfo, out depositRep3);

                if (apiResult.ErrorCode == (int)ErrorCode.Success)
                {
                    apiResult.Data = depositRep3;
                }
            }
            return apiResult;
        }

        private ApiResult CheckDeposit(AuthData authData, decimal amount, bool hasPromotion)
        {
            if (authData is not { CustId: > 0 })
            {
                return new ApiResult
                {
                    ErrorCode = -1,
                    Message = selLangBLL.GetMsg("msg_ForceLogedout")
                };
            }

            //check AccountName first
            var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);

            if (custInfo.SiteId == (int)SiteId.Goal123 && custInfoBLL.GetCustBankInfo(authData.CustId) == null)
            {
                return new ApiResult
                {
                    ErrorCode = -2,
                    Message = selLangBLL.GetMsg("msg_NeedAccountName")
                };
            }

            const int decimalPlaces = 0;
            if (!HasThisManyDecimalPlacesOrLess(amount, decimalPlaces))
            {
                return new ApiResult
                {
                    ErrorCode = -5,
                    Message = "Amount format is wrong."
                };
            }

            if (hasPromotion)
            {
                var custInfoProfile = custInfoBLL.GetCustInfoProfile(authData.CustId);
                if (!custInfoProfile.PhoneVerified
                    || string.IsNullOrEmpty(custInfoProfile.AccountName)
                    || string.IsNullOrEmpty(custInfoProfile.Birth_y)
                    || string.IsNullOrEmpty(custInfoProfile.Birth_m)
                    || string.IsNullOrEmpty(custInfoProfile.Birth_d))
                {
                    return new ApiResult
                    {
                        ErrorCode = 2000,
                        Message = selLangBLL.GetMsg("msg_OTPHint")
                    };
                }
            }

            return new ApiResult
            {
                ErrorCode = 0
            };
        }

        public class DepositReqLocalPayReq
        {
            public int GroupId { set; get; }
            public decimal Amount { set; get; }
            public string BonusCode { set; get; }
        }

        [HttpPost]
        public ApiResult<LocalPayResp> DepositReqLocalPayAsync(DepositReqLocalPayReq req)
        {
            var authData = authorizeBLL.GetAuthData();
            var custId = authData.CustId;
            var custInfo = custInfoBLL.GetCustInfoWithCache(custId);
            var localPayment = bankInfoOffBLL.GetLocalPaymentSettingWithCache()
                .FirstOrDefault(x => x.GroupId == req.GroupId);

            if (localPayment == null)
            {
                return new ApiResult<LocalPayResp>
                {
                    ErrorCode = -1,
                    RawMessage = "No available channels."
                };
            }

            var cards = bankInfoOffBLL.GetOfflineBankCardListWithCache(custId)
                .Where(x => x.GroupIds.Contains(req.GroupId)
                    && req.Amount >= x.MinAmount
                    && req.Amount <= x.MaxAmount)
                .ToList();

            if (!cards.Any())
            {
                return new ApiResult<LocalPayResp>
                {
                    ErrorCode = -2,
                    RawMessage = "No available channels."
                };
            }

            var hasPromotion = _promotionBll.VerifyBonusCode(req.BonusCode);
            var checkResult = CheckDeposit(authData, req.Amount, hasPromotion);

            if (checkResult.ErrorCode != 0)
            {
                return new ApiResult<LocalPayResp>
                {
                    ErrorCode = checkResult.ErrorCode,
                    RawMessage = checkResult.Message
                };
            }

            // 隨機配卡
            var random = new Random();
            var card = cards[random.Next(cards.Count - 1)];
            var (errorCode, message) = paymentBLL.DepositReqLocalBank(new DepositReqLocalPayModel
            {
                Amount = req.Amount,
                CustId = custId,
                BankId = card.BankId,
                Type = localPayment.Type
            }, out var transId, out var code, out var bankAccount);

            if (errorCode != 0)
            {
                return new ApiResult<LocalPayResp>
                {
                    ErrorCode = errorCode,
                    RawMessage = message
                };
            }

            if (hasPromotion)
            {
                if (!_promotionBll.SetDepositPromotionQueue(
                        transId,
                        req.BonusCode,
                        authData.CustId,
                        custInfo.SiteId,
                        custInfo.UserName,
                        custInfo.CurrencyId,
                        1,
                        0))
                {
                    _logger.LogError($"TransId:{transId} SetDepositPromotionQueue Failed");
                };
            }

            if (localPayment.Type == DepositType.QrPayPayment && custInfo.CurrencyId == (int)CurrencyId.VND)
            {
                var vietQr = PaymentBLL.GenerateVietQr(bankAccount, transId, req.Amount);
                return new ApiResult<LocalPayResp>
                {
                    Data = new LocalPayResp
                    {
                        GenerateQr = true,
                        Amount = req.Amount,
                        ReturnUrl = vietQr,
                        TransId = transId,
                        Code = code,
                        BankLogo = card.BankLogo
                    },
                    ErrorCode = 0
                };
            }

            return new ApiResult<LocalPayResp>
            {
                Data = new LocalPayResp
                {
                    Amount = req.Amount,
                    ReturnUrl = card.QRCodeUrl,
                    TransId = transId,
                    Code = code,
                    BankLogo = card.BankLogo
                },
                ErrorCode = 0
            };
        }

        [HttpPost]
        public ApiResult<DepositRep3> GetLocalBankInfo(DepositReqWireTransferReq depositReqWireTransferReq)
        {
            var apiResult = new ApiResult<DepositRep3>();
            var authData = authorizeBLL.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                apiResult.Message = selLangBLL.GetMsg("msg_ForceLogedout");
                return apiResult;
            }

            apiResult.ErrorCode = bankInfoOffBLL.LocalBankinfo(authData.CustId, depositReqWireTransferReq.sysId, depositReqWireTransferReq.memo, out var depositRep3);
            if (apiResult.ErrorCode == (int)ErrorCode.Success)
            {
                apiResult.Data = depositRep3;
            }

            return apiResult;
        }

        public class WithdrawalReqModel
        {
            public int sysId { set; get; }
            public string AccountNo { set; get; }
            public string IFSCCode { get; set; }
            public decimal amount { set; get; }
            public string memo { set; get; }
            public string blackbox { get; set; }
            public int CurrencyId { get; set; }
            /// <summary>
            /// 參考匯率
            /// </summary>
            public decimal ExchangeRate { get; set; }
            /// <summary>
            /// 參考加密貨幣金額
            /// </summary>
            public decimal ExchangeAmount { get; set; }
        }

        [HttpPost]
        public async Task<ApiResult> WithdrawalReqAsync(WithdrawalReqModel withdrawalReqModel)
        {
            var apiResult = new ApiResult(selLangBLL);
            var authData = authorizeBLL.GetAuthData(true);
            if (authData == null)
            {
                apiResult.Message = selLangBLL.GetMsg("msg_ForceLogedout");
                return apiResult;
            }
            var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);

            #region - Check -
            if (!WithdrawalReqCheck(ref apiResult, withdrawalReqModel, authData, custInfo))
            {
                return apiResult;
            }
            #endregion

            #region - Check WithdrawalLimit -
            var withdrawalLimitInfo = paymentBLL.GetWithdrawalLimitInfo(new WithdrawalLimitInfoInput
            { SiteId = custInfo.SiteId, CurrencyId = custInfo.CurrencyId, VipLevel = custInfo.UserLevel });
            var dailyCustomerWithdrawalInfo = paymentBLL.GetDailyCustomerWithdrawalInfo(new DailyCustomerWithdrawalInfoInput { CustId = custInfo.CustId });
            if (withdrawalLimitInfo != null && withdrawalLimitInfo.WithdrawalMin > withdrawalReqModel.amount)
            {
                apiResult.Message = string.Format(selLangBLL.GetMsg("msg_LessThanWithdrawalMin"), withdrawalLimitInfo.WithdrawalMin);
                apiResult.ErrorCode = -5;
                return apiResult;
            }

            if (withdrawalLimitInfo != null && withdrawalLimitInfo.TotalWithdrawalMax != 0 &&
                dailyCustomerWithdrawalInfo.Amount + withdrawalReqModel.amount > withdrawalLimitInfo.TotalWithdrawalMax)
            {
                apiResult.Message = selLangBLL.GetMsg("msg_GreaterThanWithdrawalMax");
                apiResult.ErrorCode = -6;
                return apiResult;
            }

            if (withdrawalLimitInfo != null
                && withdrawalLimitInfo.Times != 0
                && dailyCustomerWithdrawalInfo.Count >= withdrawalLimitInfo.Times)
            {
                apiResult.Message = selLangBLL.GetMsg("msg_GreaterThanWithdrawalMax");
                apiResult.ErrorCode = -7;
                return apiResult;
            }

            #endregion

            #region - Check CustBankInfo -
            var custBankInfo = custInfoBLL.GetCustBankInfo(authData.CustId);
            if (custInfo.CurrencyId == (int)CurrencyId.USDT)
            {
                if (custBankInfo?.usdtDetail == null
                    || !custBankInfo.usdtDetail.Any()
                    || custBankInfo.usdtDetail.All(x => x.USDTAddress != withdrawalReqModel.AccountNo))
                {
                    return apiResult;
                }

                withdrawalReqModel.sysId = 0;
            }
            else
            {
                if (custBankInfo?.bankInfoDetail == null
                    || !custBankInfo.bankInfoDetail.Any()
                    || !custBankInfo.bankInfoDetail.Any(x =>
                        x.SysId == withdrawalReqModel.sysId && x.AccountNo == withdrawalReqModel.AccountNo))
                {
                    return apiResult;
                }
            }
            #endregion

            return await DoWithdrawalAsync(apiResult, authData, custInfo, withdrawalReqModel, custBankInfo, WithdrawalTypeStatus.LocalBank, null);
        }

        private async Task<ApiResult> DoWithdrawalAsync(ApiResult apiResult,
            AuthData authData,
            Customer custInfo,
            WithdrawalReqModel withdrawalReqModel,
            CustBankInfo custBankInfo,
            WithdrawalTypeStatus withdrawalType,
            CryptoWalletDetail cryptoDetail)
        {
            var result = 99;

            //GetWithdrawalAvailableBalance

            /*Temp will be Removed*/
            var balance = walletBLL.GetBalance(authData.CustId);
            var WithdrawalAvailableBalance = paymentBLL.GetWithdrawalAvailableBalance(authData.CustId);

            if (WithdrawalAvailableBalance < withdrawalReqModel.amount)
            {
                apiResult.Message = $"{(WithdrawalAvailableBalance < 0 ? 0 : WithdrawalAvailableBalance)}";
                apiResult.ErrorCode = -3;
                return apiResult;
            }

            if (balance < withdrawalReqModel.amount)
            {
                apiResult.Message = $"{withdrawalReqModel.amount - balance}";
                apiResult.ErrorCode = -2;
                return apiResult;
            }

            var transferToSabaResult = new ApiResult<TransferResult> { ErrorCode = 0 };

            /*Temp*/
            if (transferToSabaResult.ErrorCode == 0)
            {
                if (withdrawalReqModel.blackbox != null)
                {
                    _ = Task.Run(async () =>
                    {
                        var apiResultIovation = await iovationBLL.IovationChecks(authData.CustId, custInfo.CurrencyId, custInfo.UserName, withdrawalReqModel.blackbox, IovationType.withdrawal);
                        if (apiResultIovation.ErrorCode == 0 && apiResultIovation.Data != null)
                            iovationBLL.SetIovation(authData.CustId, IovationType.withdrawal, apiResultIovation.Data);

                    });
                }

                decimal afterAmount;
                string transId;
                switch (withdrawalType)
                {
                    case WithdrawalTypeStatus.LocalBank:
                        result = paymentBLL.WithdrawalReq(authData.CustId,
                        withdrawalReqModel.sysId,
                        custBankInfo.AccountName,
                        withdrawalReqModel.AccountNo,
                        withdrawalReqModel.IFSCCode,
                        withdrawalReqModel.amount,
                        withdrawalReqModel.memo, out afterAmount, out transId);
                        _logger.LogInformation($"AfterAmount={afterAmount}, TransId={transId}");
                        break;
                    case WithdrawalTypeStatus.Crypto:
                        result = paymentBLL.WithdrawalCryptoReq(authData.CustId,
                        custBankInfo.AccountName,
                        cryptoDetail,
                        withdrawalReqModel.AccountNo,
                        withdrawalReqModel.amount,
                        withdrawalReqModel.memo,
                        withdrawalReqModel.ExchangeRate,
                        withdrawalReqModel.ExchangeAmount, out afterAmount, out transId);
                        _logger.LogInformation($"AfterAmount={afterAmount}, TransId={transId}");
                        break;
                    case WithdrawalTypeStatus.EWallet:
                        result = paymentBLL.WithdrawalEWalletReq(authData.CustId,
                        custBankInfo.AccountName,
                        withdrawalReqModel.sysId,
                        withdrawalReqModel.AccountNo,
                        withdrawalReqModel.amount,
                        withdrawalReqModel.memo, out afterAmount, out transId);
                        _logger.LogInformation($"AfterAmount={afterAmount}, TransId={transId}");
                        break;
                }

                switch (result)
                {
                    case 0:
                    case 2:
                        apiResult.ErrorCode = result;
                        apiResult.Message = selLangBLL.GetMsg("msg_WithdrawalSuccessful");
                        break;
                    case -1:
                        apiResult.ErrorCode = 9000;
                        apiResult.Message = selLangBLL.GetMsg("msg_MustInteger");
                        break;
                    default:
                        {
                            apiResult.ErrorCode = 9000;
                            apiResult.Message = selLangBLL.GetMsg("msg_WithdrawalFailed");
                            break;
                        }
                }
            }
            else
            {
                apiResult.ErrorCode = transferToSabaResult.ErrorCode;
                apiResult.Message = selLangBLL.GetMsg("msg_WithdrawalNoFund");
                apiResult.RawErrorCode = transferToSabaResult.RawErrorCode;
                apiResult.RawMessage = transferToSabaResult.RawMessage;
            }

            return apiResult;
        }

        [HttpPost]
        public async Task<ApiResult> WithdrawalCryptoReqAsync(WithdrawalReqModel withdrawalReqModel)
        {
            var apiResult = new ApiResult(selLangBLL);
            var authData = authorizeBLL.GetAuthData(true);
            if (authData == null)
            {
                apiResult.Message = selLangBLL.GetMsg("msg_ForceLogedout");
                return apiResult;
            }

            if (withdrawalReqModel.ExchangeAmount <= 0)
            {
                apiResult.Message = "Please resubmit a withdrawal request"; // selLangBLL.GetMsg("msg_ResubmitWithdrawal"); 語系還沒給，先回英文
                apiResult.ErrorCode = -11;
                return apiResult;
            }

            var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);

            if (!WithdrawalReqCheck(ref apiResult, withdrawalReqModel, authData, custInfo))
            {
                return apiResult;
            }

            #region - Check USDTwithdrawalLimit -
            var dailyCustomerWithdrawalInfo = paymentBLL
                .GetDailyCustomerWithdrawalInfo(new DailyCustomerWithdrawalInfoInput { CustId = custInfo.CustId });
            var custBankInfo = custInfoBLL.GetCustBankInfo(authData.CustId);

            if (custBankInfo?.usdtDetail == null
                || !custBankInfo.usdtDetail.Any(x => x.USDTAddress == withdrawalReqModel.AccountNo
                    && x.CurrencyId == withdrawalReqModel.CurrencyId))
            {
                _logger.LogInformation(
                    $"CustBankInfo has no match USDTDetail address: {withdrawalReqModel.AccountNo}, currency: {withdrawalReqModel.CurrencyId}");
                return apiResult;
            }

            var usdtDetail = custBankInfo.usdtDetail
                .FirstOrDefault(x => x.USDTAddress == withdrawalReqModel.AccountNo
                                     && x.CurrencyId == withdrawalReqModel.CurrencyId);
            var usdtWithdrawalLimitInfo = paymentBLL.GetUSDTWithdrawalLimitInfoV2(new USDTWithdrawalLimitInfoInput
            {
                SiteId = custInfo.SiteId,
                CurrencyId = custInfo.CurrencyId,
                VipLevel = custInfo.UserLevel,
                CryptoCurrencyId = usdtDetail.CurrencyId,
                Network = usdtDetail.Network
            }).FirstOrDefault();

            var withdrawalLimitInfo = paymentBLL.GetWithdrawalLimitInfo(new WithdrawalLimitInfoInput
            { SiteId = custInfo.SiteId, CurrencyId = custInfo.CurrencyId, VipLevel = custInfo.UserLevel });

            if (usdtWithdrawalLimitInfo != null
                && usdtWithdrawalLimitInfo.WithdrawalMin > withdrawalReqModel.amount)
            {
                apiResult.Message = string.Format(selLangBLL.GetMsg("msg_LessThanWithdrawalMin"), usdtWithdrawalLimitInfo.WithdrawalMin);
                apiResult.ErrorCode = -5;
                return apiResult;
            }

            if (withdrawalLimitInfo.TotalWithdrawalMax != 0
                && dailyCustomerWithdrawalInfo.Amount + withdrawalReqModel.amount > withdrawalLimitInfo.TotalWithdrawalMax)
            {
                apiResult.Message = selLangBLL.GetMsg("msg_GreaterThanWithdrawalMax");
                apiResult.ErrorCode = -6;
                return apiResult;
            }

            if (withdrawalLimitInfo.Times != 0
                && dailyCustomerWithdrawalInfo.Count >= withdrawalLimitInfo.Times)
            {
                apiResult.Message = selLangBLL.GetMsg("msg_GreaterThanWithdrawalMax");
                apiResult.ErrorCode = -7;
                return apiResult;
            }
            #endregion

            return await DoWithdrawalAsync(apiResult, authData, custInfo, withdrawalReqModel, custBankInfo, WithdrawalTypeStatus.Crypto, usdtDetail);
        }

        private bool WithdrawalReqCheck(ref ApiResult apiResult, WithdrawalReqModel withdrawalReqModel, AuthData authData, Customer custInfo)
        {
            if (withdrawalReqModel.amount <= 0)
            {
                return false;
            }

            if (authData.CustId == 0)
            {
                apiResult.Message = selLangBLL.GetMsg("msg_DepositProcessLogin_ACLocked");
                apiResult.ErrorCode = -7;
                return false;
            }

            if (custInfo.CurrencyId == (int)CurrencyId.INR && !_custInfoService.GetPhoneVerified(custInfo.CustId).Result)
            {
                apiResult.Message = selLangBLL.GetMsg("msg_CompletePhoneNumber");
                apiResult.ErrorCode = -9;
                return false;
            }

            var check = paymentBLL.GetWithdrawalInfoCheck(new CustomerWithdrawalInfoInput { CustId = authData.CustId });
            if (check.WithdrawalExists)
            {
                apiResult.Message = selLangBLL.GetMsg("msg_WithdrawalExists");
                apiResult.ErrorCode = -8;
                return false;
            }

            if (custInfo.ActStatus == 1)
            {
                apiResult.Message = selLangBLL.GetMsg("msg_MustDeposit");
                apiResult.ErrorCode = -4;
                return false;
            }

            return true;
        }

        [HttpPost]
        public ApiResult<IEnumerable<BankInfoSys>> GetBankInfoSysList()
        {
            var apiResult = new ApiResult<IEnumerable<BankInfoSys>>();
            var authData = authorizeBLL.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                apiResult.Message = selLangBLL.GetMsg("msg_ForceLogedout");
                return apiResult;
            }
            var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);
            var result = bankInfoBLL.GetBankInfoSysListByCurrencyForCtrl(custInfo.CurrencyId)
                .ToList();

            if (custInfo.SiteId == (int)SiteId.Nova88
                && custInfo.CurrencyId == (int)CurrencyId.THB) // Nova88 泰國銀行名稱支援多語系
            {
                bankInfoBLL.BankNameLocalization(result, authData.LangId);
            }

            apiResult.Data = result;
            apiResult.ErrorCode = 0;
            return apiResult;
        }

        public class Payment
        {
            public int payId { get; set; }
        }
        [HttpPost]
        public ApiResult<IEnumerable<SysBankInfoFront>> GetOnlineBankInfoSysList(Payment payment)
        {
            var apiResult = new ApiResult<IEnumerable<SysBankInfoFront>>();
            var authData = authorizeBLL.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                apiResult.Message = selLangBLL.GetMsg("msg_ForceLogedout");
                return apiResult;
            }
            var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);
            var result = bankInfoBLL.GetOnlineBankInfoSysList(custInfo.CustId, payment.payId)
                .ToList();

            if (custInfo.SiteId == (int)SiteId.Nova88
                && custInfo.CurrencyId == (int)CurrencyId.THB) // Nova88 泰國銀行名稱支援多語系
            {
                bankInfoBLL.BankNameLocalization(result, authData.LangId);
            }

            apiResult.Data = result;
            apiResult.ErrorCode = 0;
            return apiResult;
        }

        [HttpPost]
        public ApiResult<IEnumerable<PaymentFront>> GetOnlinePaymentList()
        {
            var apiResult = new ApiResult<IEnumerable<PaymentFront>>();
            var authData = authorizeBLL.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                apiResult.Message = selLangBLL.GetMsg("msg_ForceLogedout");
                return apiResult;
            }
            var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);
            apiResult.Data = bankInfoBLL.GetOnlinePaymentList(custInfo.CustId);
            apiResult.ErrorCode = 0;
            return apiResult;
        }

        public class OnlineBankInfoReq
        {
            public int sysId { set; get; }
            public int payId { set; get; }
        }

        [HttpPost]
        public ApiResult<BankInfoPayFront> GetOnlineBankInfo(OnlineBankInfoReq onlineBankInfoReq)
        {
            var apiResult = new ApiResult<BankInfoPayFront>();
            var authData = authorizeBLL.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                apiResult.Message = selLangBLL.GetMsg("msg_ForceLogedout");
                return apiResult;
            }
            var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);
            apiResult.Data = bankInfoBLL.GetOnlineBankInfo(custInfo.CustId, onlineBankInfoReq.sysId, onlineBankInfoReq.payId);
            apiResult.ErrorCode = 0;
            return apiResult;
        }

        [HttpPost]
        public ApiResult<IEnumerable<PayGroup>> GetMobilePayGroupList()
        {
            var apiResult = new ApiResult<IEnumerable<PayGroup>>();
            var authData = authorizeBLL.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                apiResult.Message = selLangBLL.GetMsg("msg_ForceLogedout");
                return apiResult;
            }

            apiResult.Data = _payInfoBLL.GetPayGroupList(authData.CustId, DepositType.MobilePayment);
            apiResult.ErrorCode = 0;
            return apiResult;
        }

        public class PayInfoReq
        {
            public int groupId { set; get; }
        }

        [HttpPost]
        public ApiResult<IEnumerable<PayInfo>> GetMobilePayInfoList(PayInfoReq payInfoReq)
        {
            var apiResult = new ApiResult<IEnumerable<PayInfo>>();
            var authData = authorizeBLL.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                apiResult.Message = selLangBLL.GetMsg("msg_ForceLogedout");
                return apiResult;
            }

            apiResult.Data = _payInfoBLL.GetPayInfoList(authData.CustId, payInfoReq.groupId, DepositType.MobilePayment);
            apiResult.ErrorCode = 0;
            return apiResult;
        }

        [HttpPost]
        public ApiResult<IEnumerable<PayInfo>> GetCryptoPayInfoListAsync()
        {
            var apiResult = new ApiResult<IEnumerable<PayInfo>>();
            var authData = authorizeBLL.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                apiResult.Message = selLangBLL.GetMsg("msg_ForceLogedout");
                return apiResult;
            }

            apiResult.Data = cryptoPayInfoBLL.GetCryptoPayInfoList(authData.CustId);
            apiResult.ErrorCode = 0;
            return apiResult;
        }

        [HttpPost]
        public ApiResult<IEnumerable<LoadCardPayGroup>> GetLoadCardPayGroupList()
        {
            var apiResult = new ApiResult<IEnumerable<LoadCardPayGroup>>();
            var authData = authorizeBLL.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                apiResult.Message = selLangBLL.GetMsg("msg_ForceLogedout");
                return apiResult;
            }

            apiResult.Data = loadCardPayInfoBLL.GetLoadCardPayGroupList(authData.CustId);
            apiResult.ErrorCode = 0;
            return apiResult;
        }

        [HttpPost]
        public ApiResult<IEnumerable<PayInfo>> GetLoadCardPayInfoList(PayInfoReq payInfoReq)
        {
            var apiResult = new ApiResult<IEnumerable<PayInfo>>();
            var authData = authorizeBLL.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                apiResult.Message = selLangBLL.GetMsg("msg_ForceLogedout");
                return apiResult;
            }

            apiResult.Data = loadCardPayInfoBLL.GetLoadCardPayInfoList(authData.CustId, payInfoReq.groupId);
            apiResult.ErrorCode = 0;
            return apiResult;
        }

        public class DepositReceiptFile
        {
            public string TransId { get; set; }
            public string Receipt { get; set; }
        }

        [HttpPost]
        //[DisableFormValueModelBinding]
        public async Task<ApiResult> UploadReceipt(DepositReceiptFile depositReceipt)
        {
            var apiResult = new ApiResult();
            try
            {
                var authData = authorizeBLL.GetAuthData();
                if (authData is not { CustId: > 0 })
                {
                    apiResult.ErrorCode = -1;
                    apiResult.Message = selLangBLL.GetMsg("msg_ForceLogedout");
                    return apiResult;
                }

                if (depositReceipt.Receipt != null)
                {
                    var receipts = paymentBLL.GetDepositReceipt(depositReceipt.TransId, authData.CustId);
                    var receiptNo = receipts.Any() ? receipts.Max(x => x.ReceiptNo) + 1 : 1;

                    var x = depositReceipt.Receipt.Replace("data:image/png;base64,", "").Replace("data:image/jpg;base64,", "").Replace("data:image/jpeg;base64,", "");
                    var bytes = Convert.FromBase64String(x);
                    var stream = new MemoryStream(bytes);

                    var putRequest = new PutObjectRequest();
                    var key = $"deposit/{depositReceipt.TransId}_{receiptNo}.png";
                    putRequest.BucketName =  _paymentBucketName; // Your bucket name
                    putRequest.ContentType = "image/png";
                    putRequest.InputStream = stream;
                    putRequest.Key = key;
                    var putResponse = await _s3Helper.PutObjectAsync(putRequest);

                    if (putResponse)
                    {
                        var fileName = $"{depositReceipt.TransId}_{receiptNo}";
                        var result = await paymentBLL.SetDepositReceiptPic(authData.CustId, depositReceipt.TransId, receiptNo, fileName, authData.UserName);

                        if (result > 0)
                        {
                            apiResult.ErrorCode = 0;
                        }
                        else
                        {
                            apiResult.ErrorCode = -1;
                            apiResult.Message = "Upload fail.";
                        }
                    }
                }
                else
                {
                    apiResult.ErrorCode = -1;
                    apiResult.Message = "The file is not supported.";
                }
            }
            catch (Exception e)
            {
                apiResult.ErrorCode = -1;
                apiResult.Message = "The file is not supported.";
                _logger.LogError(e, GetCurrentAsyncMethod());
            }

            return apiResult;
        }

        public class DepositReq
        {
            public string TransId { get; set; }
        }
        public class DepositReceipt : DepositReq
        {
            public string RefNo { get; set; }
            public long DepositTime { get; set; }
        }

        [HttpPost]
        public ApiResult SetDepositReceipt(DepositReceipt depositReceipt)
        {
            var apiResult = new ApiResult();
            var authData = authorizeBLL.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                apiResult.ErrorCode = -1;
                apiResult.Message = selLangBLL.GetMsg("msg_ForceLogedout");
                return apiResult;
            }

            var result = paymentBLL.SetDepositReceipt(authData.CustId, depositReceipt.TransId, depositReceipt.RefNo, depositReceipt.DepositTime);
            if (result > 0)
            {
                apiResult.ErrorCode = 0;
            }
            else
            {
                apiResult.ErrorCode = -1;
            }
            return apiResult;
        }

        [HttpPost]
        public ApiResult SetDepositCancel(DepositReq depositReq)
        {
            var apiResult = new ApiResult();
            var authData = authorizeBLL.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                apiResult.ErrorCode = -1;
                apiResult.Message = selLangBLL.GetMsg("msg_ForceLogedout");
                return apiResult;
            }

            var result = paymentBLL.SetDepositCancel(authData.CustId, depositReq.TransId);
            if (result > 0)
            {
                apiResult.ErrorCode = 0;
            }
            else
            {
                apiResult.ErrorCode = -1;
            }
            return apiResult;
        }

        public class WithdrawaltReq
        {
            public string TransId { get; set; }
        }

        [HttpPost]
        public async Task<ApiResult> SetWithdrawalCancelAsync(WithdrawaltReq withdrawaltReq)
        {
            var apiResult = new ApiResult();
            var authData = authorizeBLL.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                apiResult.ErrorCode = -1;
                apiResult.Message = selLangBLL.GetMsg("msg_ForceLogedout");
                return apiResult;
            }

            var result = paymentBLL.SetWithdrawalCancel(authData.CustId, withdrawaltReq.TransId);
            if (result > 0)
            {
                apiResult.ErrorCode = 0;
            }
            else
            {
                apiResult.ErrorCode = -1;
            }
            return apiResult;
        }

        [HttpPost]
        public ApiResult GetStatement(HistoryReq historyReq)
        {
            var apiResult = new ApiListResult<Statement>();
            var authData = authorizeBLL.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                apiResult.Message = selLangBLL.GetMsg("msg_ForceLogedout");
                return apiResult;
            }

            apiResult.Data = paymentBLL.GetStatement(
                authData.CustId,
                historyReq.StartDate,
                historyReq.EndDate,
                historyReq.PageNumber,
                5,
                out var pageInfo);
            apiResult.PageNumber = pageInfo.PageNumber;
            apiResult.TotalPages = pageInfo.TotalPages;
            apiResult.RowCount = pageInfo.RowCount;
            apiResult.ErrorCode = 0;
            return apiResult;
        }

        public class TransferResult
        {
            public TransferResult()
            {
                Status = TransferStatus.Pending;
            }
            public TransferStatus Status { get; set; }
            public string ThirdId { get; set; }
            public decimal Difference { get; set; }
        }
        public enum TransferStatus
        {
            Success = 0, Fail = 1, Pending = 2, PendingD = 3
        }

        /// <summary>
        /// </summary>
        /// <returns>ErrorCode: 0=無須驗證KYC, 1=需驗證KYC, 2=等候KYC審核驗證通過中</returns>
        [HttpPost]
        public ApiResult KycCheck()
        {
            var result = new ApiResult();
            var authData = authorizeBLL.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                result.Message = selLangBLL.GetMsg("msg_ForceLogedout");
                return result;
            }
            result.ErrorCode = custInfoBLL.SubmittedCustPhotoId(authData.CustId);

            return result;
        }

        public class CustTpBetRecordListOneDayReq
        {
            public string SelDate { get; set; }
            public int PageNumber { get; set; }
            public int PageSize { get; set; }
        }
        [HttpPost]
        public ApiListResult<TpBetRecordListOneDay> GetCustTpBetRecordListOneDay(CustTpBetRecordListOneDayReq custTpBetRecordListOneDayReq)
        {
            var apiResult = new ApiListResult<TpBetRecordListOneDay>();
            var authData = authorizeBLL.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                apiResult.Message = selLangBLL.GetMsg("msg_ForceLogedout");
                return apiResult;
            }
            var PageSize = custTpBetRecordListOneDayReq.PageSize;
            var TotalPages = 0;
            var RowCount = 0;
            var PageNumber = custTpBetRecordListOneDayReq.PageNumber;

            apiResult.ErrorCode = 0;
            apiResult.Data = paymentBLL.GetCustTpBetRecordListOneDay(authData.CustId, custTpBetRecordListOneDayReq.SelDate, PageSize, ref PageNumber, ref TotalPages, ref RowCount);
            apiResult.PageNumber = PageNumber;
            apiResult.RowCount = RowCount;
            apiResult.TotalPages = TotalPages;

            return apiResult;
        }
        public class CustTpBetRecordListbyCategoryIdReq
        {
            public string SelDate { get; set; }
            public int CategoryId { get; set; }
            public int PageNumber { get; set; }
            public int PageSize { get; set; }
        }
        [HttpPost]
        public ApiListResult<TpBetRecordbyCategoryId> GetCustTpBetRecordListbyCategoryId(CustTpBetRecordListbyCategoryIdReq custTpBetRecordListbyCategoryIdReq)
        {
            var apiResult = new ApiListResult<TpBetRecordbyCategoryId>();
            var authData = authorizeBLL.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                apiResult.Message = selLangBLL.GetMsg("msg_ForceLogedout");
                return apiResult;
            }
            var PageSize = custTpBetRecordListbyCategoryIdReq.PageSize;
            var TotalPages = 0;
            var RowCount = 0;
            var PageNumber = custTpBetRecordListbyCategoryIdReq.PageNumber;

            apiResult.ErrorCode = 0;
            apiResult.Data = paymentBLL.GetCustTpBetRecordListbyCategoryId(authData.CustId, custTpBetRecordListbyCategoryIdReq.SelDate, custTpBetRecordListbyCategoryIdReq.CategoryId, PageSize, ref PageNumber, ref TotalPages, ref RowCount);
            apiResult.PageNumber = PageNumber;
            apiResult.RowCount = RowCount;
            apiResult.TotalPages = TotalPages;

            return apiResult;
        }
        public class CustTpBetRecordDetailListbyGameTypeReq
        {
            public string SelDate { get; set; }
            public int gameTypeId { get; set; }
            public int PageNumber { get; set; }
            public int PageSize { get; set; }
        }
        [HttpPost]
        public ApiListResult<TpBetRecordDetailbyGameType> GetCustTpBetRecordDetailListbyGameType(CustTpBetRecordDetailListbyGameTypeReq custTpBetRecordDetailListbyGameTypeReq)
        {
            var apiResult = new ApiListResult<TpBetRecordDetailbyGameType>();
            var authData = authorizeBLL.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                apiResult.Message = selLangBLL.GetMsg("msg_ForceLogedout");
                return apiResult;
            }
            var PageSize = custTpBetRecordDetailListbyGameTypeReq.PageSize;
            var TotalPages = 0;
            var RowCount = 0;
            var PageNumber = custTpBetRecordDetailListbyGameTypeReq.PageNumber;

            apiResult.ErrorCode = 0;
            apiResult.Data = paymentBLL.GetCustTpBetRecordDetailListbyGameType(authData.CustId, custTpBetRecordDetailListbyGameTypeReq.SelDate, custTpBetRecordDetailListbyGameTypeReq.gameTypeId, PageSize, ref PageNumber, ref TotalPages, ref RowCount);
            apiResult.PageNumber = PageNumber;
            apiResult.RowCount = RowCount;
            apiResult.TotalPages = TotalPages;

            return apiResult;
        }

        public class TpBetRecordDateRangeReq
        {
            public string StartDate { get; set; }
            public string EndDate { get; set; }
            public int PageNumber { get; set; }
            public int PageSize { get; set; }
        }
        [HttpPost]
        public ApiListResult<TpBetRecordListDateRange> CustTpBetRecordListDateRange(TpBetRecordDateRangeReq tpBetRecordDateRangeReq)
        {
            var apiResult = new ApiListResult<TpBetRecordListDateRange>
            {
                Data = new List<TpBetRecordListDateRange>()
            };
            var pageNumber = tpBetRecordDateRangeReq.PageNumber;
            var pageSize = tpBetRecordDateRangeReq.PageSize;
            var totalPages = 0;
            var rowCount = 0;
            try
            {
                var authData = authorizeBLL.GetAuthData();
                if (authData is not { CustId: > 0 })
                {
                    apiResult.Message = selLangBLL.GetMsg("msg_ForceLogedout");
                    return apiResult;
                }

                apiResult.Data = paymentBLL.GetCustTpBetRecordDateRange(authData.CustId,
                    tpBetRecordDateRangeReq.StartDate, tpBetRecordDateRangeReq.EndDate, pageSize, ref pageNumber,
                    ref totalPages, ref rowCount);

                apiResult.PageNumber = pageNumber;
                apiResult.RowCount = rowCount;
                apiResult.TotalPages = totalPages;
            }
            catch (Exception e)
            {
                _logger.LogError(e, GetCurrentMethod());
            }

            apiResult.ErrorCode = 0;
            return apiResult;
        }
        [HttpPost]
        public ApiResult<WithdrawalLimitOutput> GetWithdrawalLimit()
        {
            var apiResult = new ApiResult<WithdrawalLimitOutput>();
            var cryptoList = new List<CryptoWithdrawalLimitOutput>();
            var authData = authorizeBLL.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                apiResult.Message = selLangBLL.GetMsg("msg_ForceLogedout");
                return apiResult;
            }
            var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);

            #region Limitation

            var withdrawalLimitInfo = paymentBLL.GetWithdrawalLimitInfo(new WithdrawalLimitInfoInput
            {
                SiteId = custInfo.SiteId,
                CurrencyId = custInfo.CurrencyId,
                VipLevel = custInfo.UserLevel
            });

            var cryptoWithdrawalLimitInfo = paymentBLL.GetUSDTWithdrawalLimitInfoV2(new USDTWithdrawalLimitInfoInput
            {
                SiteId = custInfo.SiteId,
                CurrencyId = custInfo.CurrencyId,
                VipLevel = custInfo.UserLevel,
                CryptoCurrencyId = 0
            }).ToList();

            var eWalletWithdrawalLimitInfo = paymentBLL.GetEWalletWithdrawalLimitInfo(new WithdrawalLimitInfoInput
            {
                SiteId = custInfo.SiteId,
                CurrencyId = custInfo.CurrencyId
            });

            #endregion

            var dailyCustomerWithdrawalInfo =
                paymentBLL.GetDailyCustomerWithdrawalInfo(new DailyCustomerWithdrawalInfoInput { CustId = custInfo.CustId });

            decimal tmpWithdrawalAvailableAmount;

            #region crypto

            cryptoWithdrawalLimitInfo.ForEach(x =>
            {
                tmpWithdrawalAvailableAmount = x.WithdrawalMax == 0
                    ? dailyCustomerWithdrawalInfo.Amount
                    : x.WithdrawalMax - dailyCustomerWithdrawalInfo.Amount;

                cryptoList.Add(new CryptoWithdrawalLimitOutput
                {
                    CurrencyId = x.CurrencyId,
                    CurrencyName = x.CurrencyName,
                    Network = x.Network,
                    WithdrawalMin = x.WithdrawalMin,
                    Unlimited = x.WithdrawalMax == 0,
                    WithdrawalAvailableAmount = tmpWithdrawalAvailableAmount < 0
                        ? 0 : tmpWithdrawalAvailableAmount,
                    MaxTimes = x.Times,
                    WithdrawalTimes = dailyCustomerWithdrawalInfo.Count
                });
            });

            #endregion

            var output = withdrawalLimitInfo ?? new WithdrawalLimitInfoOutput()
            {
                WithdrawalMin = 0,
                WithdrawalMax = 0,
                TotalWithdrawalMax = 0,
                Times = 0
            };

            tmpWithdrawalAvailableAmount = output.TotalWithdrawalMax == 0
                ? dailyCustomerWithdrawalInfo.Amount
                : output.TotalWithdrawalMax - dailyCustomerWithdrawalInfo.Amount;

            #region eWallet

            var eWallet = eWalletWithdrawalLimitInfo ?? new EWalletWithdrawalLimitInfoOutput()
            {
                WithdrawalMin = 0,
                WithdrawalMax = 0,
                Status = 0
            };

            #endregion

            var eWalletMin = Math.Max(output.WithdrawalMin, eWallet.WithdrawalMin);
            var eWalletMax = Math.Min(output.TotalWithdrawalMax, eWallet.WithdrawalMax);

            var data = new WithdrawalLimitOutput
            {
                WithdrawalMin = output.WithdrawalMin,
                Unlimited = output.TotalWithdrawalMax == 0,
                WithdrawalAvailableAmount = tmpWithdrawalAvailableAmount < 0
                    ? 0 : tmpWithdrawalAvailableAmount,
                MaxTimes = output.Times,
                WithdrawalTimes = dailyCustomerWithdrawalInfo.Count,
                USDTList = cryptoList,
                EWalletWithdrawalMin = eWalletMin,
                EWalletUnlimited = eWallet.WithdrawalMax == 0,
                EWalletWithdrawalAvailableAmount = eWalletMax,
                EWalletStatus = eWallet.Status
            };

            apiResult.Data = data;
            apiResult.ErrorCode = 0;

            return apiResult;
        }

        public class GetExchangeReq
        {
            public string CurrencyName { get; set; }
            public string Network { get; set; }
            public decimal Amount { get; set; }
            public ExchangeTypeEnum Type { get; set; }
        }

        [HttpPost]
        public async Task<ApiResult<ExchangeInfo>> GetExchangeInfoAsync(GetExchangeReq getExchangeReq)
        {
            var apiResult = new ApiResult<ExchangeInfo>();
            var authData = authorizeBLL.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                apiResult.Message = selLangBLL.GetMsg("msg_ForceLogedout");
                return apiResult;
            }

            var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);
            var channelName =
                getExchangeReq.CurrencyName == getExchangeReq.Network
                    ? getExchangeReq.CurrencyName
                    : $"{getExchangeReq.CurrencyName}-{getExchangeReq.Network}";

            var result = await cryptoPayInfoBLL.GetExchangeInfoAsync(custInfo.SiteId, custInfo.CurrencyId,
                getExchangeReq.Amount, channelName, getExchangeReq.Type);

            if (result is { Data: { } })
            {
                apiResult.Data = new ExchangeInfo
                {
                    Rate = result.Data.Rate,
                    To_Amount = result.Data.ExchangeAmount,
                    Updated_timestamp = result.Data.Timestamp
                };
                apiResult.ErrorCode = result.ErrorCode;
            }
            return apiResult;
        }

        [HttpPost]
        public ApiResult<CryptoInfoData> GetSupportCryptoInfo()
        {
            var authData = authorizeBLL.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                return new ApiResult<CryptoInfoData>
                {
                    Message = selLangBLL.GetMsg("msg_ForceLogedout")
                };
            }

            var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);
            var result = paymentBLL.GetSupportCryptoInfoWithCache(custInfo.SiteId);

            return new ApiResult<CryptoInfoData>
            {
                Data = result,
                ErrorCode = 0
            };
        }

        public class CheckDepositSpecialCodeReq
        {
            public string SpecialCode { get; set; }
            public int DeviceType { get; set; }
        }
        [HttpPost]
        public ApiResult<SpecialCodeResponse> CheckDepositSpecialCode(CheckDepositSpecialCodeReq request)
        {
            var result = new ApiResult<SpecialCodeResponse>();

            try
            {
                var authData = authorizeBLL.GetAuthData();
                if (authData is not { CustId: > 0 })
                {
                    result.Message = selLangBLL.GetMsg("msg_ForceLogedout");
                    return result;
                }
                var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);
                var custInfoProfile = custInfoBLL.GetCustInfoProfile(authData.CustId);

                if (!custInfoProfile.PhoneVerified
                    || string.IsNullOrEmpty(custInfoProfile.AccountName)
                    || string.IsNullOrEmpty(custInfoProfile.Birth_y)
                    || string.IsNullOrEmpty(custInfoProfile.Birth_m)
                    || string.IsNullOrEmpty(custInfoProfile.Birth_d))
                {
                    result.ErrorCode = 2000;
                    result.Message = selLangBLL.GetMsg("msg_OTPHint");
                    return result;
                }

                var apiUrl = $"{_bonusServiceUrl}CheckDepositSpecialCode";
                var req = new SpecialCodeRequest
                {
                    CustId = authData.CustId,
                    DeviceType = request.DeviceType,
                    SpecialCode = request.SpecialCode,
                    SiteId = custInfo.SiteId
                };

                result = _courierAdapter.MyPostAsync<SpecialCodeResponse, SpecialCodeRequest>(apiUrl, req).Result;

                result.Message = result.ErrorCode switch
                {
                    (int)SpecialCode.SpecialCodeErrorForCheck => selLangBLL.GetMsg("msg_SpecialCodeErr"),
                    (int)SpecialCode.SpecialCodeVipLevelsOrBlockTagInvalid => selLangBLL.GetMsg("msg_SpecialCodeVipLevelsOrBlockTagInvalid"),
                    (int)SpecialCode.PromoCodeHasBeenUsed => selLangBLL.GetMsg("msg_PromoCodeHasBeenUsed"),
                    9000 => selLangBLL.GetMsg("msg_Ineligible"),
                    _ => result.Message
                };
            }
            catch (Exception e)
            {
                result.ErrorCode = 9000;
                result.Message = selLangBLL.GetMsg("msg_Ineligible");
                _logger.LogError(e, GetCurrentMethod());
            }

            return result;
        }

        [HttpPost]
        public ApiResult<IEnumerable<PayGroup>> GetQRPayGroupList()
        {
            var result = new ApiResult<IEnumerable<PayGroup>>();
            var authData = authorizeBLL.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                result.Message = selLangBLL.GetMsg("msg_ForceLogedout");
                return result;
            }

            result.Data = _payInfoBLL.GetPayGroupList(authorizeBLL.GetAuthData().CustId, DepositType.QrPayPayment);
            result.ErrorCode = 0;
            return result;
        }

        [HttpPost]
        public ApiResult<IEnumerable<PayInfo>> GetQRPayInfoList(PayInfoReq payInfoReq)
        {
            var result = new ApiResult<IEnumerable<PayInfo>>();
            var authData = authorizeBLL.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                result.Message = selLangBLL.GetMsg("msg_ForceLogedout");
                return result;
            }

            result.Data = _payInfoBLL.GetPayInfoList(authorizeBLL.GetAuthData().CustId, payInfoReq.groupId, DepositType.QrPayPayment);
            result.ErrorCode = 0;
            return result;
        }

        [HttpPost]
        public ApiResult<IEnumerable<SysBankInfoFront>> GetAutoChannelBankGroupList()
        {
            var apiResult = new ApiResult<IEnumerable<SysBankInfoFront>>();
            var authData = authorizeBLL.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                apiResult.Message = selLangBLL.GetMsg("msg_ForceLogedout");
                return apiResult;
            }

            var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);
            var vipLevel = custInfoBLL.GetPointLevelWithCache(authData.CustId);
            var result = bankInfoBLL.GetAutoChannelBankGroupList(custInfo, vipLevel).ToList();
            if (custInfo.SiteId == (int)SiteId.Nova88 && custInfo.CurrencyId == (int)CurrencyId.THB) // Nova88 泰國銀行名稱支援多語系
            {
                bankInfoBLL.BankNameLocalization(result, authData.LangId);
            }

            apiResult.Data = result;
            apiResult.ErrorCode = 0;
            return apiResult;
        }

        /// <summary>
        /// 取得存款Line頻道
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ApiResult<LineChannel> GetLineChannelForDpeosit()
        {
            var authData = authorizeBLL.GetAuthData();
            if (authData is not { CustId: > 0 })
                return new ApiResult<LineChannel>
                {
                    ErrorCode = -1,
                    Message = "Cust is Invalid.Please Contact to CSD."
                };

            var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);
            if (custInfo == null)
                return new ApiResult<LineChannel>
                {
                    ErrorCode = -1,
                    Message = "Cust Not Found.Please Contact to CSD."
                };

            var result = bankInfoBLL.GetLineChannel(custInfo.SiteId, custInfo.CurrencyId);
            if (result == null)
                return new ApiResult<LineChannel>
                {
                    ErrorCode = -1,
                    Message = "Link Not Found.Please Contact to CSD."
                };

            return new ApiResult<LineChannel>
            {
                Data = result,
                ErrorCode = 0
            }; ;
        }

        [HttpPost]
        public ApiResult<IEnumerable<EWalletInfoSys>> GetEWalletInfoSysList()
        {
            var apiResult = new ApiResult<IEnumerable<EWalletInfoSys>>();
            var authData = authorizeBLL.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                apiResult.Message = selLangBLL.GetMsg("msg_ForceLogedout");
                return apiResult;
            }
            var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);
            var result = bankInfoBLL.GetEWalletInfoSysListByCurrency(custInfo.CurrencyId)
                .ToList();

            apiResult.Data = result;
            apiResult.ErrorCode = 0;
            return apiResult;
        }

        private bool HasThisManyDecimalPlacesOrLess(decimal value, int decimalPlaces)
        {
            return (decimal.Round(value, decimalPlaces) == value);
        }

        [HttpPost]
        public async Task<ApiResult> WithdrawalEWalletReqAsync(WithdrawalReqModel withdrawalReqModel)
        {
            var apiResult = new ApiResult(selLangBLL);
            var authData = authorizeBLL.GetAuthData(true);
            if (authData == null)
            {
                apiResult.Message = selLangBLL.GetMsg("msg_ForceLogedout");
                return apiResult;
            }
            var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);

            #region - Check -
            if (!WithdrawalReqCheck(ref apiResult, withdrawalReqModel, authData, custInfo))
            {
                return apiResult;
            }
            #endregion

            #region - Check WithdrawalLimit -
            var withdrawalLimitInfo = paymentBLL.GetWithdrawalLimitInfo(new WithdrawalLimitInfoInput
            { SiteId = custInfo.SiteId, CurrencyId = custInfo.CurrencyId, VipLevel = custInfo.UserLevel });
            var eWalletWithdrawalLimitInfo = paymentBLL.GetEWalletWithdrawalLimitInfo(new WithdrawalLimitInfoInput { SiteId = custInfo.SiteId, CurrencyId = custInfo.CurrencyId });
            var dailyCustomerWithdrawalInfo = paymentBLL.GetDailyCustomerWithdrawalInfo(new DailyCustomerWithdrawalInfoInput { CustId = custInfo.CustId });

            var withdrawLimit = WithdrawLimitFactory.Create(WithdrawalTypeStatus.EWallet);
            var errorCode = withdrawLimit.CheckWithdrawLimit(eWalletWithdrawalLimitInfo, withdrawalLimitInfo, dailyCustomerWithdrawalInfo, withdrawalReqModel.amount);

            if (errorCode != 0)
            {
                apiResult.ErrorCode = errorCode;
                switch (errorCode)
                {
                    case -8:
                        apiResult.Message = selLangBLL.GetMsg("msg_EWalletIsMaintenance");
                        break;
                    case -5:
                        apiResult.Message = string.Format(selLangBLL.GetMsg("msg_LessThanWithdrawalMin"), withdrawLimit.MinAmount);
                        break;
                    case -9:
                        apiResult.Message = selLangBLL.GetMsg("msg_NotInLimit");
                        break;
                    case -6:
                    case -7:
                        apiResult.Message = selLangBLL.GetMsg("msg_GreaterThanWithdrawalMax");
                        break;
                }

                return apiResult;
            }
            #endregion

            #region - Check EWalletInfo -
            var eWalletInfo = custInfoBLL.GetCustEWalletInfo(authData.CustId).ToList();
            var custBankInfo = custInfoBLL.GetCustBankInfo(authData.CustId);
            if (eWalletInfo.Count == 0 || eWalletInfo.Find(x => x.AccountNo == withdrawalReqModel.AccountNo && x.SysId == withdrawalReqModel.sysId) == null)
            {
                return apiResult;
            }
            #endregion
            return await DoWithdrawalAsync(apiResult, authData, custInfo, withdrawalReqModel, custBankInfo, WithdrawalTypeStatus.EWallet, null);
        }
        
        [HttpPost]
        public ApiResult<IEnumerable<WithdrawalBlockSetting>> GetWithdrawalBlockSetting()
        {
            var apiResult = new ApiResult<IEnumerable<WithdrawalBlockSetting>>();
            var authData = authorizeBLL.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                apiResult.Message = selLangBLL.GetMsg("msg_ForceLogedout");
                return apiResult;
            }

            var custInfo =  custInfoBLL.GetCustInfoWithCache(authData.CustId);
            var data = 
                _payInfoBLL.GetWithdrawalBlockSettingWithCache(custInfo.SiteId, custInfo.CurrencyId);

            apiResult.Data = data;
            apiResult.ErrorCode = 0;
            
            return apiResult;
        }


        [HttpPost]
        public ApiResult<IDictionary<string, PayMethod>> GetPayInfoListAll()
        {
            var authData = authorizeBLL.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                return new ApiResult<IDictionary<string, PayMethod>>
                {
                    Message = selLangBLL.GetMsg("msg_ForceLogedout")
                };
            }
            
            return new ApiResult<IDictionary<string, PayMethod>>
            {
                Data = _payInfoBLL.GetPayInfoListAll(authData.CustId),
                ErrorCode = 0
            };
        }
        
        /// <summary>
        /// 舊的site沒有改用GetPayInfoListAll，等都轉移去新平台再一起拔掉
        /// </summary>
        [HttpPost]
        public ApiResult<IDictionary<string, PayMethod>> GetPayInfoQuickDeposit()
        {
            var authData = authorizeBLL.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                return new ApiResult<IDictionary<string, PayMethod>>
                {
                    Message = selLangBLL.GetMsg("msg_ForceLogedout")
                };
            }
            
            return new ApiResult<IDictionary<string, PayMethod>>
            {
                Data = _payInfoBLL.GetGernericPayMethods(
                    new Dictionary<DepositType, string>
                    {
                        { DepositType.QuickTransfer, "QuickTransfer" }
                    }, authData.CustId),
                
                ErrorCode = 0
            };
        }
    }
}
