using MemberSite.Service.BLL;
using MemberSite.Service.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MemberSite.Controllers.Api
{
    [Authorize]
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class TelegramController : ControllerBase
    {
        private readonly AuthorizeBLL _authorizeBll;
        private readonly SelLangBLL _selLangBll;
        private readonly TelegramBotConfig _telegramBotConfig;
        public TelegramController(IServiceProvider provider)
        {
            _authorizeBll = provider.GetRequiredService<AuthorizeBLL>();
            _selLangBll = provider.GetRequiredService<SelLangBLL>();
            var configBll = provider.GetRequiredService<ConfigBLL>();
            _telegramBotConfig = configBll.GetConfigValue<TelegramBotConfig>("TelegramBot");
        }

        public class TelegramBot
        {
            public string Url { get; set; }
        }
        public class TelegramBotConfig
        {
            public string Url { get; set; }
            public string Token { get; set; }
            public string UserName { get; set; }
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

            var data = new TelegramBot()
            {
                Url = $"{_telegramBotConfig.Url}/{_telegramBotConfig.UserName}?start={authData.CustId}"
            };

            apiResult.Data = data;
            apiResult.ErrorCode = 0;
            apiResult.Message = "";

            return apiResult;
        }
    }
}
