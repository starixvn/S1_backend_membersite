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

        public TpController(CourierAdapter courierAdapter, IConfiguration configuration, Tools tools, SelLangBLL selLangBLL, AuthorizeBLL authorizeBLL)
        {
            this.courierAdapter = courierAdapter;
            this.configuration = configuration;
            this.tools = tools;
            this.selLangBLL = selLangBLL;
            this.authorizeBLL = authorizeBLL;
            apiUrl = configuration.GetValue<string>("TpService");
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

            var data = await courierAdapter.MyPostAsync<bool, TpInfo> (myUrl, tpInfo);

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
        }
        public enum TransferStatus
        {
            Success = 0, Fail = 1, Pending = 2, PendingD = 3
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

        [HttpPost]
        public async Task<ApiResult<TransferResult>> Transfer([FromBody]TpTransfer inTpTransfer)
        {
            var authData = authorizeBLL.GetAuthData();
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
            var data = await courierAdapter.MyPostAsync<TransferResult, TpTransfer>(myUrl, tpTransfer);

            ApiResult<TransferResult> result = data;
            result.SetSelLang(selLangBLL);

            return result;
        }

        public class UserBalance
        {
            public decimal balance { get; set; }
            public decimal outstanding { get; set; }
            public byte Currency { get; set; }
        }

        [HttpPost]
        public async Task<ApiResult<UserBalance>> GetBalance([FromBody]TpInfo inInfo)
        {
            var authData = authorizeBLL.GetAuthData();
            Dictionary<string, string> input = new Dictionary<string, string>();
            var myUrl = $"{apiUrl}GetBalance";
            var oTpInfo = new TpInfo
            {
                TpId = inInfo.TpId,
                CustId = authData.CustId,
                inputs = input
            };

            var data = await courierAdapter.MyPostAsync<UserBalance, TpInfo>(myUrl, oTpInfo);

            ApiResult<UserBalance> result = data;

            return result;
        }
    }
}