using Amazon.S3.Model;
using MemberSite.Service.BLL;
using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using static MemberSite.Extensions.MethodExtension;

namespace MemberSite.Controllers.Api
{
    [Authorize]
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly BankInfoOffBLL _bankInfoOffBll;
        private readonly AuthorizeBLL _authorizeBll;
        private readonly PaymentBLL _paymentBll;
        private readonly CustInfoBll _custInfoBll;
        private readonly BankInfoBLL _bankInfoBll;
        private readonly SelLangBLL _selLangBll;
        private readonly WalletBLL _walletBll;
        private readonly IovationBLL _iovationBll;
        private readonly CryptoPayInfoBll _cryptoPayInfoBll;
        private readonly LoadCardPayInfoBLL _loadCardPayInfoBll;
        private readonly PayInfoBLL _payInfoBll;
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
            _bankInfoOffBll = provider.GetRequiredService<BankInfoOffBLL>();
            _authorizeBll = provider.GetRequiredService<AuthorizeBLL>();
            _paymentBll = provider.GetRequiredService<PaymentBLL>();
            _custInfoBll = provider.GetRequiredService<CustInfoBll>();
            _bankInfoBll = provider.GetRequiredService<BankInfoBLL>();
            
            _selLangBll = provider.GetRequiredService<SelLangBLL>();
            _walletBll = provider.GetRequiredService<WalletBLL>();
            _iovationBll = provider.GetRequiredService<IovationBLL>();
            _cryptoPayInfoBll = provider.GetRequiredService<CryptoPayInfoBll>();
            _loadCardPayInfoBll = provider.GetRequiredService<LoadCardPayInfoBLL>();
            
            _payInfoBll = provider.GetRequiredService<PayInfoBLL>();
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
            var authData = _authorizeBll.GetAuthData();

            if (authData is not { CustId: > 0 })
            {
                return new ApiResult<IEnumerable<OfflineBankInfoFront>>
                {
                    Message = _selLangBll.GetMsg("msg_ForceLogedout")
                };
            }

