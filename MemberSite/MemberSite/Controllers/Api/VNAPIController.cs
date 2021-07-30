using MemberSite.Service.BLL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using Microsoft.AspNetCore.Mvc;

namespace MemberSite.Controllers.Api
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class VNAPIController : ControllerBase
    {
        private readonly Common common;
        private readonly WalletBLL walletBLL;
        string key = "Dd36ygD";

        public VNAPIController(Common common, WalletBLL walletBLL)
        {
            this.common = common;
            this.walletBLL = walletBLL;
        }

        public class GetWithdrawalLimitReq
        {
            public int custid { get; set; }
            public string token { get; set; }
        }
        [HttpPost]
        public ApiResult<int> GetWithdrawalLimit(GetWithdrawalLimitReq getWithdrawalLimitReq)
        {
            string p = $"{getWithdrawalLimitReq.custid}|{key}";

            var apiResult = new ApiResult<int>();

            if (common.ToMD5(p) == getWithdrawalLimitReq.token)
            {
                decimal withdrawallimit = walletBLL.GetWithdrawalLimit(getWithdrawalLimitReq.custid);
                apiResult.Message = withdrawallimit.ToString();
                apiResult.ErrorCode = 0;
            }
            else
            {
                apiResult.ErrorCode = 98;
            }
            return apiResult;
        }

        public class SetNoRolloverReq
        {
            public int custid { get; set; }
            public decimal amount { get; set; }
            public string token { get; set; }
        }
        [HttpPost]
        public ApiResult<int> DeduteWithdrawalLimit(SetNoRolloverReq setNoRolloverReq)
        {
            string p = $"{setNoRolloverReq.custid}|{setNoRolloverReq.amount}|{key}";

            var apiResult = new ApiResult<int>();

            if (common.ToMD5(p) == setNoRolloverReq.token)
            {
                if (walletBLL.DeduteWithdrawalLimit(setNoRolloverReq.custid, setNoRolloverReq.amount) == 1)
                {
                    decimal withdrawallimit = walletBLL.GetWithdrawalLimit(setNoRolloverReq.custid);
                    apiResult.ErrorCode = 0;
                    apiResult.Message = withdrawallimit.ToString();
                }
                else
                {
                    apiResult.ErrorCode = 99;
                }
            }
            else
            {
                apiResult.ErrorCode = 98;
            }
            return apiResult;
        }
    }
}