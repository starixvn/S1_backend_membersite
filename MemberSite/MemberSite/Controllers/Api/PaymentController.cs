using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MemberSite.Service.BLL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

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
        private readonly MobilePayInfoBll mobilePayInfoBLL;
        private readonly IovationBLL iovationBLL;
        private readonly CryptoPayInfoBll cryptoPayInfoBLL;
        private readonly LoadCardPayInfoBLL loadCardPayInfoBLL;
        private readonly string tpApiUrl;
        private readonly int mainWalletType;
        private readonly FileNameBLL _fileNameBll;
        private readonly ImageEditorBLL _imageEditorBll;
        private readonly PromotionBll _promotionBLL;

        public PaymentController(CourierAdapter courierAdapter, IConfiguration configuration, BankInfoBLL bankInfoBLL, BankInfoOffBLL bankInfoOffBLL, AuthorizeBLL authorizeBLL, PaymentBLL paymentBLL, CustInfoBll custInfoBLL, SelLangBLL selLangBLL, WalletBLL walletBLL, MobilePayInfoBll mobilePayInfoBLL, IovationBLL iovationBLL, CryptoPayInfoBll cryptoPayInfoBLL, LoadCardPayInfoBLL loadCardPayInfoBLL, FileNameBLL fileNameBll, ImageEditorBLL imageEditorBll, PromotionBll promotionBLL)
        {
            this.bankInfoOffBLL = bankInfoOffBLL;
            this.authorizeBLL = authorizeBLL;
            this.paymentBLL = paymentBLL;
            this.custInfoBLL = custInfoBLL;
            this.bankInfoBLL = bankInfoBLL;
            this.courierAdapter = courierAdapter;
            this.selLangBLL = selLangBLL;
            this.walletBLL = walletBLL;
            this.mobilePayInfoBLL = mobilePayInfoBLL;
            this.iovationBLL = iovationBLL;
            this.cryptoPayInfoBLL = cryptoPayInfoBLL;
            this.loadCardPayInfoBLL = loadCardPayInfoBLL;
            this.loadCardPayInfoBLL = loadCardPayInfoBLL;
            _fileNameBll = fileNameBll;
            _imageEditorBll = imageEditorBll;
            _promotionBLL = promotionBLL;
            tpApiUrl = configuration.GetValue<string>("TpService");
            mainWalletType = configuration.GetValue<int>("MainWalletType");
        }

        [HttpPost]
        public ApiResult<IEnumerable<SysBankInfoFront>> GetBankInfoOffList()
        {
            var authData = authorizeBLL.GetAuthData();
            var apiResult = new ApiResult<IEnumerable<SysBankInfoFront>>
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
        public ApiResult<IEnumerable<SysBankInfoFront>> GetBankInfoOffListStatus(GetBankInfoOffListReq getBankInfoOffListReq)
        {
            var authData = authorizeBLL.GetAuthData();
            var apiResult = new ApiResult<IEnumerable<SysBankInfoFront>>();
            var queryType = 0;
            if (getBankInfoOffListReq != null && getBankInfoOffListReq.QueryType != null)
                queryType = (int)getBankInfoOffListReq.QueryType;
            apiResult.Data = bankInfoOffBLL.GetBankInfoOffListWithCache(authData.CustId, queryType);
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

            if (custInfo.SiteId == 2 && custInfoBLL.GetCustBankInfo(authData.CustId) == null)
            {
                apiResult.ErrorCode = -2;
                apiResult.Message = selLangBLL.GetMsg("msg_NeedAccountName");
                return apiResult;
            }
            else /**/
            {

            }

            if (depositReqWireTransferReq.hasPromotion)
            {
                //check PhoneVerified,AccountName,Birth
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

            if (withdrawalLimitInfo != null && withdrawalLimitInfo.WithdrawalMax != 0 &&
                dailyCustomerWithdrawalInfo.Amount + withdrawalReqModel.amount > withdrawalLimitInfo.WithdrawalMax)
            {
                apiResult.Message = selLangBLL.GetMsg("msg_GreaterThanWithdrawalMax");
                apiResult.ErrorCode = -6;
                return apiResult;
            }
            #endregion

            #region - Check CustBankInfo -
            var custBankInfo = custInfoBLL.GetCustBankInfo(authData.CustId);
            if (custInfo.CurrencyId == 96)//USDT
            {
                if (custBankInfo?.uSDTDetail == null
                    || !custBankInfo.uSDTDetail.Any()
                    || custBankInfo.uSDTDetail.All(x => x.USDTAddress != withdrawalReqModel.AccountNo))
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
                else if (balance + sabaBalance < withdrawalReqModel.amount)
                {
                    apiResult.Message = $"{withdrawalReqModel.amount - (balance + sabaBalance)}";
                    apiResult.ErrorCode = -2;
                    return apiResult;
                }

                else if (balance < withdrawalReqModel.amount)
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
                else if (balance < withdrawalReqModel.amount)
                {
                    apiResult.Message = $"{withdrawalReqModel.amount - balance}";
                    apiResult.ErrorCode = -2;
                    return apiResult;
                }

                transferToSabaResult = new ApiResult<TransferResult> {ErrorCode = 0};
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

                result = paymentBLL.WithdrawalReq(authData.CustId, withdrawalReqModel.sysId, custBankInfo.AccountName, withdrawalReqModel.AccountNo, withdrawalReqModel.IFSCCode, withdrawalReqModel.amount, withdrawalReqModel.memo, out var afterAmount, out var transId);

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

        public class WithdrawalUSDTReqModel
        {
            public string AccountNo { set; get; }
            public decimal amount { set; get; }
            public string memo { set; get; }
            public string blackbox { get; set; }
        }

        [HttpPost]
        public async Task<ApiResult> WithdrawalUSDTReqAsync(WithdrawalReqModel withdrawalReqModel)
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

            #region - Check USDTwithdrawalLimit -
            var custBankInfo = custInfoBLL.GetCustBankInfo(authData.CustId);

            if (custBankInfo?.uSDTDetail == null
                || !custBankInfo.uSDTDetail.Any()
                || custBankInfo.uSDTDetail.All(x => x.USDTAddress != withdrawalReqModel.AccountNo))
            {
                return apiResult;
            }

            var custSeluSDTDetail = custBankInfo.uSDTDetail.FirstOrDefault(x => x.USDTAddress == withdrawalReqModel.AccountNo);
            var USDTwithdrawalLimitInfo = paymentBLL.GetUSDTWithdrawalLimitInfo(new USDTWithdrawalLimitInfoInput
            { SiteId = custInfo.SiteId, CurrencyId = custInfo.CurrencyId, VipLevel = custInfo.UserLevel, Network = custSeluSDTDetail.Network });

            if (USDTwithdrawalLimitInfo != null && USDTwithdrawalLimitInfo.WithdrawalMin > withdrawalReqModel.amount)
            {
                apiResult.Message = string.Format(selLangBLL.GetMsg("msg_LessThanWithdrawalMin"), USDTwithdrawalLimitInfo.WithdrawalMin);
                apiResult.ErrorCode = -5;
                return apiResult;
            }

            if (USDTwithdrawalLimitInfo != null && USDTwithdrawalLimitInfo.WithdrawalMax != 0 &&
                dailyCustomerWithdrawalInfo.Amount + withdrawalReqModel.amount > USDTwithdrawalLimitInfo.WithdrawalMax)
            {
                apiResult.Message = selLangBLL.GetMsg("msg_GreaterThanWithdrawalMax");
                apiResult.ErrorCode = -6;
                return apiResult;
            }
            #endregion

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
                else if (balance + sabaBalance < withdrawalReqModel.amount)
                {
                    apiResult.Message = $"{withdrawalReqModel.amount - (balance + sabaBalance)}";
                    apiResult.ErrorCode = -2;
                    return apiResult;
                }

                else if (balance < withdrawalReqModel.amount)
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
                else if (balance < withdrawalReqModel.amount)
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

                result = paymentBLL.WithdrawalUSDTReq(authData.CustId, custBankInfo.AccountName, custSeluSDTDetail, withdrawalReqModel.AccountNo, withdrawalReqModel.amount, withdrawalReqModel.memo, out var afterAmount, out var transId);

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
        
        private bool WithdrawalReqCheck(ref ApiResult apiResult, WithdrawalReqModel withdrawalReqModel,AuthData authData, Customer custInfo)
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

            var check = paymentBLL.GetWithdrawalInfoCheck(new CustomerWithdrawalInfoInput { CustId = authData.CustId });
            if (check.WithdrawalExists)
            {
                apiResult.Message = selLangBLL.GetMsg("msg_WithdrawalExists");
                apiResult.ErrorCode = -8;
                return false;
            }

            var rollover = paymentBLL.ReachedRollover(authData.CustId);

            if (!rollover.ReachedRollover)
            {
                apiResult.Message = $"{rollover.Lack}";
                apiResult.ErrorCode = -1;
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
            apiResult.Data = bankInfoBLL.GetBankInfoSysListByCurrencyForCtrl(custInfo.CurrencyId);
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
            apiResult.Data = bankInfoBLL.GetOnlineBankInfoSysList(custInfo.CustId, payment.payId);
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
        public ApiResult<IEnumerable<MobilePayGroup>> GetMobilePayGroupList()
        {
            var authData = authorizeBLL.GetAuthData();
            var apiResult = new ApiResult<IEnumerable<MobilePayGroup>>
            {
                Data = mobilePayInfoBLL.GetMobilePayGroupList(authData.CustId),
                ErrorCode = 0
            };
            return apiResult;
        }
        public class PayInfoReq
        {
            public int groupId { set; get; }
        }

        [HttpPost]
        public ApiResult<IEnumerable<MobilePayInfo>> GetMobilePayInfoList(PayInfoReq payInfoReq)
        {
            var authData = authorizeBLL.GetAuthData();
            var apiResult = new ApiResult<IEnumerable<MobilePayInfo>>
            {
                Data = mobilePayInfoBLL.GetMobilePayInfoList(authData.CustId, payInfoReq.groupId),
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
                    .FileName ;

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
            List<USDTWithdrawalLimitOutput> USDTList = new List<USDTWithdrawalLimitOutput>();
            var authData = authorizeBLL.GetAuthData();
            var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);
            var withdrawalLimitInfo = paymentBLL.GetWithdrawalLimitInfo(new WithdrawalLimitInfoInput
                {SiteId = custInfo.SiteId, CurrencyId = custInfo.CurrencyId, VipLevel = custInfo.UserLevel});
            var dailyCustomerWithdrawalInfo = paymentBLL.GetDailyCustomerWithdrawalInfo(new DailyCustomerWithdrawalInfoInput {CustId = custInfo.CustId});
            var USDTwithdrawalLimitInfo = paymentBLL.GetUSDTWithdrawalLimitInfoList(new USDTWithdrawalLimitInfoInput
            { SiteId = custInfo.SiteId, CurrencyId = custInfo.CurrencyId, VipLevel = custInfo.UserLevel, Network = null }).ToList();

            foreach (string name in Enum.GetNames(typeof(NetworkType)))
            {
                if(!USDTwithdrawalLimitInfo.ToList().Any(b=> b.Network == name))
                {
                    USDTwithdrawalLimitInfo.Add(new USDTWithdrawalLimitInfoOutput
                    {
                        Network = name,
                        WithdrawalMax = 0,
                        WithdrawalMin = 0
                    });
                }
            }

            USDTwithdrawalLimitInfo.ForEach(x =>
            {
                USDTList.Add(new USDTWithdrawalLimitOutput
                {
                    Network = x.Network,
                    WithdrawalMin = x.WithdrawalMin,
                    Unlimited = x.WithdrawalMax == 0,
                    WithdrawalAvailableAmount = x.WithdrawalMax == 0
                    ? dailyCustomerWithdrawalInfo.Amount
                    : x.WithdrawalMax - dailyCustomerWithdrawalInfo.Amount
                });
            });
            var output = withdrawalLimitInfo ?? new WithdrawalLimitInfoOutput() {WithdrawalMin = 0, WithdrawalMax = 0};
            var data = new WithdrawalLimitOutput
            {
                WithdrawalMin = output.WithdrawalMin,
                Unlimited = output.WithdrawalMax == 0,
                WithdrawalAvailableAmount = output.WithdrawalMax == 0
                    ? dailyCustomerWithdrawalInfo.Amount
                    : output.WithdrawalMax - dailyCustomerWithdrawalInfo.Amount,
                USDTList = USDTList
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
            public decimal Amount { get; set; }
        }

        [HttpPost]
        public async Task<ApiResult<ExchangeInfo>> GetExchangeInfoAsync(GetExchangeReq getExchangeReq)
        {
            var authData = authorizeBLL.GetAuthData();
            var apiResult = new ApiResult<ExchangeInfo>();
            var PayId = 22;
      
            var result = await cryptoPayInfoBLL.GetExchangeInfoAsync(authData.CustId, getExchangeReq.Amount, PayId);
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
        public class CheckDepositSpecialCodeReq
        {
            public string SpecialCode { get; set; }
            public int DeviceType { get; set; }
        }
        [HttpPost]
        public ApiResult<CheckDepositSpecialCodeRsp> CheckDepositSpecialCode(CheckDepositSpecialCodeReq checkDepositSpecialCodeReq)
        {
            var apiResult = new ApiResult<CheckDepositSpecialCodeRsp>();
            var authData = authorizeBLL.GetAuthData();
            var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);
            var custInfoProfile = custInfoBLL.GetCustInfoProfile(authData.CustId);

            //檢核SpecialCode格式是否正確
            bool IsSpecialCode = _promotionBLL.CheckSpecialCode(checkDepositSpecialCodeReq.SpecialCode);
            bool IsScratchCardCode = _promotionBLL.CheckScratchCardCode(checkDepositSpecialCodeReq.SpecialCode.ToUpper());
            if (!IsSpecialCode && !IsScratchCardCode)
            {
                apiResult.ErrorCode = 1000;
                apiResult.Message = selLangBLL.GetMsg("msg_SpecialCodeErr");
                return apiResult;
            }
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

            if (IsScratchCardCode)
            {
                //檢核是否真有這筆ScratchCardCode
                var promotion = _promotionBLL.GetPromotionBySpecialScratchCard(custInfo.SiteId, checkDepositSpecialCodeReq.SpecialCode);
                if(promotion != null)
                {
                    if (promotion.PromotionType  != (int)PromotionType.FstDeposit && promotion.PromotionType != (int)PromotionType.ReDeposit)
                    {
                        apiResult.ErrorCode = 1000;
                        apiResult.Message = selLangBLL.GetMsg("msg_SpecialCodeErr");
                        return apiResult;
                    }
                    if (promotion.CustId != 0 && promotion.CustId != authData.CustId)
                    {
                        apiResult.ErrorCode = 3000;
                        apiResult.Message = selLangBLL.GetMsg("msg_PromoCodeHasBeenUsed");
                        return apiResult;
                    }
                    //寫入CustSpecialCode Table,並檢核是否輸入相同ScratchCard過
                    //有輸入相同ScratchCard過
                    if (_promotionBLL.SetScratchCardCode(authData.CustId, checkDepositSpecialCodeReq.SpecialCode, checkDepositSpecialCodeReq.DeviceType) != 0)
                    {
                        //檢核是否領過這個BonusCode
                        if (_promotionBLL.GetBonusinfoByBonusCode(authData.CustId, promotion.BonusCode) != null)
                        {
                            apiResult.ErrorCode = 3000;
                            apiResult.Message = selLangBLL.GetMsg("msg_PromoCodeHasBeenUsed");
                            return apiResult;
                        }
                    }
                    apiResult.ErrorCode = 0;
                    apiResult.Data = new CheckDepositSpecialCodeRsp() { BonusCode = promotion.BonusCode };
                }
                else
                {
                    if (!IsSpecialCode)
                    {
                        apiResult.ErrorCode = 1000;
                        apiResult.Message = selLangBLL.GetMsg("msg_SpecialCodeErr");
                        return apiResult;
                    }
                    else
                    {
                        //檢核是否真有這筆specialCode
                        var promotionv2 = _promotionBLL.GetPromotionBySpecialCode(custInfo.SiteId, checkDepositSpecialCodeReq.SpecialCode);
                        if (promotionv2 != null)
                        {
                            if (promotionv2.PromotionType != (int)PromotionType.FstDeposit && promotionv2.PromotionType != (int)PromotionType.ReDeposit)
                            {
                                apiResult.ErrorCode = 1000;
                                apiResult.Message = selLangBLL.GetMsg("msg_SpecialCodeErr");
                                return apiResult;
                            }
                            //寫入CustSpecialCode Table,並檢核是否輸入相同specialCode過
                            //有輸入相同specialCode過
                            if (!_promotionBLL.SetSpecialCode(authData.CustId, checkDepositSpecialCodeReq.SpecialCode, checkDepositSpecialCodeReq.DeviceType))
                            {
                                //檢核是否領過這個BonusCode
                                if (_promotionBLL.GetBonusinfoByBonusCode(authData.CustId, promotionv2.BonusCode) != null)
                                {
                                    apiResult.ErrorCode = 3000;
                                    apiResult.Message = selLangBLL.GetMsg("msg_PromoCodeHasBeenUsed");
                                    return apiResult;
                                }
                            }
                            apiResult.ErrorCode = 0;
                            apiResult.Data = new CheckDepositSpecialCodeRsp() { BonusCode = promotionv2.BonusCode };
                        }
                        else
                        {
                            apiResult.ErrorCode = 1000;
                            apiResult.Message = selLangBLL.GetMsg("msg_SpecialCodeErr");
                            return apiResult;
                        }
                    }
                }             
            }
            else
            {
                //檢核是否真有這筆specialCode
                var promotion = _promotionBLL.GetPromotionBySpecialCode(custInfo.SiteId, checkDepositSpecialCodeReq.SpecialCode);
                if (promotion != null)
                {
                    if (promotion.PromotionType != (int)PromotionType.FstDeposit && promotion.PromotionType != (int)PromotionType.ReDeposit)
                    {
                        apiResult.ErrorCode = 1000;
                        apiResult.Message = selLangBLL.GetMsg("msg_SpecialCodeErr");
                        return apiResult;
                    }
                    //寫入CustSpecialCode Table,並檢核是否輸入相同specialCode過
                    //有輸入相同specialCode過
                    if (!_promotionBLL.SetSpecialCode(authData.CustId, checkDepositSpecialCodeReq.SpecialCode, checkDepositSpecialCodeReq.DeviceType))
                    {
                        //檢核是否領過這個BonusCode
                        if (_promotionBLL.GetBonusinfoByBonusCode(authData.CustId, promotion.BonusCode) != null)
                        {
                            apiResult.ErrorCode = 3000;
                            apiResult.Message = selLangBLL.GetMsg("msg_PromoCodeHasBeenUsed");
                            return apiResult;
                        }
                    }
                    apiResult.ErrorCode = 0;
                    apiResult.Data = new CheckDepositSpecialCodeRsp() { BonusCode = promotion.BonusCode };
                }
                else
                {
                    apiResult.ErrorCode = 1000;
                    apiResult.Message = selLangBLL.GetMsg("msg_SpecialCodeErr");
                    return apiResult;
                }
            }

            return apiResult;
        }
    }
}