            return new ApiResult<IEnumerable<OfflineBankInfoFront>>
            {
                Data = _bankInfoOffBll.GetBankInfoOffListWithCache(authData.CustId),
                ErrorCode = 0
            };
        }

        [HttpPost]
        public ApiResult<IEnumerable<OfflineBankInfoFront>> GetBankInfoOffListStatus()
        {
            var authData = _authorizeBll.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                return new ApiResult<IEnumerable<OfflineBankInfoFront>>
                {
                    Message = _selLangBll.GetMsg("msg_ForceLogedout")
                };
            }

            return new ApiResult<IEnumerable<OfflineBankInfoFront>>
            {
                Data = _bankInfoOffBll.GetBankInfoOffListWithCache(authData.CustId),
                ErrorCode = 0
            };
        }

        [HttpPost]
        public ApiResult<IEnumerable<WithdrawalHistory>> GetWithdrawalInfoRecent()
        {
            var apiResult = new ApiResult<IEnumerable<WithdrawalHistory>>();
            var authData = _authorizeBll.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                apiResult.Message = _selLangBll.GetMsg("msg_ForceLogedout");
                return apiResult;
            }

            apiResult.Data = _paymentBll.GetWithdrawalInfoRecent(authData.CustId);
            apiResult.ErrorCode = 0;
            return apiResult;
        }

        [HttpPost]
        public ApiResult<IEnumerable<DepositInfoHistory>> GetDepositInfoRecent()
        {
            var apiResult = new ApiResult<IEnumerable<DepositInfoHistory>>();
            var authData = _authorizeBll.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                apiResult.Message = _selLangBll.GetMsg("msg_ForceLogedout");
                return apiResult;
            }

            apiResult.Data = _paymentBll.GetDepositInfoRecent(authData.CustId);
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
            var authData = _authorizeBll.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                apiResult.Message = _selLangBll.GetMsg("msg_ForceLogedout");
                return apiResult;
            }
            var pageNumber = historyReq.PageNumber;
            apiResult.Data = _paymentBll.GetDepositInfoHistory(authData.CustId, historyReq.StartDate, historyReq.EndDate, ref pageNumber, out var totalPages, out var rowCount);
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
            var authData = _authorizeBll.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                apiResult.Message = _selLangBll.GetMsg("msg_ForceLogedout");
                return apiResult;
            }
            var pageNumber = historyReq.PageNumber;
            apiResult.Data = _paymentBll.GetWithdrawalInfoHistory(authData.CustId, historyReq.StartDate, historyReq.EndDate, ref pageNumber, out var totalPages, out var rowCount);
            apiResult.PageNumber = pageNumber;
            apiResult.TotalPages = totalPages;
            apiResult.RowCount = rowCount;
            apiResult.ErrorCode = 0;
            return apiResult;
        }
        public class DepositReqWireTransferReq : ILocalBankReq
        {
            public int SysId { set; get; }
            public decimal Amount { set; get; }
            public string Memo { set; get; }

            public int BankId { set; get; }
            public bool HasPromotion { set; get; }
        }

        /// <summary>
        /// Local Bank Transfer
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ApiResult<DepositRep3>> DepositReqWireTransferAsync(DepositReqWireTransferReq req)
        {
            var authData = _authorizeBll.GetAuthData();

            var checkResult = CheckDeposit(authData, req.Amount,
                req.HasPromotion);

            if (checkResult.ErrorCode != 0)
            {
                return new ApiResult<DepositRep3>
                {
                    ErrorCode = checkResult.ErrorCode,
                    Message = checkResult.Message
                };
            }

            var apiResult = await DepositByLocalBankAsync(authData.CustId, req);
            
            return apiResult;
        }

        private ApiResult CheckDeposit(AuthData authData, decimal amount, bool hasPromotion)
        {
            if (authData is not { CustId: > 0 })
            {
                return new ApiResult
                {
                    ErrorCode = -1,
                    Message = _selLangBll.GetMsg("msg_ForceLogedout")
                };
            }

            //check AccountName first
            var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);

            if (custInfo.SiteId == (int)SiteId.Goal123 && _custInfoBll.GetCustBankInfo(authData.CustId) == null)
            {
                return new ApiResult
                {
                    ErrorCode = -2,
                    Message = _selLangBll.GetMsg("msg_NeedAccountName")
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
                var custInfoProfile = _custInfoBll.GetCustInfoProfile(authData.CustId);
                if (!custInfoProfile.PhoneVerified
                    || string.IsNullOrEmpty(custInfoProfile.AccountName)
                    || string.IsNullOrEmpty(custInfoProfile.Birth_y)
                    || string.IsNullOrEmpty(custInfoProfile.Birth_m)
                    || string.IsNullOrEmpty(custInfoProfile.Birth_d))
                {
                    return new ApiResult
                    {
                        ErrorCode = 2000,
                        Message = _selLangBll.GetMsg("msg_OTPHint")
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
            public DepositType DepositType { set; get; }
        }

        [HttpPost]
        public ApiResult<LocalPayResp> DepositReqLocalPay(DepositReqLocalPayReq req)
        {
            var authData = _authorizeBll.GetAuthData();
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
            
            var custId = authData.CustId;
            var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
            var result = DepositByLocalPayment(custId, req.GroupId, req.Amount);

            if (result.ErrorCode != (int) ErrorCode.Success)
                return result;
            
            var transId = result.Data.TransId;
            
            if (hasPromotion)
            {
                if (!_promotionBll.SetDepositPromotionQueue(
                        transId,
                        req.BonusCode,
                        custId,
                        custInfo.SiteId,
                        custInfo.UserName,
                        custInfo.CurrencyId,
                        1,
                        0))
                {
                    _logger.LogError($"TransId:{transId} SetDepositPromotionQueue Failed");
                }
            }
            
            return result;
        }

        [HttpPost]
        public ApiResult<DepositRep3> GetLocalBankInfo(DepositReqWireTransferReq depositReqWireTransferReq)
        {
            var apiResult = new ApiResult<DepositRep3>();
            var authData = _authorizeBll.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                apiResult.Message = _selLangBll.GetMsg("msg_ForceLogedout");
                return apiResult;
            }

            apiResult.ErrorCode = _bankInfoOffBll.LocalBankinfo(authData.CustId, depositReqWireTransferReq.SysId, depositReqWireTransferReq.Memo, out var depositRep3);
            if (apiResult.ErrorCode == (int)ErrorCode.Success)
            {
                apiResult.Data = depositRep3;
            }

            return apiResult;
        }

        public class WithdrawalReqModel
        {
            public int SysId { set; get; }
            public string AccountNo { set; get; }
            public string IfscCode { get; set; }
            public decimal Amount { set; get; }
            public string Memo { set; get; }
            public string Blackbox { get; set; }
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
            var apiResult = new ApiResult(_selLangBll);
            var authData = _authorizeBll.GetAuthData(true);
            if (authData == null)
            {
                apiResult.Message = _selLangBll.GetMsg("msg_ForceLogedout");
                return apiResult;
            }
            var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);

            #region - Check -
            if (!WithdrawalReqCheck(ref apiResult, withdrawalReqModel, authData, custInfo))
            {
                return apiResult;
            }
            #endregion

            #region - Check WithdrawalLimit -
            var withdrawalLimitInfo = _paymentBll.GetWithdrawalLimitInfo(new WithdrawalLimitInfoInput
            { SiteId = custInfo.SiteId, CurrencyId = custInfo.CurrencyId, VipLevel = custInfo.UserLevel });
            var dailyCustomerWithdrawalInfo = _paymentBll.GetDailyCustomerWithdrawalInfo(new DailyCustomerWithdrawalInfoInput { CustId = custInfo.CustId });
            if (withdrawalLimitInfo != null && withdrawalLimitInfo.WithdrawalMin > withdrawalReqModel.Amount)
            {
                apiResult.Message = string.Format(_selLangBll.GetMsg("msg_LessThanWithdrawalMin"), withdrawalLimitInfo.WithdrawalMin);
                apiResult.ErrorCode = -5;
                return apiResult;
            }

            if (withdrawalLimitInfo != null && withdrawalLimitInfo.TotalWithdrawalMax != 0 &&
                dailyCustomerWithdrawalInfo.Amount + withdrawalReqModel.Amount > withdrawalLimitInfo.TotalWithdrawalMax)
            {
                apiResult.Message = _selLangBll.GetMsg("msg_GreaterThanWithdrawalMax");
                apiResult.ErrorCode = -6;
                return apiResult;
            }

            if (withdrawalLimitInfo != null
                && withdrawalLimitInfo.Times != 0
                && dailyCustomerWithdrawalInfo.Count >= withdrawalLimitInfo.Times)
            {
                apiResult.Message = _selLangBll.GetMsg("msg_GreaterThanWithdrawalMax");
                apiResult.ErrorCode = -7;
                return apiResult;
            }

            #endregion

            #region - Check CustBankInfo -
            var custBankInfo = _custInfoBll.GetCustBankInfo(authData.CustId);
            if (custInfo.CurrencyId == (int)CurrencyId.USDT)
            {
                if (custBankInfo?.usdtDetail == null
                    || !custBankInfo.usdtDetail.Any()
                    || custBankInfo.usdtDetail.All(x => x.USDTAddress != withdrawalReqModel.AccountNo))
                {
                    return apiResult;
                }

                withdrawalReqModel.SysId = 0;
            }
            else
            {
                if (custBankInfo?.bankInfoDetail == null
                    || !custBankInfo.bankInfoDetail.Any()
                    || !custBankInfo.bankInfoDetail.Any(x =>
                        x.SysId == withdrawalReqModel.SysId && x.AccountNo == withdrawalReqModel.AccountNo))
                {
                    return apiResult;
                }
            }
            #endregion

            return DoWithdrawal(apiResult, authData, custInfo, withdrawalReqModel, custBankInfo, WithdrawalTypeStatus.LocalBank, null);
        }

        private ApiResult DoWithdrawal(ApiResult apiResult,
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
            var balance = _walletBll.GetBalance(authData.CustId);
            var withdrawalAvailableBalance = _paymentBll.GetWithdrawalAvailableBalance(authData.CustId);

            if (withdrawalAvailableBalance < withdrawalReqModel.Amount)
            {
                apiResult.Message = $"{(withdrawalAvailableBalance < 0 ? 0 : withdrawalAvailableBalance)}";
                apiResult.ErrorCode = -3;
                return apiResult;
            }

            if (balance < withdrawalReqModel.Amount)
            {
                apiResult.Message = $"{withdrawalReqModel.Amount - balance}";
                apiResult.ErrorCode = -2;
                return apiResult;
            }

            if (withdrawalReqModel.Blackbox != null)
            {
                _ = Task.Run(async () =>
                {
                    var apiResultIovation = await _iovationBll.IovationChecks(authData.CustId, custInfo.CurrencyId, custInfo.UserName, withdrawalReqModel.Blackbox, IovationType.withdrawal);
                    if (apiResultIovation.ErrorCode == 0 && apiResultIovation.Data != null)
                        _iovationBll.SetIovation(authData.CustId, IovationType.withdrawal, apiResultIovation.Data);

                });
            }

            decimal afterAmount;
            string transId;
            switch (withdrawalType)
            {
                case WithdrawalTypeStatus.LocalBank:
                    result = _paymentBll.WithdrawalReq(authData.CustId,
                    withdrawalReqModel.SysId,
                    custBankInfo.AccountName,
                    withdrawalReqModel.AccountNo,
                    withdrawalReqModel.IfscCode,
                    withdrawalReqModel.Amount,
                    withdrawalReqModel.Memo, out afterAmount, out transId);
                    _logger.LogInformation($"AfterAmount={afterAmount}, TransId={transId}");
                    break;
                case WithdrawalTypeStatus.Crypto:
                    result = _paymentBll.WithdrawalCryptoReq(authData.CustId,
                    custBankInfo.AccountName,
                    cryptoDetail,
                    withdrawalReqModel.AccountNo,
                    withdrawalReqModel.Amount,
                    withdrawalReqModel.Memo,
                    withdrawalReqModel.ExchangeRate,
                    withdrawalReqModel.ExchangeAmount, out afterAmount, out transId);
                    _logger.LogInformation($"AfterAmount={afterAmount}, TransId={transId}");
                    break;
                case WithdrawalTypeStatus.EWallet:
                    result = _paymentBll.WithdrawalEWalletReq(authData.CustId,
                    custBankInfo.AccountName,
                    withdrawalReqModel.SysId,
                    withdrawalReqModel.AccountNo,
                    withdrawalReqModel.Amount,
                    withdrawalReqModel.Memo, out afterAmount, out transId);
                    _logger.LogInformation($"AfterAmount={afterAmount}, TransId={transId}");
                    break;
            }

            switch (result)
                {
                    case 0:
                    case 2:
                        apiResult.ErrorCode = result;
                        apiResult.Message = _selLangBll.GetMsg("msg_WithdrawalSuccessful");
                        break;
                    case -1:
                        apiResult.ErrorCode = 9000;
                        apiResult.Message = _selLangBll.GetMsg("msg_MustInteger");
                        break;
                    default:
                        {
                            apiResult.ErrorCode = 9000;
                            apiResult.Message = _selLangBll.GetMsg("msg_WithdrawalFailed");
                            break;
                        }
                }
           
            return apiResult;
        }

        [HttpPost]
        public ApiResult WithdrawalCryptoReq(WithdrawalReqModel withdrawalReqModel)
        {
            var apiResult = new ApiResult(_selLangBll);
            var authData = _authorizeBll.GetAuthData(true);
            if (authData == null)
            {
                apiResult.Message = _selLangBll.GetMsg("msg_ForceLogedout");
                return apiResult;
            }

            if (withdrawalReqModel.ExchangeAmount <= 0)
            {
                apiResult.Message = "Please resubmit a withdrawal request"; // selLangBLL.GetMsg("msg_ResubmitWithdrawal"); 語系還沒給，先回英文
                apiResult.ErrorCode = -11;
                return apiResult;
            }

            var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);

            if (!WithdrawalReqCheck(ref apiResult, withdrawalReqModel, authData, custInfo))
            {
                return apiResult;
            }

            #region - Check USDTwithdrawalLimit -
            var dailyCustomerWithdrawalInfo = _paymentBll
                .GetDailyCustomerWithdrawalInfo(new DailyCustomerWithdrawalInfoInput { CustId = custInfo.CustId });
            var custBankInfo = _custInfoBll.GetCustBankInfo(authData.CustId);

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
            var usdtWithdrawalLimitInfo = _paymentBll.GetUSDTWithdrawalLimitInfoV2(new USDTWithdrawalLimitInfoInput
            {
                SiteId = custInfo.SiteId,
                CurrencyId = custInfo.CurrencyId,
                VipLevel = custInfo.UserLevel,
                CryptoCurrencyId = usdtDetail.CurrencyId,
                Network = usdtDetail.Network
            }).FirstOrDefault();

            var withdrawalLimitInfo = _paymentBll.GetWithdrawalLimitInfo(new WithdrawalLimitInfoInput
            { SiteId = custInfo.SiteId, CurrencyId = custInfo.CurrencyId, VipLevel = custInfo.UserLevel });

            if (usdtWithdrawalLimitInfo != null
                && usdtWithdrawalLimitInfo.WithdrawalMin > withdrawalReqModel.Amount)
            {
                apiResult.Message = string.Format(_selLangBll.GetMsg("msg_LessThanWithdrawalMin"), usdtWithdrawalLimitInfo.WithdrawalMin);
                apiResult.ErrorCode = -5;
                return apiResult;
            }

            if (withdrawalLimitInfo.TotalWithdrawalMax != 0
                && dailyCustomerWithdrawalInfo.Amount + withdrawalReqModel.Amount > withdrawalLimitInfo.TotalWithdrawalMax)
            {
                apiResult.Message = _selLangBll.GetMsg("msg_GreaterThanWithdrawalMax");
                apiResult.ErrorCode = -6;
                return apiResult;
            }

            if (withdrawalLimitInfo.Times != 0
                && dailyCustomerWithdrawalInfo.Count >= withdrawalLimitInfo.Times)
            {
                apiResult.Message = _selLangBll.GetMsg("msg_GreaterThanWithdrawalMax");
                apiResult.ErrorCode = -7;
                return apiResult;
            }
            #endregion

            return DoWithdrawal(apiResult, authData, custInfo, withdrawalReqModel, custBankInfo, WithdrawalTypeStatus.Crypto, usdtDetail);
        }

        private bool WithdrawalReqCheck(ref ApiResult apiResult, WithdrawalReqModel withdrawalReqModel, AuthData authData, Customer custInfo)
        {
            if (withdrawalReqModel.Amount <= 0)
            {
                return false;
            }

            if (authData.CustId == 0)
            {
                apiResult.Message = _selLangBll.GetMsg("msg_DepositProcessLogin_ACLocked");
                apiResult.ErrorCode = -7;
                return false;
            }

            if (custInfo.CurrencyId == (int)CurrencyId.INR && !_custInfoService.GetPhoneVerified(custInfo.CustId).Result)
            {
                apiResult.Message = _selLangBll.GetMsg("msg_CompletePhoneNumber");
                apiResult.ErrorCode = -9;
                return false;
            }

            var check = _paymentBll.GetWithdrawalInfoCheck(new CustomerWithdrawalInfoInput { CustId = authData.CustId });
            if (check.WithdrawalExists)
            {
                apiResult.Message = _selLangBll.GetMsg("msg_WithdrawalExists");
                apiResult.ErrorCode = -8;
                return false;
            }

            if (custInfo.ActStatus == 1)
            {
                apiResult.Message = _selLangBll.GetMsg("msg_MustDeposit");
                apiResult.ErrorCode = -4;
                return false;
            }

            return true;
        }

        [HttpPost]
        public ApiResult<IEnumerable<BankInfoSys>> GetBankInfoSysList()
        {
            var apiResult = new ApiResult<IEnumerable<BankInfoSys>>();
            var authData = _authorizeBll.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                apiResult.Message = _selLangBll.GetMsg("msg_ForceLogedout");
                return apiResult;
            }
            var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
            var result = _bankInfoBll.GetBankInfoSysListByCurrencyForCtrl(custInfo.CurrencyId)
                .ToList();

            if (custInfo.SiteId == (int)SiteId.Nova88
                && custInfo.CurrencyId == (int)CurrencyId.THB) // Nova88 泰國銀行名稱支援多語系
            {
                _bankInfoBll.BankNameLocalization(result, authData.LangId);
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
            var authData = _authorizeBll.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                apiResult.Message = _selLangBll.GetMsg("msg_ForceLogedout");
                return apiResult;
            }
            var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
            var result = _bankInfoBll.GetOnlineBankInfoSysList(custInfo.CustId, payment.payId)
                .ToList();

            if (custInfo.SiteId == (int)SiteId.Nova88
                && custInfo.CurrencyId == (int)CurrencyId.THB) // Nova88 泰國銀行名稱支援多語系
            {
                _bankInfoBll.BankNameLocalization(result, authData.LangId);
            }

            apiResult.Data = result;
            apiResult.ErrorCode = 0;
            return apiResult;
        }

        [HttpPost]
        public ApiResult<IEnumerable<PaymentFront>> GetOnlinePaymentList()
        {
            var apiResult = new ApiResult<IEnumerable<PaymentFront>>();
            var authData = _authorizeBll.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                apiResult.Message = _selLangBll.GetMsg("msg_ForceLogedout");
                return apiResult;
            }
            var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
            apiResult.Data = _bankInfoBll.GetOnlinePaymentList(custInfo.CustId, DepositType.OnlineBanking);
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
            var authData = _authorizeBll.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                apiResult.Message = _selLangBll.GetMsg("msg_ForceLogedout");
                return apiResult;
            }
            var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
            apiResult.Data = _bankInfoBll.GetOnlineBankInfo(custInfo.CustId, onlineBankInfoReq.sysId, onlineBankInfoReq.payId);
            apiResult.ErrorCode = 0;
            return apiResult;
        }

        [HttpPost]
        public ApiResult<IEnumerable<PayGroup>> GetMobilePayGroupList()
        {
            var apiResult = new ApiResult<IEnumerable<PayGroup>>();
            var authData = _authorizeBll.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                apiResult.Message = _selLangBll.GetMsg("msg_ForceLogedout");
                return apiResult;
            }

            apiResult.Data = _payInfoBll.GetPayGroupList(authData.CustId, DepositType.MobilePayment);
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
            var authData = _authorizeBll.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                apiResult.Message = _selLangBll.GetMsg("msg_ForceLogedout");
                return apiResult;
            }

            apiResult.Data = _payInfoBll.GetPayInfoList(authData.CustId, payInfoReq.groupId, DepositType.MobilePayment);
            apiResult.ErrorCode = 0;
            return apiResult;
        }

        [HttpPost]
        public ApiResult<IEnumerable<PayInfo>> GetCryptoPayInfoListAsync()
        {
            var apiResult = new ApiResult<IEnumerable<PayInfo>>();
            var authData = _authorizeBll.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                apiResult.Message = _selLangBll.GetMsg("msg_ForceLogedout");
                return apiResult;
            }

            apiResult.Data = _cryptoPayInfoBll.GetCryptoPayInfoList(authData.CustId);
            apiResult.ErrorCode = 0;
            return apiResult;
        }

        [HttpPost]
        public ApiResult<IEnumerable<LoadCardPayGroup>> GetLoadCardPayGroupList()
        {
            var apiResult = new ApiResult<IEnumerable<LoadCardPayGroup>>();
            var authData = _authorizeBll.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                apiResult.Message = _selLangBll.GetMsg("msg_ForceLogedout");
                return apiResult;
            }

            apiResult.Data = _loadCardPayInfoBll.GetLoadCardPayGroupList(authData.CustId);
            apiResult.ErrorCode = 0;
            return apiResult;
        }

        [HttpPost]
        public ApiResult<IEnumerable<PayInfo>> GetLoadCardPayInfoList(PayInfoReq payInfoReq)
        {
            var apiResult = new ApiResult<IEnumerable<PayInfo>>();
            var authData = _authorizeBll.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                apiResult.Message = _selLangBll.GetMsg("msg_ForceLogedout");
                return apiResult;
            }

            apiResult.Data = _loadCardPayInfoBll.GetLoadCardPayInfoList(authData.CustId, payInfoReq.groupId);
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
                var authData = _authorizeBll.GetAuthData();
                if (authData is not { CustId: > 0 })
                {
                    apiResult.ErrorCode = -1;
                    apiResult.Message = _selLangBll.GetMsg("msg_ForceLogedout");
                    return apiResult;
                }

                if (depositReceipt.Receipt != null)
                {
                    var receipts = _paymentBll.GetDepositReceipt(depositReceipt.TransId, authData.CustId);
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
                        var result = await _paymentBll.SetDepositReceiptPic(authData.CustId, depositReceipt.TransId, receiptNo, fileName, authData.UserName);

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
            var authData = _authorizeBll.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                apiResult.ErrorCode = -1;
                apiResult.Message = _selLangBll.GetMsg("msg_ForceLogedout");
                return apiResult;
            }

            var result = _paymentBll.SetDepositReceipt(authData.CustId, depositReceipt.TransId, depositReceipt.RefNo, depositReceipt.DepositTime);
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
            var authData = _authorizeBll.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                apiResult.ErrorCode = -1;
                apiResult.Message = _selLangBll.GetMsg("msg_ForceLogedout");
                return apiResult;
            }

            var result = _paymentBll.SetDepositCancel(authData.CustId, depositReq.TransId);
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
            var authData = _authorizeBll.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                apiResult.ErrorCode = -1;
                apiResult.Message = _selLangBll.GetMsg("msg_ForceLogedout");
                return apiResult;
            }

            var result = _paymentBll.SetWithdrawalCancel(authData.CustId, withdrawaltReq.TransId);
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
            var authData = _authorizeBll.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                apiResult.Message = _selLangBll.GetMsg("msg_ForceLogedout");
                return apiResult;
            }

            apiResult.Data = _paymentBll.GetStatement(
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

        /// <summary>
        /// </summary>
        /// <returns>ErrorCode: 0=無須驗證KYC, 1=需驗證KYC, 2=等候KYC審核驗證通過中</returns>
        [HttpPost]
        public ApiResult KycCheck()
        {
            var result = new ApiResult();
            var authData = _authorizeBll.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                result.Message = _selLangBll.GetMsg("msg_ForceLogedout");
                return result;
            }
            result.ErrorCode = _custInfoBll.SubmittedCustPhotoId(authData.CustId);

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
            var authData = _authorizeBll.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                apiResult.Message = _selLangBll.GetMsg("msg_ForceLogedout");
                return apiResult;
            }
            var PageSize = custTpBetRecordListOneDayReq.PageSize;
            var TotalPages = 0;
            var RowCount = 0;
            var PageNumber = custTpBetRecordListOneDayReq.PageNumber;

            apiResult.ErrorCode = 0;
            apiResult.Data = _paymentBll.GetCustTpBetRecordListOneDay(authData.CustId, custTpBetRecordListOneDayReq.SelDate, PageSize, ref PageNumber, ref TotalPages, ref RowCount);
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
            var authData = _authorizeBll.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                apiResult.Message = _selLangBll.GetMsg("msg_ForceLogedout");
                return apiResult;
            }
            var PageSize = custTpBetRecordListbyCategoryIdReq.PageSize;
            var TotalPages = 0;
            var RowCount = 0;
            var PageNumber = custTpBetRecordListbyCategoryIdReq.PageNumber;

            apiResult.ErrorCode = 0;
            apiResult.Data = _paymentBll.GetCustTpBetRecordListbyCategoryId(authData.CustId, custTpBetRecordListbyCategoryIdReq.SelDate, custTpBetRecordListbyCategoryIdReq.CategoryId, PageSize, ref PageNumber, ref TotalPages, ref RowCount);
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
            var authData = _authorizeBll.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                apiResult.Message = _selLangBll.GetMsg("msg_ForceLogedout");
                return apiResult;
            }
            var PageSize = custTpBetRecordDetailListbyGameTypeReq.PageSize;
            var TotalPages = 0;
            var RowCount = 0;
            var PageNumber = custTpBetRecordDetailListbyGameTypeReq.PageNumber;

            apiResult.ErrorCode = 0;
            apiResult.Data = _paymentBll.GetCustTpBetRecordDetailListbyGameType(authData.CustId, custTpBetRecordDetailListbyGameTypeReq.SelDate, custTpBetRecordDetailListbyGameTypeReq.gameTypeId, PageSize, ref PageNumber, ref TotalPages, ref RowCount);
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
                var authData = _authorizeBll.GetAuthData();
                if (authData is not { CustId: > 0 })
                {
                    apiResult.Message = _selLangBll.GetMsg("msg_ForceLogedout");
                    return apiResult;
                }

                apiResult.Data = _paymentBll.GetCustTpBetRecordDateRange(authData.CustId,
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
            var authData = _authorizeBll.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                apiResult.Message = _selLangBll.GetMsg("msg_ForceLogedout");
                return apiResult;
            }
            var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);

            #region Limitation

            var withdrawalLimitInfo = _paymentBll.GetWithdrawalLimitInfo(new WithdrawalLimitInfoInput
            {
                SiteId = custInfo.SiteId,
                CurrencyId = custInfo.CurrencyId,
                VipLevel = custInfo.UserLevel
            });

            var cryptoWithdrawalLimitInfo = _paymentBll.GetUSDTWithdrawalLimitInfoV2(new USDTWithdrawalLimitInfoInput
            {
                SiteId = custInfo.SiteId,
                CurrencyId = custInfo.CurrencyId,
                VipLevel = custInfo.UserLevel,
                CryptoCurrencyId = 0
            }).ToList();

            var eWalletWithdrawalLimitInfo = _paymentBll.GetEWalletWithdrawalLimitInfo(new WithdrawalLimitInfoInput
            {
                SiteId = custInfo.SiteId,
                CurrencyId = custInfo.CurrencyId
            });

            #endregion

            var dailyCustomerWithdrawalInfo =
                _paymentBll.GetDailyCustomerWithdrawalInfo(new DailyCustomerWithdrawalInfoInput { CustId = custInfo.CustId });

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
            var authData = _authorizeBll.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                apiResult.Message = _selLangBll.GetMsg("msg_ForceLogedout");
                return apiResult;
            }

            var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
            var channelName =
                getExchangeReq.CurrencyName == getExchangeReq.Network
                    ? getExchangeReq.CurrencyName
                    : $"{getExchangeReq.CurrencyName}-{getExchangeReq.Network}";

            var result = await _cryptoPayInfoBll.GetExchangeInfoAsync(custInfo.SiteId, custInfo.CurrencyId,
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
            var authData = _authorizeBll.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                return new ApiResult<CryptoInfoData>
                {
                    Message = _selLangBll.GetMsg("msg_ForceLogedout")
                };
            }

            var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
            var result = _paymentBll.GetSupportCryptoInfoWithCache(custInfo.SiteId);

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
                var authData = _authorizeBll.GetAuthData();
                if (authData is not { CustId: > 0 })
                {
                    result.Message = _selLangBll.GetMsg("msg_ForceLogedout");
                    return result;
                }
                var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
                var custInfoProfile = _custInfoBll.GetCustInfoProfile(authData.CustId);

                if (!custInfoProfile.PhoneVerified
                    || string.IsNullOrEmpty(custInfoProfile.AccountName)
                    || string.IsNullOrEmpty(custInfoProfile.Birth_y)
                    || string.IsNullOrEmpty(custInfoProfile.Birth_m)
                    || string.IsNullOrEmpty(custInfoProfile.Birth_d))
                {
                    result.ErrorCode = 2000;
                    result.Message = _selLangBll.GetMsg("msg_OTPHint");
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
                    (int)SpecialCode.SpecialCodeErrorForCheck => _selLangBll.GetMsg("msg_SpecialCodeErr"),
                    (int)SpecialCode.SpecialCodeVipLevelsOrBlockTagInvalid => _selLangBll.GetMsg("msg_SpecialCodeVipLevelsOrBlockTagInvalid"),
                    (int)SpecialCode.PromoCodeHasBeenUsed => _selLangBll.GetMsg("msg_PromoCodeHasBeenUsed"),
                    9000 => _selLangBll.GetMsg("msg_Ineligible"),
                    _ => result.Message
                };
            }
            catch (Exception e)
            {
                result.ErrorCode = 9000;
                result.Message = _selLangBll.GetMsg("msg_Ineligible");
                _logger.LogError(e, GetCurrentMethod());
            }

            return result;
        }

        [HttpPost]
        public ApiResult<IEnumerable<PayGroup>> GetQRPayGroupList()
        {
            var result = new ApiResult<IEnumerable<PayGroup>>();
            var authData = _authorizeBll.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                result.Message = _selLangBll.GetMsg("msg_ForceLogedout");
                return result;
            }

            result.Data = _payInfoBll.GetPayGroupList(_authorizeBll.GetAuthData().CustId, DepositType.QrPayPayment);
            result.ErrorCode = 0;
            return result;
        }

        [HttpPost]
        public ApiResult<IEnumerable<PayInfo>> GetQRPayInfoList(PayInfoReq payInfoReq)
        {
            var result = new ApiResult<IEnumerable<PayInfo>>();
            var authData = _authorizeBll.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                result.Message = _selLangBll.GetMsg("msg_ForceLogedout");
                return result;
            }

            result.Data = _payInfoBll.GetPayInfoList(_authorizeBll.GetAuthData().CustId, payInfoReq.groupId, DepositType.QrPayPayment);
            result.ErrorCode = 0;
            return result;
        }

        [HttpPost]
        public ApiResult<IEnumerable<SysBankInfoFront>> GetAutoChannelBankGroupList()
        {
            var apiResult = new ApiResult<IEnumerable<SysBankInfoFront>>();
            var authData = _authorizeBll.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                apiResult.Message = _selLangBll.GetMsg("msg_ForceLogedout");
                return apiResult;
            }

            var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
            var vipLevel = _custInfoBll.GetPointLevelWithCache(authData.CustId);
            var result = _bankInfoBll.GetAutoChannelBankGroupList(custInfo, vipLevel).ToList();
            if (custInfo.SiteId == (int)SiteId.Nova88 && custInfo.CurrencyId == (int)CurrencyId.THB) // Nova88 泰國銀行名稱支援多語系
            {
                _bankInfoBll.BankNameLocalization(result, authData.LangId);
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
            var authData = _authorizeBll.GetAuthData();
            if (authData is not { CustId: > 0 })
                return new ApiResult<LineChannel>
                {
                    ErrorCode = -1,
                    Message = "Cust is Invalid.Please Contact to CSD."
                };

            var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
            if (custInfo == null)
                return new ApiResult<LineChannel>
                {
                    ErrorCode = -1,
                    Message = "Cust Not Found.Please Contact to CSD."
                };

            var result = _bankInfoBll.GetLineChannel(custInfo.SiteId, custInfo.CurrencyId);
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
            var authData = _authorizeBll.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                apiResult.Message = _selLangBll.GetMsg("msg_ForceLogedout");
                return apiResult;
            }
            var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
            var result = _bankInfoBll.GetEWalletInfoSysListByCurrency(custInfo.CurrencyId)
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
            var apiResult = new ApiResult(_selLangBll);
            var authData = _authorizeBll.GetAuthData(true);
            if (authData == null)
            {
                apiResult.Message = _selLangBll.GetMsg("msg_ForceLogedout");
                return apiResult;
            }
            var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);

            #region - Check -
            if (!WithdrawalReqCheck(ref apiResult, withdrawalReqModel, authData, custInfo))
            {
                return apiResult;
            }
            #endregion

            #region - Check WithdrawalLimit -
            var withdrawalLimitInfo = _paymentBll.GetWithdrawalLimitInfo(new WithdrawalLimitInfoInput
            { SiteId = custInfo.SiteId, CurrencyId = custInfo.CurrencyId, VipLevel = custInfo.UserLevel });
            var eWalletWithdrawalLimitInfo = _paymentBll.GetEWalletWithdrawalLimitInfo(new WithdrawalLimitInfoInput { SiteId = custInfo.SiteId, CurrencyId = custInfo.CurrencyId });
            var dailyCustomerWithdrawalInfo = _paymentBll.GetDailyCustomerWithdrawalInfo(new DailyCustomerWithdrawalInfoInput { CustId = custInfo.CustId });

            var withdrawLimit = WithdrawLimitFactory.Create(WithdrawalTypeStatus.EWallet);
            var errorCode = withdrawLimit.CheckWithdrawLimit(eWalletWithdrawalLimitInfo, withdrawalLimitInfo, dailyCustomerWithdrawalInfo, withdrawalReqModel.Amount);

            if (errorCode != 0)
            {
                apiResult.ErrorCode = errorCode;
                switch (errorCode)
                {
                    case -8:
                        apiResult.Message = _selLangBll.GetMsg("msg_EWalletIsMaintenance");
                        break;
                    case -5:
                        apiResult.Message = string.Format(_selLangBll.GetMsg("msg_LessThanWithdrawalMin"), withdrawLimit.MinAmount);
                        break;
                    case -9:
                        apiResult.Message = _selLangBll.GetMsg("msg_NotInLimit");
                        break;
                    case -6:
                    case -7:
                        apiResult.Message = _selLangBll.GetMsg("msg_GreaterThanWithdrawalMax");
                        break;
                }

                return apiResult;
            }
            #endregion

            #region - Check EWalletInfo -
            var eWalletInfo = _custInfoBll.GetCustEWalletInfo(authData.CustId).ToList();
            var custBankInfo = _custInfoBll.GetCustBankInfo(authData.CustId);
            if (eWalletInfo.Count == 0 || eWalletInfo.Find(x => x.AccountNo == withdrawalReqModel.AccountNo && x.SysId == withdrawalReqModel.SysId) == null)
            {
                return apiResult;
            }
            #endregion
            return DoWithdrawal(apiResult, authData, custInfo, withdrawalReqModel, custBankInfo, WithdrawalTypeStatus.EWallet, null);
        }
        
        [HttpPost]
        public ApiResult<IEnumerable<WithdrawalBlockSetting>> GetWithdrawalBlockSetting()
        {
            var apiResult = new ApiResult<IEnumerable<WithdrawalBlockSetting>>();
            var authData = _authorizeBll.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                apiResult.Message = _selLangBll.GetMsg("msg_ForceLogedout");
                return apiResult;
            }

            var custInfo =  _custInfoBll.GetCustInfoWithCache(authData.CustId);
            var data = 
                _payInfoBll.GetWithdrawalBlockSettingWithCache(custInfo.SiteId, custInfo.CurrencyId);

            apiResult.Data = data;
            apiResult.ErrorCode = 0;
            
            return apiResult;
        }


        [HttpPost]
        public ApiResult<IDictionary<string, PayMethod>> GetPayInfoListAll()
        {
            var authData = _authorizeBll.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                return new ApiResult<IDictionary<string, PayMethod>>
                {
                    Message = _selLangBll.GetMsg("msg_ForceLogedout")
                };
            }
            
            return new ApiResult<IDictionary<string, PayMethod>>
            {
                Data = _payInfoBll.GetPayInfoListAll(authData.CustId),
                ErrorCode = 0
            };
        }
        
        /// <summary>
        /// 舊的site沒有改用GetPayInfoListAll，等都轉移去新平台再一起拔掉
        /// </summary>
        [HttpPost]
        public ApiResult<IDictionary<string, PayMethod>> GetPayInfoQuickTransfer()
        {
            var authData = _authorizeBll.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                return new ApiResult<IDictionary<string, PayMethod>>
                {
                    Message = _selLangBll.GetMsg("msg_ForceLogedout")
                };
            }
            
            return new ApiResult<IDictionary<string, PayMethod>>
            {
                Data = _payInfoBll.GetOnlineBankMethods(
                    new []{ DepositType.QuickTransfer }, authData.CustId),
                
                ErrorCode = 0
            };
        }
        
        public class DepositReqLocalPaymentReq : ILocalBankReq
        {
            public int ChannelId { set; get; }
            public decimal Amount { set; get; }
            public string BonusCode { set; get; }
            public DepositType DepositType { set; get; }
            
            // LOCAL BANK
            public int SysId { set; get; }
            public int BankId { set; get; }
        }
        
        private interface ILocalBankReq
        {
            public decimal Amount { set; get; }
            public int SysId { set; get; }
            public int BankId { set; get; }
        }
        
        [HttpPost]
        public async Task<ApiResult<object>> DepositReqLocalPaymentAsync(DepositReqLocalPaymentReq req)
        {
            var authData = _authorizeBll.GetAuthData();
            var hasPromotion = _promotionBll.VerifyBonusCode(req.BonusCode);
            var checkResult = CheckDeposit(authData, req.Amount, hasPromotion);

            if (checkResult.ErrorCode != 0)
            {
                return new ApiResult<object>
                {
                    ErrorCode = checkResult.ErrorCode,
                    RawMessage = checkResult.Message
                };
            }
            
            var custId = authData.CustId;
            var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
            string transId;
            ApiResult<object> result;

            switch (req.DepositType)
            {
                case DepositType.LocalBank:
                    var localBankResult = await DepositByLocalBankAsync(authData.CustId, req);
                    transId = localBankResult.Data?.TransId;

                    result = new ApiResult<object>(localBankResult.ErrorCode,
                        localBankResult.Message,
                        new {TransId = transId});
                    
                    break;
                
                case DepositType.CryptoWallet:
                    var cryptoWalletResult = await DepositByCryptoWalletAsync(authData.CustId, req.ChannelId * -1, req.Amount);
                    transId = cryptoWalletResult.Data?.ThirdId;

                    result = new ApiResult<object>(cryptoWalletResult.ErrorCode,
                        cryptoWalletResult.Message,
                        cryptoWalletResult.Data);
                    
                    break;
                
                default:
                    var localPaymentResult = DepositByLocalPayment(custId, req.ChannelId * -1, req.Amount);
                    transId = localPaymentResult.Data?.TransId;

                    result = new ApiResult<object>(localPaymentResult.ErrorCode,
                        localPaymentResult.Message,
                        localPaymentResult.Data);
                    
                    break;
            }

            if (result.ErrorCode != (int)ErrorCode.Success)
                return result;

            if (hasPromotion)
            {
                if (!_promotionBll.SetDepositPromotionQueue(
                        transId,
                        req.BonusCode,
                        custId,
                        custInfo.SiteId,
                        custInfo.UserName,
                        custInfo.CurrencyId,
                        1,
                        0))
                {
                    _logger.LogError($"TransId:{transId} SetDepositPromotionQueue Failed");
                }
            }
            
            return result;
        }

        [HttpPost]
        public ApiResult<IDictionary<string, PayMethod>> GetPayInfoChatTransfer()
        {
            var authData = _authorizeBll.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                return new ApiResult<IDictionary<string, PayMethod>>
                {
                    Message = _selLangBll.GetMsg("msg_ForceLogedout")
                };
            }

            return new ApiResult<IDictionary<string, PayMethod>>
            {
                Data = _payInfoBll.GetGenericPayMethods(
                    new[] { DepositType.ChatTransfer }, authData.CustId),

                ErrorCode = 0
            };
        }
        private ApiResult<LocalPayResp> DepositByLocalPayment(int custId, int groupId, decimal amount)
        {
            var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
            var localPayment = _bankInfoOffBll.GetLocalPaymentSettingWithCache()
                .FirstOrDefault(x => x.GroupId == groupId);

            if (localPayment == null)
            {
                return new ApiResult<LocalPayResp>
                {
                    ErrorCode = -1,
                    RawMessage = "No available channels."
                };
            }

            var cards = _bankInfoOffBll.GetOfflineBankCardListWithCache(custId)
                .Where(x => x.GroupIds.Contains(groupId)
                    && amount >= x.MinAmount
                    && amount <= x.MaxAmount)
                .ToList();

            if (!cards.Any())
            {
                return new ApiResult<LocalPayResp>
                {
                    ErrorCode = -2,
                    RawMessage = "No available channels."
                };
            }

            // 隨機配卡
            var random = new Random();
            var card = cards[random.Next(cards.Count - 1)];
            var (errorCode, message) = _paymentBll.DepositReqLocalBank(new DepositReqLocalPayModel
            {
                Amount = amount,
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

            if (localPayment.Type == DepositType.QrPayPayment && custInfo.CurrencyId == (int)CurrencyId.VND)
            {
                var vietQr = PaymentBLL.GenerateVietQr(bankAccount, transId, amount);
                return new ApiResult<LocalPayResp>
                {
                    Data = new LocalPayResp
                    {
                        GenerateQr = true,
                        Amount = amount,
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
                    Amount = amount,
                    ReturnUrl = card.QRCodeUrl,
                    TransId = transId,
                    Code = code,
                    BankLogo = card.BankLogo
                },
                ErrorCode = 0
            };
        }

        private async Task<ApiResult<PayResult>> DepositByCryptoWalletAsync(int custId, int cryptoCurrencyId, decimal amount)
        {
            var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
            var cryptoInfos = _bankInfoOffBll.GetFilteredOfflineCryptoPayInfo(custInfo.CustId)
                .Where(x => (int)x.CryptoCurrencyId == cryptoCurrencyId
                            && (x.MinAmount == 0 || amount >= x.MinAmount)
                            && (x.MaxAmount == 0 || amount <= x.MaxAmount))
                .ToList();

            if (!cryptoInfos.Any())
            {
                var msg = "no channels available";
            }

            // 隨機配卡
            var random = new Random();
            var cryptoInfo = cryptoInfos[random.Next(cryptoInfos.Count - 1)];

            var (errorCode, message, payResult) = await _cryptoPayInfoBll.DepositReqCryptoLocalPayAsync(
                custInfo, amount, cryptoInfo);

            if (errorCode != (int)ErrorCode.Success)
            {
                _logger.LogError($"({errorCode}) {message}");
                
            }

            return new ApiResult<PayResult>
            {
                Data = payResult,
                ErrorCode = errorCode
            };
        }
        
        private async Task<ApiResult<DepositRep3>> DepositByLocalBankAsync(int custId, ILocalBankReq req)
        {
            var key = $"Deposit:LocalBank:{custId}:{req.SysId}";
            var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
            const int sec = 5;
            if (!await _cacheHelper.SetNxAsync(key, "", TimeSpan.FromSeconds(sec)))
            {
                _logger.LogError($"Duplicate request within {sec} sec, UserName={custInfo.UserName}");

                return new ApiResult<DepositRep3>
                {
                    ErrorCode = -1,
                    Message = "Duplicate request."
                };
            }
            
            if (_bankInfoOffBll.GetCustOffDepositSysId(custId).Contains(req.SysId))
            {
                return new ApiResult<DepositRep3>
                {
                    ErrorCode = -3,
                    Message = "A deposit with the same bank already exists. Please complete it before trying again."
                };
            }
            
            var errorCode = _paymentBll.DepositReqLocalBank(
                custId,
                req.SysId,
                req.BankId, 
                req.Amount,
                string.Empty,
                out var depositRep);
            
            return new ApiResult<DepositRep3>
            {
                ErrorCode = errorCode,
                Data = depositRep
            };
        }
    }
}
