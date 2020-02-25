using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using LazZiya.ImageResize;
using MemberSite.Attributes;
using MemberSite.Service.BLL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
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
        private readonly CustInfoBLL custInfoBLL;
        private readonly BankInfoBLL bankInfoBLL;
        private readonly CourierAdapter courierAdapter;
        private readonly SelLangBLL selLangBLL;
        private readonly WalletBLL walletBLL;
        private readonly string tpApiUrl;
        private readonly int mainWalletType;

        public PaymentController(CourierAdapter courierAdapter, IConfiguration configuration, BankInfoBLL bankInfoBLL, BankInfoOffBLL bankInfoOffBLL, AuthorizeBLL authorizeBLL, PaymentBLL paymentBLL, CustInfoBLL custInfoBLL, SelLangBLL selLangBLL, WalletBLL walletBLL)
        {
            this.bankInfoOffBLL = bankInfoOffBLL;
            this.authorizeBLL = authorizeBLL;
            this.paymentBLL = paymentBLL;
            this.custInfoBLL = custInfoBLL;
            this.bankInfoBLL = bankInfoBLL;
            this.courierAdapter = courierAdapter;
            this.selLangBLL = selLangBLL;
            this.walletBLL = walletBLL;
            tpApiUrl = configuration.GetValue<string>("TpService");
            mainWalletType = configuration.GetValue<int>("MainWalletType");
        }

        [HttpPost]
        public ApiResult<IEnumerable<SysBankInfoFront>> GetBankInfoOffList()
        {
            var authData = authorizeBLL.GetAuthData();
            var apiResult = new ApiResult<IEnumerable<SysBankInfoFront>>();
            apiResult.Data = bankInfoOffBLL.GetBankInfoOffList(authData.CustId);
            apiResult.ErrorCode = 0;
            return apiResult;
        }

        [HttpPost]
        public ApiResult<IEnumerable<WithdrawalHistory>> GetWithdrawalInfoRecent()
        {
            var authData = authorizeBLL.GetAuthData();
            var apiResult = new ApiResult<IEnumerable<WithdrawalHistory>>();
            apiResult.Data = paymentBLL.GetWithdrawalInfoRecent(authData.CustId);
            apiResult.ErrorCode = 0;
            return apiResult;
        }

        [HttpPost]
        public ApiResult<IEnumerable<DepositInfoHistory>> GetDepositInfoRecent()
        {
            var authData = authorizeBLL.GetAuthData();
            var apiResult = new ApiResult<IEnumerable<DepositInfoHistory>>();
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
            var authData = authorizeBLL.GetAuthData();
            var apiResult = new ApiListResult<DepositInfoHistory>();
            int pageNumber = historyReq.PageNumber;
            int totalPages;
            int rowCount;
            apiResult.Data = paymentBLL.GetDepositInfoHistory(authData.CustId, historyReq.StartDate, historyReq.EndDate, ref pageNumber, out totalPages, out rowCount);
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
            int pageNumber = historyReq.PageNumber;
            int totalPages;
            int rowCount;
            apiResult.Data = paymentBLL.GetWithdrawalInfoHistory(authData.CustId, historyReq.StartDate, historyReq.EndDate, ref pageNumber, out totalPages, out rowCount);
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
        }

        [HttpPost]
        public ApiResult<DepositRep3> DepositReqWireTransfer(DepositReqWireTransferReq depositReqWireTransferReq)
        {
            ApiResult<DepositRep3> apiResult = new ApiResult<DepositRep3>();

            var authData = authorizeBLL.GetAuthData();
            DepositRep3 depositRep3 = null;
            var result = paymentBLL.DepositReqWireTransfer(authData.CustId, depositReqWireTransferReq.sysId, depositReqWireTransferReq.amount, depositReqWireTransferReq.memo, out depositRep3);
            if (result == -1)
            {
                apiResult.ErrorCode = -1;
            }
            else if (result == -2)
            {
                apiResult.ErrorCode = -1;
            }
            else if (result == -3)
            {
                apiResult.ErrorCode = -1;
            }
            else
            {
                apiResult.ErrorCode = 0;
                apiResult.Data = depositRep3;
            }

            return apiResult;
        }

        public class WithdrawalReqModel
        {
            public int sysId { set; get; }
            public string AccountNo { set; get; }
            public decimal amount { set; get; }
            public string memo { set; get; }
        }

        [HttpPost]
        public async Task<ApiResult> WithdrawalReqAsync(WithdrawalReqModel withdrawalReqModel)
        {
            ApiResult apiResult = new ApiResult(selLangBLL);

            if (withdrawalReqModel.amount <= 0)
            {
                return apiResult;
            }

            var authData = authorizeBLL.GetAuthData();
            var rollover = paymentBLL.ReachedRollover(authData.CustId);

            if (!rollover.ReachedRollover)
            {
                apiResult.Message = $"{rollover.Lack}";
                apiResult.ErrorCode = -1;
                return apiResult;
            }

            var custBankInfo = custInfoBLL.GetCustBankInfo(authData.CustId);

            if (custBankInfo == null || custBankInfo.bankInfoDetail == null || custBankInfo.bankInfoDetail.Count() == 0)
            {
                return apiResult;
            }
            else if (custBankInfo.bankInfoDetail.Where(x => x.SysId == withdrawalReqModel.sysId && x.AccountNo == withdrawalReqModel.AccountNo).Count() == 0)
            {
                return apiResult;
            }
            int result = 99;

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

                var input = new TpTransfer();
                input.Amount = transferBalance;
                input.CustId = authData.CustId;
                input.direction = TpDirection.ToMain;
                input.TpId = 1;
                transferToSabaResult = await TransferToSabaAsync(input);
            }
            else
            {
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
                else if (balance < withdrawalReqModel.amount)
                {
                    apiResult.Message = $"{withdrawalReqModel.amount - balance}";
                    apiResult.ErrorCode = -2;
                    return apiResult;
                }

                transferToSabaResult = new ApiResult<TransferResult>();
                transferToSabaResult.ErrorCode = 0;
            }
            /*Temp*/
            if (transferToSabaResult.ErrorCode == 0)
            {
                decimal afterAmount = 0;
                string transId = "";

                result = paymentBLL.WithdrawalReq(authData.CustId, withdrawalReqModel.sysId, custBankInfo.AccountName, withdrawalReqModel.AccountNo, withdrawalReqModel.amount, withdrawalReqModel.memo, out afterAmount, out transId);
                if (result == 0)
                {
                    apiResult.ErrorCode = 0;
                    apiResult.Message = selLangBLL.GetMsg("msg_WithdrawalSuccessful");
                }
                else
                {
                    if (mainWalletType == 2)
                    {
                        transferToSabaResult = null;
                        var input = new TpTransfer();
                        input.Amount = walletBLL.GetBalance(authData.CustId);
                        input.CustId = authData.CustId;
                        input.direction = TpDirection.ToTp;
                        input.TpId = 1;
                        transferToSabaResult = await TransferToSabaAsync(input);
                    }

                    apiResult.ErrorCode = 9000;
                    apiResult.Message = selLangBLL.GetMsg("msg_WithdrawalFailed");
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
        }

        [HttpPost]
        public ApiResult<BankInfoPayFront> GetOnlineBankInfo(OnlineBankInfoReq onlineBankInfoReq)
        {
            var authData = authorizeBLL.GetAuthData();
            var apiResult = new ApiResult<BankInfoPayFront>();
            var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);
            apiResult.Data = bankInfoBLL.GetOnlineBankInfo(custInfo.CustId, onlineBankInfoReq.sysId);
            apiResult.ErrorCode = 0;
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
        public ApiResult UploadReceipt([FromForm]DepositReceiptFile depositReceipt)
        {
            decimal xSize = 500m;
            var apiResult = new ApiResult();
            var authData = authorizeBLL.GetAuthData();
            decimal xCell = 1;
            if (_contentTypesCheck.ContainsKey(depositReceipt.Receipt.ContentType))
            {
                using var fileStream = depositReceipt.Receipt.OpenReadStream();
                using var uploadedImage = Image.FromStream(fileStream);
                using var ms = new System.IO.MemoryStream();

                if (uploadedImage.Width > xSize || uploadedImage.Height > xSize)
                {

                    if (uploadedImage.Width > uploadedImage.Height)
                    {
                        xCell = xSize / uploadedImage.Width;
                    }
                    else
                    {
                        xCell = xSize / uploadedImage.Height;
                    }

                }

                var graphicOptions = new GraphicOptions();
                graphicOptions.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
                graphicOptions.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;
                var img = ImageResize.Scale(uploadedImage, Convert.ToInt32(uploadedImage.Width * xCell), Convert.ToInt32(uploadedImage.Height * xCell), graphicOptions);
                img.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);

                string fileName = depositReceipt.Receipt.FileName;
                fileName = fileName.Substring(fileName.LastIndexOf('\\') + 1);
                fileName = $"{fileName.Substring(0, fileName.LastIndexOf('.'))}.jpg";

                byte[] fileBytes = ms.ToArray();
                var result = paymentBLL.SetDepositReceiptPic(authData.CustId, depositReceipt.TransId, fileName, fileBytes);

                if (result > 0)
                {
                    apiResult.ErrorCode = 0;
                }
                else
                {
                    apiResult.ErrorCode = -1;
                }
            }
            else
            {

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
                    var input = new TpTransfer();
                    input.Amount = walletBLL.GetBalance(authData.CustId);
                    input.CustId = authData.CustId;
                    input.direction = TpDirection.ToTp;
                    input.TpId = 1;
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

        private class UserBalance
        {
            public decimal balance { get; set; }
            public decimal outstanding { get; set; }
            public byte Currency { get; set; }
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

            return data.ErrorCode == 0 ? data.Data.balance : 0;
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
            decimal amount = inTpTransfer.Amount;
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
    }
}