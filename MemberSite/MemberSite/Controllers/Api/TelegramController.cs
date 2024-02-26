using MemberSite.Service.BLL;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace MemberSite.Controllers.Api
{
    [Authorize]
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class TelegramController : ControllerBase
    {
        private readonly AuthorizeBLL _authorizeBll;
        private readonly SelLangBLL _selLangBll;
        private readonly CustInfoBll _custInfoBLL;
        private readonly TelegramBLL _telegramBLL;

        public TelegramController(IServiceProvider provider)
        {
            _authorizeBll = provider.GetRequiredService<AuthorizeBLL>();
            _selLangBll = provider.GetRequiredService<SelLangBLL>();
            _custInfoBLL = provider.GetRequiredService<CustInfoBll>();
            _telegramBLL = provider.GetRequiredService<TelegramBLL>();
        }

        public class TelegramBot
        {
            public string Url { get; set; }
        }
        public class SabaConfig
        {
            public string ApiUrl { get; set; }
            public string VendorId { get; set; }
        }
        [HttpPost]
        [AllowAnonymous]
        public async Task<ApiResult<TelegramBot>> GetTelegramBot()
        {
            var apiResult = new ApiResult<TelegramBot>(_selLangBll);
            var authData = _authorizeBll.GetAuthData();

            if (authData is not { CustId: > 0 })
            {
                apiResult.Message = _selLangBll.GetMsg("msg_ForceLogedout");
                return apiResult;
            }

            var tpCustInfo = _custInfoBLL.GetTpCustInfo(authData.CustId, (int)Tp.SBSW);

            var tpSiteConfigs = _telegramBLL.GetTpSiteConfig().ToList();
            var sbswConfig = tpSiteConfigs.Find(x => x.SiteId == tpCustInfo.SiteId && x.ConfigKey == Tp.SBSW.ToString());
            var config = JsonConvert.DeserializeObject<SabaConfig>(sbswConfig.ConfigValue);

            var result = _telegramBLL.GetTelegramUrl(config.ApiUrl, config.VendorId, tpCustInfo.TpUserName);

            if(result.error_code == 0)
            {
                apiResult.Data = new TelegramBot()
                {
                    Url = result.Data.url
                };
                apiResult.ErrorCode = 0;
                apiResult.Message = "";
            }

            return apiResult;
        }
    }
}
