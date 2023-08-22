using MemberSite.Service.Model;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Newtonsoft.Json;

using Serilog.Context;

namespace MemberSite.Controllers.Api
{

    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize]
    public class FrontEndLogController : ControllerBase
    {
        private readonly ILogger<FrontEndLogController> _logger;

        public FrontEndLogController(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<FrontEndLogController>>();
        }

        public class FrontEndLogDto
        {
            public string UserName { get; set; }

            public string ApiPath { get; set; }

            public string LogBody { get; set; }

            /// <summary>
            /// Info、Error
            /// </summary>
            public string LogLevel { get; set; }

            public string Step { get; set; }

        }

        [HttpPost]
        public ApiResult LogMessage(FrontEndLogDto log)
        {
            var apiResult = new ApiResult() { ErrorCode = 0 };
            if (true) //不需要寫 ELK Log 就設定成 false
            {
                LogContext.PushProperty("UserName", log.UserName);
                LogContext.PushProperty("ApiPath", log.ApiPath);

                var logMessage = JsonConvert.SerializeObject(log);
                if (log.LogLevel.ToLower() == "error")
                {
                    _logger.LogError(logMessage);
                }
                else
                {
                    _logger.LogInformation(logMessage);
                }
            }

            return apiResult;
        }
    }
}