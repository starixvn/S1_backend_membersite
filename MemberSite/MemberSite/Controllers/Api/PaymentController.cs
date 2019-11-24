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

        public PaymentController(BankInfoBLL bankInfoBLL, AuthorizeBLL authorizeBLL)
        {
            this.bankInfoBLL = bankInfoBLL;
            this.authorizeBLL = authorizeBLL;
        }

        [HttpGet]
        public IEnumerable<SysBankInfo> GetBankInfoList()
        {
            var authData = authorizeBLL.GetAuthData();
            return bankInfoBLL.GetBankInfoList(authData.CustId);
        }
    }
}