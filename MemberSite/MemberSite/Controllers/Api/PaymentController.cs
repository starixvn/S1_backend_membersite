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
        public PaymentController(BankInfoOffBLL bankInfoOffBLL, AuthorizeBLL authorizeBLL, PaymentBLL paymentBLL)
        {
            this.bankInfoOffBLL = bankInfoOffBLL;
            this.authorizeBLL = authorizeBLL;
            this.paymentBLL = paymentBLL;
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
    }
}