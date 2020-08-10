using System;
using System.Collections.Generic;
using System.IO;
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
    public class TpController : ControllerBase
    {
        private readonly CourierAdapter courierAdapter;
        private readonly IConfiguration configuration;
        private readonly Tools tools;
        private readonly SelLangBLL selLangBLL;
        private readonly AuthorizeBLL authorizeBLL;
        private readonly string apiUrl;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly WalletBLL walletBLL;
        private readonly BonusWalletBLL bonusWalletBLL;
        private readonly CustInfoBLL custInfoBLL;

        public TpController(CourierAdapter courierAdapter, IConfiguration configuration, Tools tools, SelLangBLL selLangBLL, AuthorizeBLL authorizeBLL, ThirdPartyBLL thirdPartyBLL,WalletBLL walletBLL, BonusWalletBLL bonusWalletBLL, CustInfoBLL custInfoBLL)
        {
            this.courierAdapter = courierAdapter;
            this.configuration = configuration;
            this.tools = tools;
            this.selLangBLL = selLangBLL;
            this.authorizeBLL = authorizeBLL;
            apiUrl = configuration.GetValue<string>("TpService");
            this.thirdPartyBLL = thirdPartyBLL;
            this.walletBLL = walletBLL;
            this.bonusWalletBLL = bonusWalletBLL;
            this.custInfoBLL = custInfoBLL;
        }

        public class TpInfo
        {
            public int TpId { set; get; }
            public int CustId { set; get; }
            public string LangId { set; get; }
            public int AppType { set; get; }
            public Dictionary<string, string> inputs { set; get; }
        }

        [HttpPost]
        public async Task<ApiResult> GotoGame(TpInfo inInfo)
        {
            Dictionary<string, string> input = new Dictionary<string, string>();
            ////input.Add("gameType", gameType);/**/
            ////input.Add("gameId", gameId);/*for mg*/

            var authData = authorizeBLL.GetAuthData();

            input.Add("Host", $"{Request.Scheme}://{Request.Host}{Request.PathBase}");
            input.Add("ClientIP", tools.GetClientIP());

            var myUrl = $"{apiUrl}GotoGame";
            var tpInfo = new TpInfo
            {
                TpId = inInfo.TpId,
                CustId = authData.CustId,
                LangId = selLangBLL.GetLangId(),
                AppType = 1,
                inputs = input
            };

            var data = await courierAdapter.MyPostAsync<bool, TpInfo>(myUrl, tpInfo);

            ApiResult<bool> result = data;
            result.SetSelLang(selLangBLL);

            return result;
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
            public decimal Bonus { get; set; }
            public decimal Rollover { get; set; }
            public decimal Turnover { get; set; }
            public int BonusStatus { get; set; }
        }
        public enum TransferStatus
        {
            Success = 0, Fail = 1, Pending = 2, PendingD = 3
        }

        public class TpTransfer
        {
            public int TpId { set; get; }
            public int CustId { set; get; }
            public string Description { set; get; }
            public decimal Amount { set; get; }
            public TpDirection direction { set; get; }
            public Dictionary<string, string> inputs { set; get; }
        }

        public enum TpDirection
        {
            ToTp = 1,
            ToMain = 2
        }

        public class TpTransferReq
        {
            public int TpId { set; get; }
            public decimal Amount { set; get; }
            public TpDirection direction { set; get; }
        }

        [HttpPost]
        public async Task<ApiResult<TransferResult>> Transfer([FromBody]TpTransferReq inTpTransfer)
        {
            var authData = authorizeBLL.GetAuthData();
            BonusWallet bonusWalletInfo = inTpTransfer.direction == TpDirection.ToMain ? bonusWalletBLL.CheckBonusWallet(authData.CustId, inTpTransfer.TpId) : null;
            ApiResult<TransferResult> result = new ApiResult<TransferResult>();
            if (bonusWalletInfo != null)
            {
                result.ErrorCode = 9;
                result.Data = new TransferResult() {
                    Bonus = bonusWalletInfo.Bonus,
                    Turnover = bonusWalletInfo.Turnover,
                    Rollover = bonusWalletInfo.Rollover,
                    BonusStatus = bonusWalletInfo.Status
                };

                return result;
            }
            else
            {
                decimal amount = inTpTransfer.Amount;
                if (amount.ToString().IndexOf(".") > -1)
                {
                    decimal.TryParse(amount.ToString("#.##"), out amount);
                }
                Dictionary<string, string> input = new Dictionary<string, string>();
                input.Add("Host", $"{Request.Scheme}://{Request.Host}{Request.PathBase}");
                input.Add("ClientIP", tools.GetClientIP());

                var tpTransfer = new TpTransfer
                {
                    TpId = inTpTransfer.TpId,
                    CustId = authData.CustId,
                    Amount = amount,
                    direction = inTpTransfer.direction,
                    inputs = input
                };
                var myUrl = $"{apiUrl}Transfer";
                result = await courierAdapter.MyPostAsync<TransferResult, TpTransfer>(myUrl, tpTransfer);
                if (result.ErrorCode != 0 && string.IsNullOrEmpty(result.Message) && !string.IsNullOrEmpty(result.RawMessage))
                {
                    if (result.RawErrorCode == "3")
                        result.Message = selLangBLL.GetMsg("msg_InsufficientFund");
                    else
                        result.Message = result.RawMessage;
                }
                else if (result.ErrorCode == 1201)
                {
                    result.Message = selLangBLL.GetMsg("msg_InsufficientFund");
                }

                result.SetSelLang(selLangBLL);
                return result;
            }
        }

        [HttpPost]
        public async Task<ApiResult<TransferResult>> RetrieveAll()
        {
            var authData = authorizeBLL.GetAuthData();
            var tpWallets = thirdPartyBLL.GetTpWallet(authData.CustId);
            ApiResult<TransferResult> result = new ApiResult<TransferResult>();

            foreach (var tpWallet in tpWallets?.Where(x => x.Transfer))
            {
                var TpBalance = await GetBalance(tpWallet.TpId);
                if (TpBalance.ErrorCode > 0)
                {
                    continue;
                }
                else if (TpBalance.Data.balance < 1)
                {
                    result.ErrorCode = 0;
                    return result;
                }

                decimal amount = TpBalance.Data.balance;
                if (amount.ToString().IndexOf(".") > -1)
                {
                    decimal.TryParse(amount.ToString("#.##"), out amount);
                }
                Dictionary<string, string> input = new Dictionary<string, string>();
                input.Add("Host", $"{Request.Scheme}://{Request.Host}{Request.PathBase}");
                input.Add("ClientIP", tools.GetClientIP());

                var tpTransfer = new TpTransfer
                {
                    TpId = tpWallet.TpId,
                    Description = tpWallet.Description,
                    CustId = authData.CustId,
                    Amount = amount,
                    direction = TpDirection.ToMain,
                    inputs = input
                };
                var myUrl = $"{apiUrl}Transfer";
                var data = await courierAdapter.MyPostAsync<TransferResult, TpTransfer>(myUrl, tpTransfer);
                result = data;
            }

            if (result.ErrorCode != 0 && string.IsNullOrEmpty(result.Message) && !string.IsNullOrEmpty(result.RawMessage))
            {
                result.Message = result.RawMessage;
            }

            result.SetSelLang(selLangBLL);
            return result;
        }

        public class TpReq
        {
            public int TpId { set; get; }
        }
        [HttpPost]
        public async Task<ApiResult<TransferResult>> AllTransferIn(TpReq tpReq)
        {
            var authData = authorizeBLL.GetAuthData();
            var tpWallets = thirdPartyBLL.GetTpWallet(authData.CustId);
            ApiResult<TransferResult> result = new ApiResult<TransferResult>();
            var myUrl = $"{apiUrl}Transfer";

            foreach (var tpWallet in tpWallets?.Where(x => x.Transfer && x.TpId != tpReq.TpId))
            {
                var TpBalance = await GetBalance(tpWallet.TpId);
                if (TpBalance.ErrorCode > 0)
                {
                    continue;
                }
                else if (TpBalance.Data.balance < 1)
                {
                    result.ErrorCode = 0;
                    continue;
                }

                decimal amount = TpBalance.Data.balance;
                if (amount.ToString().IndexOf(".") > -1)
                {
                    decimal.TryParse(amount.ToString("#.##"), out amount);
                }
                Dictionary<string, string> input = new Dictionary<string, string>();
                input.Add("Host", $"{Request.Scheme}://{Request.Host}{Request.PathBase}");
                input.Add("ClientIP", tools.GetClientIP());

                var tpTransfer = new TpTransfer
                {
                    TpId = tpWallet.TpId,
                    CustId = authData.CustId,
                    Amount = amount,
                    direction = TpDirection.ToMain,
                    inputs = input
                };

                result = await courierAdapter.MyPostAsync<TransferResult, TpTransfer>(myUrl, tpTransfer);
            }

            decimal amount1 = walletBLL.GetBalance(authData.CustId);
            if (amount1 <= 0)
            {
                result.ErrorCode = 0;
                return result;
            }
            else if (amount1.ToString().IndexOf(".") > -1)
            {
                decimal.TryParse(amount1.ToString("#.##"), out amount1);
            }
            Dictionary<string, string> input1 = new Dictionary<string, string>();
            input1.Add("Host", $"{Request.Scheme}://{Request.Host}{Request.PathBase}");
            input1.Add("ClientIP", tools.GetClientIP());

            var tpTransfer1 = new TpTransfer
            {
                TpId = tpReq.TpId,
                CustId = authData.CustId,
                Amount = amount1,
                direction = TpDirection.ToTp,
                inputs = input1
            };

            result = await courierAdapter.MyPostAsync<TransferResult, TpTransfer>(myUrl, tpTransfer1);

            if (result.ErrorCode != 0 && string.IsNullOrEmpty(result.Message) && !string.IsNullOrEmpty(result.RawMessage))
            {
                result.Message = result.RawMessage;
            }

            result.SetSelLang(selLangBLL);
            return result;
        }
        [HttpPost]
        public async Task<ApiResult<TransferResult>> AllTransferOut(TpReq tpReq)
        {
            var authData = authorizeBLL.GetAuthData();
            var tpWallets = thirdPartyBLL.GetTpWallet(authData.CustId);
            ApiResult<TransferResult> result = new ApiResult<TransferResult>();
            var TpBalance = await GetBalance(tpReq.TpId);
            if (TpBalance.Data.balance < 1)
            {
                result.ErrorCode = 0;
                return result;
            }

            decimal amount = TpBalance.Data.balance;
            if(amount <= 0)
            {
                result.ErrorCode = 0;
                return result;
            }
            else if (amount.ToString().IndexOf(".") > -1)
            {
                decimal.TryParse(amount.ToString("#.##"), out amount);
            }
            Dictionary<string, string> input = new Dictionary<string, string>();
            input.Add("Host", $"{Request.Scheme}://{Request.Host}{Request.PathBase}");
            input.Add("ClientIP", tools.GetClientIP());

            var tpTransfer = new TpTransfer
            {
                TpId = tpReq.TpId,
                CustId = authData.CustId,
                Amount = amount,
                direction = TpDirection.ToMain,
                inputs = input
            };
            var myUrl = $"{apiUrl}Transfer";
            var data = await courierAdapter.MyPostAsync<TransferResult, TpTransfer>(myUrl, tpTransfer);
            result = data;

            if (result.ErrorCode != 0 && string.IsNullOrEmpty(result.Message) && !string.IsNullOrEmpty(result.RawMessage))
            {
                result.Message = result.RawMessage;
            }

            result.SetSelLang(selLangBLL);
            return result;
        }
        public async Task<ApiResult<UserBalance>> GetBalance(int tpId)
        {
            var authData = authorizeBLL.GetAuthData();
            Dictionary<string, string> input = new Dictionary<string, string>();
            var myUrl = $"{apiUrl}GetBalance";
            var oTpInfo = new TpInfo
            {
                TpId = tpId,
                CustId = authData.CustId,
                inputs = input
            };

            var data = await courierAdapter.MyPostAsync<UserBalance, TpInfo>(myUrl, oTpInfo);

            ApiResult<UserBalance> result = data;

            return result;
        }
        public class HistoryReq
        {
            public long StartDate { get; set; }
            public long EndDate { get; set; }
            public int PageNumber { get; set; }
            //public int PageSize { get; set; }
        }
        [HttpPost]
        public ApiListResult<TpTransferInfoHistory> GetTransferHistory(HistoryReq historyReq)
        {
            var authData = authorizeBLL.GetAuthData();
            var apiResult = new ApiListResult<TpTransferInfoHistory>();
            int pageNumber = historyReq.PageNumber;
            int totalPages;
            int rowCount;
            apiResult.Data = thirdPartyBLL.GetTransferHistory(authData.CustId, historyReq.StartDate, historyReq.EndDate, ref pageNumber, out totalPages, out rowCount);
            apiResult.PageNumber = pageNumber;
            apiResult.TotalPages = totalPages;
            apiResult.RowCount = rowCount;
            apiResult.ErrorCode = 0;
            return apiResult;
        }
        public ApiResult<IEnumerable<TpTransferInfoHistory>> GetTransferRecent()
        {
            var authData = authorizeBLL.GetAuthData();
            var apiResult = new ApiResult<IEnumerable<TpTransferInfoHistory>>();
            apiResult.Data = thirdPartyBLL.GetTransferRecent(authData.CustId);
            apiResult.ErrorCode = 0;
            return apiResult;
        }
    }
}