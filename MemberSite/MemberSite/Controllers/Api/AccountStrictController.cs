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
    public class AccountStrictController : ControllerBase
    {
        private readonly SelLangBLL selLangBLL;
        private readonly AuthorizeBLL authorizeBLL;
        private readonly CustInfoBLL custInfoBLL;
        private readonly BankInfoBLL bankInfoBLL;

        public AccountStrictController(SelLangBLL selLangBLL, AuthorizeBLL authorizeBLL, CustInfoBLL custInfoBLL, BankInfoBLL bankInfoBLL)
        {
            this.selLangBLL = selLangBLL;
            this.authorizeBLL = authorizeBLL;
            this.custInfoBLL = custInfoBLL;
            this.bankInfoBLL = bankInfoBLL;
        }
        public class BankInfo
        {
            public int SysId { set; get; }
            public string AccountName { set; get; }
            public string AccountNo { set; get; }
        }

        [HttpPost]
        public ApiResult SetCustBankInfo(BankInfo bankInfo)
        {
            var apiResult = new ApiResult(selLangBLL);
            var authData = authorizeBLL.GetAuthData();
            List<BankInfoDetail> bankInfoList = null;

            var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);
            //
            var bankInfoSysList = bankInfoBLL.GetBankInfoSysListByCurrencyForCtrl(custInfo.CurrencyId);

            if (bankInfoSysList == null || bankInfoSysList.Where(x => x.SysId == bankInfo.SysId).Count() == 0)
            {
                apiResult.ErrorCode = -1;
                apiResult.Message = selLangBLL.GetMsg("msg_UpdateFailed");
                return apiResult;
            }

            var custBankInfo = custInfoBLL.GetCustBankInfo(authData.CustId);
            if (custBankInfo == null && string.IsNullOrEmpty(bankInfo.AccountName))
            {
                apiResult.ErrorCode = -2;
                apiResult.Message = selLangBLL.GetMsg("msg_UpdateFailed");
                return apiResult;
            }
            else if (custBankInfo != null && custBankInfo.BankInfo != null && custBankInfo.bankInfoDetail.Count() >= 3)
            {
                apiResult.ErrorCode = -3;
                apiResult.Message = selLangBLL.GetMsg("msg_UpdateFailed");
                return apiResult;
            }
            else if (custBankInfo != null && custBankInfo.bankInfoDetail != null)
            {
                bankInfoList = custBankInfo.bankInfoDetail.ToList();

                if (bankInfoList.Where(x => x.SysId == bankInfo.SysId && x.AccountNo == bankInfo.AccountNo).Count() > 0)
                {
                    apiResult.ErrorCode = -4;
                    apiResult.Message = selLangBLL.GetMsg("msg_UpdateFailed");
                    return apiResult;
                }
            }
            else
            {
                bankInfoList = new List<BankInfoDetail>();
            }

            bankInfoList.Add(new BankInfoDetail { AccountNo = bankInfo.AccountNo, SysId = bankInfo.SysId });

            apiResult.ErrorCode = custInfoBLL.SetCustBankInfo(authData.CustId, bankInfo.AccountName, bankInfoList);
            apiResult.Message = selLangBLL.GetMsg("msg_Updatesuccessful");
            return apiResult;
        }

        [HttpPost]
        public ApiResult DelCustBankInfo(BankInfo bankInfo)
        {
            var apiResult = new ApiResult(selLangBLL);
            var authData = authorizeBLL.GetAuthData();
            IEnumerable<BankInfoDetail> bankInfoList = null;

            var custBankInfo = custInfoBLL.GetCustBankInfo(authData.CustId);
            if (custBankInfo == null || custBankInfo.BankInfo == null)
            {
                apiResult.ErrorCode = -2;
                apiResult.Message = selLangBLL.GetMsg("msg_UpdateFailed");
                return apiResult;
            }
            else if (custBankInfo != null && custBankInfo.bankInfoDetail != null)
            {
                bankInfoList = custBankInfo.bankInfoDetail.Where(x => !(x.SysId == bankInfo.SysId && x.AccountNo == bankInfo.AccountNo));
            }

            apiResult.ErrorCode = custInfoBLL.SetCustBankInfo(authData.CustId, custBankInfo.AccountName, bankInfoList);
            apiResult.Message = selLangBLL.GetMsg("msg_Updatesuccessful");
            return apiResult;
        }
    }
}