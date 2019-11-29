using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MemberSite.Service.BLL;
using MemberSite.Service.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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

        public PaymentController(BankInfoOffBLL bankInfoOffBLL, AuthorizeBLL authorizeBLL, PaymentBLL paymentBLL, CustInfoBLL custInfoBLL)
        {
            this.bankInfoOffBLL = bankInfoOffBLL;
            this.authorizeBLL = authorizeBLL;
            this.paymentBLL = paymentBLL;
            this.custInfoBLL = custInfoBLL;
        }

        [HttpGet]
        public IEnumerable<SysBankInfoOff> GetBankInfoOffList()
        {
            var authData = authorizeBLL.GetAuthData();
            return bankInfoOffBLL.GetBankInfoOffList(authData.CustId);
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
        public ApiResult WithdrawalReq(WithdrawalReqModel withdrawalReqModel)
        {
            ApiResult apiResult = new ApiResult();

            if (withdrawalReqModel.amount <= 0)
            {
                return null;
            }

            var authData = authorizeBLL.GetAuthData();
            var custBankInfo = custInfoBLL.GetCustBankInfo(authData.CustId);

            if (custBankInfo == null || custBankInfo.bankInfoDetail == null || custBankInfo.bankInfoDetail.Count() == 0)
            {
                return null;
            }
            else if (custBankInfo.bankInfoDetail.Where(x => x.SysId == withdrawalReqModel.sysId && x.AccountNo == withdrawalReqModel.AccountNo).Count() == 0)
            {
                return null;
            }

            decimal afterAmount = 0;
            string transId = "";

            var result = paymentBLL.WithdrawalReq(authData.CustId, withdrawalReqModel.sysId, custBankInfo.AccountName, withdrawalReqModel.AccountNo, withdrawalReqModel.amount, withdrawalReqModel.memo, out afterAmount, out transId);

            if (result != 0)
            {
                //apiResult
            }

            return apiResult;
        }
    }
}