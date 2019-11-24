using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MemberSite.Service.BLL;
using MemberSite.Service.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MemberSite.Controllers.Api
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class RegistController : ControllerBase
    {
        private readonly ServerInfoBLL serverInfoBLL;
        private readonly SelLangBLL selLangBLL;
        private readonly CustInfoBLL custInfoBLL;

        public RegistController(ServerInfoBLL serverInfoBLL, SelLangBLL selLangBLL, CustInfoBLL custInfoBLL)
        {
            this.serverInfoBLL = serverInfoBLL;
            this.selLangBLL = selLangBLL;
            this.custInfoBLL = custInfoBLL;
        }

        public class RegisteredModel
        {
            public string UserName { get; set; }
            public string Userpwd { get; set; }
            public int CurrencyId { get; set; }
            public string UserLang { get; set; }
        }

        [HttpPost]
        public ApiResult Registered([FromBody]RegisteredModel registeredModel)
        {
            var apiResult = new ApiResult(selLangBLL);
            int custId = 0;
            var result = custInfoBLL.AddCustInfo(registeredModel.UserName, registeredModel.Userpwd, registeredModel.CurrencyId, registeredModel.UserLang, out custId);
            if (result == -1)
            {
                apiResult.ErrorCode = result;
                apiResult.Message = selLangBLL.GetMsg("msg_ThisCurrencyNotSupport");
            }
            else if (result == -2)
            {
                apiResult.ErrorCode = result;/**/
                apiResult.Message = "";
            }
            else if (result == -3)
            {
                apiResult.ErrorCode = result;/**/
                apiResult.Message = "";
            }
            else if (custId == 0)
            {
                apiResult.ErrorCode = result;/**/
                apiResult.Message = "";
            }
            else
            {
                apiResult.ErrorCode = 0;
                apiResult.Message = selLangBLL.GetMsg("msg_Congratulations");
            }

            return apiResult;
        }
    }
}