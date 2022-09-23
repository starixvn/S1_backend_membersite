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
        private readonly CourierAdapter courierAdapter;
        private readonly SelLangBLL selLangBLL;
        private readonly WalletBLL walletBLL;
        private readonly IovationBLL iovationBLL;
        private readonly CryptoPayInfoBll cryptoPayInfoBLL;
        private readonly LoadCardPayInfoBLL loadCardPayInfoBLL;
        private readonly string tpApiUrl;
        private readonly int mainWalletType;
        private readonly FileNameBLL _fileNameBll;
        private readonly ImageEditorBLL _imageEditorBll;
        private readonly PayInfoBLL _payInfoBLL;
        private readonly CustInfoService _custInfoService;
        private readonly ILogger<PaymentController> _logger;
        private readonly CourierAdapter _courierAdapter;
        private readonly string _bonusServiceUrl;

        public PaymentController(IServiceProvider provider)
        {
            bankInfoOffBLL = provider.GetRequiredService<BankInfoOffBLL>();
            authorizeBLL = provider.GetRequiredService<AuthorizeBLL>();
            paymentBLL = provider.GetRequiredService<PaymentBLL>();
            custInfoBLL = provider.GetRequiredService<CustInfoBll>();
            bankInfoBLL = provider.GetRequiredService<BankInfoBLL>();
            courierAdapter = provider.GetRequiredService<CourierAdapter>();
            selLangBLL = provider.GetRequiredService<SelLangBLL>();
            walletBLL = provider.GetRequiredService<WalletBLL>();
            iovationBLL = provider.GetRequiredService<IovationBLL>();
            cryptoPayInfoBLL = provider.GetRequiredService<CryptoPayInfoBll>();
            loadCardPayInfoBLL = provider.GetRequiredService<LoadCardPayInfoBLL>();
            _fileNameBll = provider.GetRequiredService<FileNameBLL>();
            _imageEditorBll = provider.GetRequiredService<ImageEditorBLL>();
            _payInfoBLL = provider.GetRequiredService<PayInfoBLL>();
            _custInfoService = provider.GetRequiredService<CustInfoService>();
            _logger = provider.GetRequiredService<ILogger<PaymentController>>();
            _courierAdapter = provider.GetRequiredService<CourierAdapter>();

            var configuration = provider.GetRequiredService<IConfiguration>();
            tpApiUrl = configuration.GetValue<string>("TpService");
            mainWalletType = configuration.GetValue<int>("MainWalletType");
            _bonusServiceUrl = configuration.GetValue<string>("BonusService");
        }

        [HttpPost]
        public ApiResult<IEnumerable<OfflineBankInfoFront>> GetBankInfoOffList()
        {
            var authData = authorizeBLL.GetAuthData();
            var apiResult = new ApiResult<IEnumerable<OfflineBankInfoFront>>
            {
                Data = bankInfoOffBLL.GetBankInfoOffList(authData.CustId),
                ErrorCode = 0
            };
            return apiResult;
        }
        public class GetBankInfoOffListReq
        {
            public int? QueryType { get; set; } // queryType: 0.only open Bank(status = 1) 1.open & suspend Bank(status = 1 or status = 2)
        }
        [HttpPost]
        public ApiResult<IEnumerable<OfflineBankInfoFront>> GetBankInfoOffListStatus(GetBankInfoOffListReq getBankInfoOffListReq)
        {
            var authData = authorizeBLL.GetAuthData();
            var apiResult = new ApiResult<IEnumerable<OfflineBankInfoFront>>();
            var queryType = 0;
            if (getBankInfoOffListReq != null && getBankInfoOffListReq.QueryType != null)
                queryType = (int)getBankInfoOffListReq.QueryType;
            apiResult.Data = bankInfoOffBLL.GetBankInfoOffListWithCache(authData.CustId, authData.LangId, queryType);
            apiResult.ErrorCode = 0;
            return apiResult;
        }

        public ApiResult<IEnumerable<SysBankInfoFront>> GetBankInfoOffListStatusORG(GetBankInfoOffListReq getBankInfoOffListReq)
        {
            var authData = authorizeBLL.GetAuthData();
            var apiResult = new ApiResult<IEnumerable<SysBankInfoFront>>();
            var queryType = 0;
            if (getBankInfoOffListReq != null && getBankInfoOffListReq.QueryType != null)
                queryType = (int)getBankInfoOffListReq.QueryType;
            apiResult.Data = bankInfoOffBLL.GetBankInfoOffList(authData.CustId, queryType);
            apiResult.ErrorCode = 0;
            return apiResult;
        }

        [HttpPost]
        public ApiResult<IEnumerable<WithdrawalHistory>> GetWithdrawalInfoRecent()
        {
            var authData = authorizeBLL.GetAuthData();
            var apiResult = new ApiResult<IEnumerable<WithdrawalHistory>>
            {
                Data = paymentBLL.GetWithdrawalInfoRecent(authData.CustId),
                ErrorCode = 0
            };
            return apiResult;
        }

        [HttpPost]
        public ApiResult<IEnumerable<DepositInfoHistory>> GetDepositInfoRecent()
        {
            var authData = authorizeBLL.GetAuthData();
            var apiResult = new ApiResult<IEnumerable<DepositInfoHistory>>
            {
                Data = paymentBLL.GetDepositInfoRecent(authData.CustId),
                ErrorCode = 0
            };
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
            var authData = authorizeBLL.GetAuthData();
            var apiResult = new ApiListResult<DepositInfoHistory>();
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
            var authData = authorizeBLL.GetAuthData();
            var apiResult = new ApiListResult<WithdrawalHistory>();
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
        public ApiResult<DepositRep3> DepositReqWireTransfer(DepositReqWireTransferReq depositReqWireTransferReq)
        {
            var apiResult = new ApiResult<DepositRep3>();
            var authData = authorizeBLL.GetAuthData();

            //check AccountName first
            var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);
            
            if (custInfo.SiteId == (int)SiteId.Goal123 && custInfoBLL.GetCustBankInfo(authData.CustId) == null)
            {
                apiResult.ErrorCode = -2;
                apiResult.Message = selLangBLL.GetMsg("msg_NeedAccountName");
                return apiResult;
            }
            
            var decimalPlaces = 0;
            if (!HasThisManyDecimalPlacesOrLess(depositReqWireTransferReq.amount, decimalPlaces))
            {
                apiResult.ErrorCode = -5;
                apiResult.Message = "Amount format is wrong.";
                return apiResult;
            }
            
            //check PhoneVerified,AccountName,Birth
            if (depositReqWireTransferReq.hasPromotion)
            {
                var custInfoProfile = custInfoBLL.GetCustInfoProfile(authData.CustId);
                if (!custInfoProfile.PhoneVerified
                    || string.IsNullOrEmpty(custInfoProfile.AccountName)
                    || string.IsNullOrEmpty(custInfoProfile.Birth_y)
                    || string.IsNullOrEmpty(custInfoProfile.Birth_m)
                    || string.IsNullOrEmpty(custInfoProfile.Birth_d))
                {
                    apiResult.ErrorCode = 2000;
                    apiResult.Message = selLangBLL.GetMsg("msg_OTPHint");
                    return apiResult;
                }
            }


            var result = -1;
            DepositRep3 depositRep3 = null;
            var depositFromInfo = new DepositFromInfo
            {
                Channel = depositReqWireTransferReq.fromChannel,
                SysId = depositReqWireTransferReq.fromSysId,
                AccountNo = depositReqWireTransferReq.fromAccountNum,
                IFSCCode = depositReqWireTransferReq.fromIFSCCode
            };
            var fromInfo = Newtonsoft.Json.JsonConvert.SerializeObject(depositFromInfo);

            if (depositReqWireTransferReq.fromChannel == 0 && depositReqWireTransferReq.fromSysId == 0 && depositReqWireTransferReq.fromAccountNum == null)
                result = paymentBLL.DepositReqLocalBank(authData.CustId, depositReqWireTransferReq.sysId, depositReqWireTransferReq.amount, depositReqWireTransferReq.memo, out depositRep3);
            else
                result = paymentBLL.DepositReqLocalBankWithFrom(authData.CustId, depositReqWireTransferReq.sysId, depositReqWireTransferReq.amount, depositReqWireTransferReq.memo, fromInfo, out depositRep3);

            apiResult.ErrorCode = result;

            switch (result)
            {
                case 0:
                    apiResult.Data = depositRep3;
                    //apiResult.Message = "";
                    break;
                case -1:
                    //apiResult.Message = "";
                    break;
                case -2:
                    //apiResult.Message = "";
                    break;
                case -3:
                    //apiResult.Message = "";
                    break;
                case -4:
                    //apiResult.Message = "";
                    break;
            }

            return apiResult;
        }

        [HttpPost]
        public ApiResult<DepositRep3> GetLocalBankInfo(DepositReqWireTransferReq depositReqWireTransferReq)
        {
            var apiResult = new ApiResult<DepositRep3>();
            var authData = authorizeBLL.GetAuthData();

            //check AccountName first
            var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);

            //if (custInfo.SiteId == 2 && custInfoBLL.GetCustBankInfo(authData.CustId) == null)
            //{
            //    apiResult.ErrorCode = -2;
            //    apiResult.Message = selLangBLL.GetMsg("msg_NeedAccountName");
            //    return apiResult;
            //}
            //else /**/
            //{

            //}

            var result = paymentBLL.LocalBankinfo(authData.CustId, depositReqWireTransferReq.sysId, depositReqWireTransferReq.memo, out var depositRep3);

            apiResult.ErrorCode = result;

            switch (result)
            {
                case 0:
                    apiResult.Data = depositRep3;
                    //apiResult.Message = "";
                    break;
                case -1:
                    //apiResult.Message = "";
                    break;
                case -2:
                    //apiResult.Message = "";
                    break;
                case -3:
                    //apiResult.Message = "";
                    break;
                case -4:
                    //apiResult.Message = "";
                    break;
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
        }

        [HttpPost]
        public async Task<ApiResult> WithdrawalReqAsync(WithdrawalReqModel withdrawalReqModel)
        {
            var apiResult = new ApiResult(selLangBLL);
            var authData = authorizeBLL.GetAuthData(true);
            var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);
            var withdrawalLimitInfo = paymentBLL.GetWithdrawalLimitInfo(new WithdrawalLimitInfoInput
            { SiteId = custInfo.SiteId, CurrencyId = custInfo.CurrencyId, VipLevel = custInfo.UserLevel });
            var dailyCustomerWithdrawalInfo = paymentBLL.GetDailyCustomerWithdrawalInfo(new DailyCustomerWithdrawalInfoInput { CustId = custInfo.CustId });

            #region - Check -
            if (!WithdrawalReqCheck(ref apiResult, withdrawalReqModel, authData, custInfo))
            {
                return apiResult;
            }
            #endregion

            #region - Check WithdrawalLimit -
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
            if (custInfo.CurrencyId == 96)//USDT
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

        public class WithdrawalUSDTReqModel
        {
            public string AccountNo { set; get; }
            public decimal amount { set; get; }
            public string memo { set; get; }
            public string blackbox { get; set; }
        }

        private async Task<ApiResult> DoWithdrawalAsync(ApiResult apiResult,
            AuthData authData,
            Customer custInfo,
            WithdrawalReqModel withdrawalReqModel,
            CustBankInfo custBankInfo,
            WithdrawalTypeStatus withdrawalType,
            USDTDetail usdtDetail)
        {
            var result = 99;

            //GetWithdrawalAvailableBalance

            /*Temp will be Removed*/
            var balance = walletBLL.GetBalance(authData.CustId);
            var WithdrawalAvailableBalance = paymentBLL.GetWithdrawalAvailableBalance(authData.CustId);
            ApiResult<TransferResult> transferToSabaResult = null;
            if (mainWalletType == 2)
            {
                var sabaBalance = await Get3rdBalanceAsync(1, authData.CustId);
                decimal transferBalance = 0;
                WithdrawalAvailableBalance += sabaBalance;
                if (WithdrawalAvailableBalance < withdrawalReqModel.amount)
                {
                    decimal DiffWithdrawalAvailableBalance = 0;
                    if (WithdrawalAvailableBalance > 0)
                    {
                        DiffWithdrawalAvailableBalance = withdrawalReqModel.amount - WithdrawalAvailableBalance;
                    }
                    else
                    {
                        DiffWithdrawalAvailableBalance = withdrawalReqModel.amount;
                    }

                    apiResult.Message = $"{DiffWithdrawalAvailableBalance}";
                    apiResult.ErrorCode = -3;
                    return apiResult;
                }
                if (balance + sabaBalance < withdrawalReqModel.amount)
                {
                    apiResult.Message = $"{withdrawalReqModel.amount - (balance + sabaBalance)}";
                    apiResult.ErrorCode = -2;
                    return apiResult;
                }

                if (balance < withdrawalReqModel.amount)
                {
                    transferBalance = withdrawalReqModel.amount - balance;
                }

                var input = new TpTransfer
                {
                    Amount = transferBalance,
                    CustId = authData.CustId,
                    direction = TpDirection.ToMain,
                    TpId = 1
                };
                transferToSabaResult = await TransferToSabaAsync(input);
            }
            else
            {
                if (WithdrawalAvailableBalance < withdrawalReqModel.amount)
                {
                    //decimal DiffWithdrawalAvailableBalance = 0;
                    //if (WithdrawalAvailableBalance > 0)
                    //{
                    //    DiffWithdrawalAvailableBalance = withdrawalReqModel.amount - WithdrawalAvailableBalance;
                    //}
                    //else
                    //{
                    //    DiffWithdrawalAvailableBalance = withdrawalReqModel.amount;
                    //}

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

                transferToSabaResult = new ApiResult<TransferResult> { ErrorCode = 0 };
            }
            /*Temp*/
            if (transferToSabaResult.ErrorCode == 0)
            {
                if (withdrawalReqModel.blackbox != null)
                {
                    _ = Task.Run(async () =>
                    {
                        var apiResultIovation = await iovationBLL.IovationChecks(authData.CustId, custInfo.CurrencyId, custInfo.UserName, withdrawalReqModel.blackbox, IovationType.withdrawal);
                        if (apiResultIovation.ErrorCode == 0)
                            iovationBLL.SetIovation(authData.CustId, apiResultIovation.Message, IovationType.withdrawal);

                    });
                }

                decimal afterAmount;
                string transId;
                switch(withdrawalType)
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
                        result = paymentBLL.WithdrawalUSDTReq(authData.CustId,
                        custBankInfo.AccountName,
                        usdtDetail,
                        withdrawalReqModel.AccountNo,
                        withdrawalReqModel.amount,
                        withdrawalReqModel.memo, out afterAmount, out transId);
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
                //result = isCrypto
                //    ? paymentBLL.WithdrawalUSDTReq(authData.CustId,
                //        custBankInfo.AccountName,
                //        usdtDetail,
                //        withdrawalReqModel.AccountNo,
                //        withdrawalReqModel.amount,
                //        withdrawalReqModel.memo, out afterAmount, out transId)
                //    : paymentBLL.WithdrawalReq(authData.CustId,
                //        withdrawalReqModel.sysId,
                //        custBankInfo.AccountName,
                //        withdrawalReqModel.AccountNo,
                //        withdrawalReqModel.IFSCCode,
                //        withdrawalReqModel.amount,
                //        withdrawalReqModel.memo, out afterAmount, out transId); 

                //_logger.LogInformation($"AfterAmount={afterAmount}, TransId={transId}");

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
                            if (mainWalletType == 2)
                            {
                                transferToSabaResult = null;
                                var input = new TpTransfer
                                {
                                    Amount = walletBLL.GetBalance(authData.CustId),
                                    CustId = authData.CustId,
                                    direction = TpDirection.ToTp,
                                    TpId = 1
                                };
                                transferToSabaResult = await TransferToSabaAsync(input);
                            }

                            apiResult.ErrorCode = 9000;
                            apiResult.Message = selLangBLL.GetMsg("msg_WithdrawalFailed");
                            break;
                        }
                }
            }
            else
            {
                apiResult.ErrorCode = transferToSabaResult.ErrorCode;
                //apiResult.Message = transferToSabaResult.Message;
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
            var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);
            var dailyCustomerWithdrawalInfo = paymentBLL
                .GetDailyCustomerWithdrawalInfo(new DailyCustomerWithdrawalInfoInput { CustId = custInfo.CustId });

            if (!WithdrawalReqCheck(ref apiResult, withdrawalReqModel, authData, custInfo))
            {
                return apiResult;
            }

            #region - Check USDTwithdrawalLimit -
            var custBankInfo = custInfoBLL.GetCustBankInfo(authData.CustId);

            if (custBankInfo?.usdtDetail == null
                || !custBankInfo.usdtDetail.Any(x => x.USDTAddress == withdrawalReqModel.AccountNo
                    && x.CurrencyId == withdrawalReqModel.CurrencyId))
            {
                _logger.LogError(
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
            
            if (dailyCustomerWithdrawalInfo.Count >= withdrawalLimitInfo.Times)
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

            if (custInfo.CurrencyId == (int)CurrencyId.INR && !_custInfoService.GetPhoneVerified(custInfo.CustId).Result)
            {
                apiResult.Message = selLangBLL.GetMsg("msg_CompletePhoneNumber");
                apiResult.ErrorCode = -9;
                return false;
            }

            if (authData.CustId == 0)
            {
                apiResult.Message = selLangBLL.GetMsg("msg_DepositProcessLogin_ACLocked");
                apiResult.ErrorCode = -7;
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
            var authData = authorizeBLL.GetAuthData();
            var apiResult = new ApiResult<IEnumerable<BankInfoSys>>();
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
            var authData = authorizeBLL.GetAuthData();
            var apiResult = new ApiResult<IEnumerable<SysBankInfoFront>>();
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
            var authData = authorizeBLL.GetAuthData();
            var apiResult = new ApiResult<IEnumerable<PaymentFront>>();
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
            var authData = authorizeBLL.GetAuthData();
            var apiResult = new ApiResult<BankInfoPayFront>();
            var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);
            apiResult.Data = bankInfoBLL.GetOnlineBankInfo(custInfo.CustId, onlineBankInfoReq.sysId, onlineBankInfoReq.payId);
            apiResult.ErrorCode = 0;
            return apiResult;
        }

        [HttpPost]
        public ApiResult<IEnumerable<PayGroup>> GetMobilePayGroupList()
        {
            var authData = authorizeBLL.GetAuthData();
            var apiResult = new ApiResult<IEnumerable<PayGroup>>
            {
                Data = _payInfoBLL.GetPayGroupList(authData.CustId, DepositType.MobilePayment),
                ErrorCode = 0
            };
            return apiResult;
        }
        public class PayInfoReq
        {
            public int groupId { set; get; }
        }

        [HttpPost]
        public ApiResult<IEnumerable<PayInfo>> GetMobilePayInfoList(PayInfoReq payInfoReq)
        {
            var authData = authorizeBLL.GetAuthData();
            var apiResult = new ApiResult<IEnumerable<PayInfo>>
            {
                Data = _payInfoBLL.GetPayInfoList(authData.CustId, payInfoReq.groupId, DepositType.MobilePayment),
                ErrorCode = 0
            };
            return apiResult;
        }

        /*public class CryptoPayInfoReq
        {
            public int groupId { set; get; }
        }*/
        [HttpPost]
        public ApiResult<IEnumerable<CryptoPayInfo>> GetCryptoPayInfoListAsync()
        {
            var authData = authorizeBLL.GetAuthData();
            var apiResult = new ApiResult<IEnumerable<CryptoPayInfo>>
            {
                Data = cryptoPayInfoBLL.GetCryptoPayInfoList(authData.CustId),
                ErrorCode = 0
            };
            return apiResult;
        }

        [HttpPost]
        public ApiResult<IEnumerable<LoadCardPayGroup>> GetLoadCardPayGroupList()
        {
            var authData = authorizeBLL.GetAuthData();
            var apiResult = new ApiResult<IEnumerable<LoadCardPayGroup>>
            {
                Data = loadCardPayInfoBLL.GetLoadCardPayGroupList(authData.CustId),
                ErrorCode = 0
            };
            return apiResult;
        }

        [HttpPost]
        public ApiResult<IEnumerable<LoadCardPayInfo>> GetLoadCardPayInfoList(PayInfoReq payInfoReq)
        {
            var authData = authorizeBLL.GetAuthData();
            var apiResult = new ApiResult<IEnumerable<LoadCardPayInfo>>
            {
                Data = loadCardPayInfoBLL.GetLoadCardPayInfoList(authData.CustId, payInfoReq.groupId),
                ErrorCode = 0
            };
            return apiResult;
        }

        public class DepositReceiptFile
        {
            public string TransId { get; set; }
            public IFormFile Receipt { get; set; }
        }

        private readonly Dictionary<string, string> _contentTypes = new Dictionary<string, string>(comparer: StringComparer.OrdinalIgnoreCase)
        {
            {".png", "image/png"},
            {".jpg", "image/jpeg"},
            {".jpeg", "image/jpeg"},
            {".gif", "image/gif"}
        };

        private readonly Dictionary<string, System.Drawing.Imaging.ImageFormat> _contentTypesCheck = new Dictionary<string, System.Drawing.Imaging.ImageFormat>(comparer: StringComparer.OrdinalIgnoreCase)
        {
            {"image/png",System.Drawing.Imaging.ImageFormat.Png},
            {"image/jpeg",System.Drawing.Imaging.ImageFormat.Jpeg},
            { "image/gif",System.Drawing.Imaging.ImageFormat.Gif}
        };

        [HttpPost]
        //[DisableFormValueModelBinding]
        public async Task<ApiResult> UploadReceipt([FromForm] DepositReceiptFile depositReceipt)
        {
            var apiResult = new ApiResult();
            var authData = authorizeBLL.GetAuthData();

            if (_contentTypesCheck.ContainsKey(depositReceipt.Receipt.ContentType))
            {
                await using var fileStream = depositReceipt.Receipt.OpenReadStream();
                using var uploadedImage = await SixLabors.ImageSharp.Image.LoadAsync(fileStream);
                var fileBytes = await _imageEditorBll.Resize(uploadedImage);

                var fileName = _fileNameBll
                    .SetFileName(depositReceipt.Receipt.FileName)
                    .ExcludePath()
                    .SetFilenameExtension("jpg")
                    .FileName;

                var result = await paymentBLL.SetDepositReceiptPic(authData.CustId, depositReceipt.TransId, fileName, fileBytes);

                if (result > 0)
                {
                    apiResult.ErrorCode = 0;
                }
                else
                {
                    apiResult.ErrorCode = -1;
                }
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

            var result = paymentBLL.SetWithdrawalCancel(authData.CustId, withdrawaltReq.TransId);
            if (result > 0)
            {

                if (mainWalletType == 2)
                {
                    ApiResult<TransferResult> transferToSabaResult = null;
                    var input = new TpTransfer
                    {
                        Amount = walletBLL.GetBalance(authData.CustId),
                        CustId = authData.CustId,
                        direction = TpDirection.ToTp,
                        TpId = 1
                    };
                    transferToSabaResult = await TransferToSabaAsync(input);
                }

                apiResult.ErrorCode = 0;
            }
            else
            {
                apiResult.ErrorCode = -1;
            }
            return apiResult;
        }
        private class TpInfo
        {
            public int TpId { set; get; }
            public int CustId { set; get; }
            public Dictionary<string, string> inputs { set; get; }
        }

        /*3rd Wallter*/
        private async Task<decimal> Get3rdBalanceAsync(int tpId, int custId)
        {
            var myUrl = $"{tpApiUrl}GetBalance";
            var oTpInfo = new TpInfo
            {
                TpId = tpId,
                CustId = custId,
                inputs = new Dictionary<string, string>()
            };

            var data = await courierAdapter.MyPostAsync<UserBalance, TpInfo>(myUrl, oTpInfo);

            return data.ErrorCode == 0 ? data.Data.balance + data.Data.bonus : 0;
        }

        [HttpPost]
        public ApiResult GetStatement(HistoryReq historyReq)
        {
            var authData = authorizeBLL.GetAuthData();
            var apiResult = new ApiListResult<Statement>();
            var pageNumber = historyReq.PageNumber;
            apiResult.Data = paymentBLL.GetStatement(authData.CustId, historyReq.StartDate, historyReq.EndDate, ref pageNumber, out var totalPages, out var rowCount);
            apiResult.PageNumber = pageNumber;
            apiResult.TotalPages = totalPages;
            apiResult.RowCount = rowCount;
            apiResult.ErrorCode = 0;
            return apiResult;
        }

        public class BounsLogReq
        {
            public long StartDate { get; set; }
            public long EndDate { get; set; }
            public int PageNumber { get; set; }
            public int PromoCategoryId { get; set; }
        }
        public ApiResult GetBounsLogStatement(BounsLogReq bounsLogReq)
        {
            var authData = authorizeBLL.GetAuthData();
            var apiResult = new ApiListResult<BounsLogStatement>();
            var pageNumber = bounsLogReq.PageNumber;
            apiResult.Data = paymentBLL.GetBounsLogStatement(authData.CustId, bounsLogReq.PromoCategoryId, bounsLogReq.StartDate, bounsLogReq.EndDate, ref pageNumber, out var totalPages, out var rowCount);
            apiResult.PageNumber = pageNumber;
            apiResult.TotalPages = totalPages;
            apiResult.RowCount = rowCount;
            apiResult.ErrorCode = 0;
            return apiResult;
        }
        public ApiResult<IEnumerable<PromotionCategory>> GetPromotionCategory()
        {

            var apiResult = new ApiResult<IEnumerable<PromotionCategory>>
            {
                Data = paymentBLL.GetPromotionCategory()
            };
            return apiResult;
        }

        public class TpTransfer
        {
            public int TpId { set; get; }
            public int CustId { set; get; }
            public decimal Amount { set; get; }
            public TpDirection direction { set; get; }
            public Dictionary<string, string> inputs { set; get; }
        }

        public enum TpDirection
        {
            ToTp = 1,
            ToMain = 2
        }
        private async Task<ApiResult<TransferResult>> TransferToSabaAsync(TpTransfer inTpTransfer)
        {
            var amount = inTpTransfer.Amount;
            if (amount.ToString().IndexOf(".") > -1)
            {
                decimal.TryParse(amount.ToString("#.##"), out amount);
            }

            var tpTransfer = new TpTransfer
            {
                TpId = inTpTransfer.TpId,
                CustId = inTpTransfer.CustId,
                Amount = amount,
                direction = inTpTransfer.direction,
                inputs = null
            };
            var myUrl = $"{tpApiUrl}Transfer";
            var data = await courierAdapter.MyPostAsync<TransferResult, TpTransfer>(myUrl, tpTransfer);

            return data;
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
            result.ErrorCode = custInfoBLL.SubmittedCustPhotoId(authData.CustId);

            return result;
        }
        [HttpPost]
        public ApiResult<IEnumerable<TpBetRecordListOneWeek>> GetCustTpBetRecordListOneWeek()
        {
            var authData = authorizeBLL.GetAuthData();
            var apiResult = new ApiResult<IEnumerable<TpBetRecordListOneWeek>>
            {
                ErrorCode = 0,
                Data = paymentBLL.GetCustTpBetRecordListOneWeek(authData.CustId)
            };
            return apiResult;
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
            var authData = authorizeBLL.GetAuthData();
            var apiResult = new ApiListResult<TpBetRecordListOneDay>();
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
            var authData = authorizeBLL.GetAuthData();
            var apiResult = new ApiListResult<TpBetRecordbyCategoryId>();
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
            var authData = authorizeBLL.GetAuthData();
            var apiResult = new ApiListResult<TpBetRecordDetailbyGameType>();
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
            var authData = authorizeBLL.GetAuthData();
            var apiResult = new ApiListResult<TpBetRecordListDateRange>();
            var PageSize = tpBetRecordDateRangeReq.PageSize;
            var TotalPages = 0;
            var RowCount = 0;
            var PageNumber = tpBetRecordDateRangeReq.PageNumber;

            apiResult.ErrorCode = 0;
            apiResult.Data = paymentBLL.GetCustTpBetRecordDateRange(authData.CustId, tpBetRecordDateRangeReq.StartDate, tpBetRecordDateRangeReq.EndDate, PageSize, ref PageNumber, ref TotalPages, ref RowCount);
            apiResult.PageNumber = PageNumber;
            apiResult.RowCount = RowCount;
            apiResult.TotalPages = TotalPages;

            return apiResult;
        }
        [HttpPost]
        public ApiResult<WithdrawalLimitOutput> GetWithdrawalLimit()
        {
            var cryptoList = new List<CryptoWithdrawalLimitOutput>();
            var authData = authorizeBLL.GetAuthData();
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

            var tmpEWalletAvailableAmount = eWallet.WithdrawalMax == 0
                ? dailyCustomerWithdrawalInfo.Amount
                : eWallet.WithdrawalMax - dailyCustomerWithdrawalInfo.Amount;
            
            #endregion
           
            var data = new WithdrawalLimitOutput
            {
                WithdrawalMin = output.WithdrawalMin,
                Unlimited = output.TotalWithdrawalMax == 0,
                WithdrawalAvailableAmount = tmpWithdrawalAvailableAmount < 0
                    ? 0 : tmpWithdrawalAvailableAmount,
                MaxTimes = output.Times,
                WithdrawalTimes = dailyCustomerWithdrawalInfo.Count,
                USDTList = cryptoList,
                EWalletWithdrawalMin = eWallet.WithdrawalMin,
                EWalletUnlimited = eWallet.WithdrawalMax == 0,
                EWalletWithdrawalAvailableAmount = tmpEWalletAvailableAmount < 0
                    ? 0 : tmpEWalletAvailableAmount,
                EWalletStatus = eWallet.Status
            };

            var apiResult = new ApiResult<WithdrawalLimitOutput>
            {
                Data = data,
                ErrorCode = 0
            };

            return apiResult;
        }

        public class GetExchangeReq
        {
            public int PayId { get; set; }
            public string CurrencyName { get; set; }
            public string Network { get; set; }
            public decimal Amount { get; set; }
        }

        [HttpPost]
        public async Task<ApiResult<ExchangeInfo>> GetExchangeInfoAsync(GetExchangeReq getExchangeReq)
        {
            var authData = authorizeBLL.GetAuthData();
            var apiResult = new ApiResult<ExchangeInfo>();
            var payId = 22; // 先留著讓新舊相容
            var channelName =
                getExchangeReq.CurrencyName == getExchangeReq.Network
                    ? getExchangeReq.CurrencyName
                    : $"{getExchangeReq.CurrencyName}-{getExchangeReq.Network}";

            var result = await cryptoPayInfoBLL.GetExchangeInfoAsync(authData.CustId,
                getExchangeReq.Amount, payId, channelName);
            if (result != null && result.Data != null)
            {
                apiResult.Data = result.Data.formData;
                apiResult.ErrorCode = result.ErrorCode;
            }
            return apiResult;
        }
        public class GetSupportNetworkReq
        {
            public int CurrencyId { get; set; }
        }
        public ApiResult<IEnumerable<NetworkList>> GetSupportNetwork(GetSupportNetworkReq getSupportNetworkReq)
        {

            var apiResult = new ApiResult<IEnumerable<NetworkList>>
            {
                Data = paymentBLL.GetSupportNetwork(getSupportNetworkReq.CurrencyId),
                ErrorCode = 0
            };
            return apiResult;
        }

        [HttpPost]
        public ApiResult<CryptoInfoData> GetSupportCryptoInfo()
        {
            var result = paymentBLL.GetSupportCryptoInfo();

            var apiResult = new ApiResult<CryptoInfoData>
            {
                Data = result,
                ErrorCode = 0
            };

            return apiResult;
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
            return new ApiResult<IEnumerable<PayGroup>>
            {
                Data = _payInfoBLL.GetPayGroupList(authorizeBLL.GetAuthData().CustId, DepositType.QRPayPayment),
                ErrorCode = 0
            };
        }

        [HttpPost]
        public ApiResult<IEnumerable<PayInfo>> GetQRPayInfoList(PayInfoReq payInfoReq)
        {
            return new ApiResult<IEnumerable<PayInfo>>
            {
                Data = _payInfoBLL.GetPayInfoList(authorizeBLL.GetAuthData().CustId, payInfoReq.groupId, DepositType.QRPayPayment),
                ErrorCode = 0
            };
        }

        [HttpPost]
        public ApiResult<IEnumerable<SysBankInfoFront>> GetAutoChannelBankGroupList()
        {
            var authData = authorizeBLL.GetAuthData();
            var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);
            var vipLevel = custInfoBLL.GetPointLevelWithCache(authData.CustId);
            var result = bankInfoBLL.GetAutoChannelBankGroupList(custInfo, vipLevel).ToList();
            if (custInfo.SiteId == (int)SiteId.Nova88 && custInfo.CurrencyId == (int)CurrencyId.THB) // Nova88 泰國銀行名稱支援多語系
            {
                bankInfoBLL.BankNameLocalization(result, authData.LangId);
            }

            return new ApiResult<IEnumerable<SysBankInfoFront>>
            {
                Data = result,
                ErrorCode = 0
            };
        }

        /// <summary>
        /// 取得存款Line頻道
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ApiResult<LineChannel> GetLineChannelForDpeosit()
        {
            var authData = authorizeBLL.GetAuthData();
            if (authData.CustId == 0)
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
            if(result == null)
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
            var authData = authorizeBLL.GetAuthData();
            var apiResult = new ApiResult<IEnumerable<EWalletInfoSys>>();
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
            var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);
            var withdrawalLimitInfo = paymentBLL.GetWithdrawalLimitInfo(new WithdrawalLimitInfoInput
            { SiteId = custInfo.SiteId, CurrencyId = custInfo.CurrencyId, VipLevel = custInfo.UserLevel });
            var eWalletWithdrawalLimitInfo = paymentBLL.GetEWalletWithdrawalLimitInfo(new WithdrawalLimitInfoInput { SiteId = custInfo.SiteId, CurrencyId = custInfo.CurrencyId });
            var dailyCustomerWithdrawalInfo = paymentBLL.GetDailyCustomerWithdrawalInfo(new DailyCustomerWithdrawalInfoInput { CustId = custInfo.CustId });

            #region - Check -
            if (!WithdrawalReqCheck(ref apiResult, withdrawalReqModel, authData, custInfo))
            {
                return apiResult;
            }
            #endregion

            #region - Check WithdrawalLimit -
            if(eWalletWithdrawalLimitInfo.Status == (int)EWalletStatus.Disable)
            {
                apiResult.Message = selLangBLL.GetMsg("msg_EWalletIsMaintenance");
                apiResult.ErrorCode = -8;
                return apiResult;
            }
            if (eWalletWithdrawalLimitInfo.WithdrawalMin > withdrawalReqModel.amount)
            {
                apiResult.Message = string.Format(selLangBLL.GetMsg("msg_LessThanWithdrawalMin"), eWalletWithdrawalLimitInfo.WithdrawalMin);
                apiResult.ErrorCode = -5;
                return apiResult;
            }

            if (withdrawalLimitInfo != null
                && withdrawalLimitInfo.TotalWithdrawalMax != 0 
                && dailyCustomerWithdrawalInfo.Amount + withdrawalReqModel.amount > withdrawalLimitInfo.TotalWithdrawalMax)
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

            #region - Check EWalletInfo -
            var eWalletInfo = custInfoBLL.GetCustEWalletInfo(authData.CustId).ToList();
            var custBankInfo = custInfoBLL.GetCustBankInfo(authData.CustId);
            if(eWalletInfo.Count == 0 || eWalletInfo.Find(x=> x.AccountNo == withdrawalReqModel.AccountNo && x.SysId == withdrawalReqModel.sysId) == null)
            {
                return apiResult;
            }
            #endregion

            return await DoWithdrawalAsync(apiResult, authData, custInfo, withdrawalReqModel, custBankInfo, WithdrawalTypeStatus.EWallet, null);
        }
    }
}
