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
        private readonly BankInfoBLL bankInfoBLL;
        private readonly AuthorizeBLL authorizeBLL;
        private readonly PaymentBLL paymentBLL;

        public PaymentController(BankInfoBLL bankInfoBLL, AuthorizeBLL authorizeBLL, PaymentBLL paymentBLL)
        {
            this.bankInfoBLL = bankInfoBLL;
            this.authorizeBLL = authorizeBLL;
            this.paymentBLL = paymentBLL;
        }

        [HttpGet]
        public IEnumerable<SysBankInfo> GetBankInfoList()
        {
            var authData = authorizeBLL.GetAuthData();
            return bankInfoBLL.GetBankInfoList(authData.CustId);
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
    }
}