using MemberSite.Service.BLL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;

using Microsoft.AspNetCore.Mvc;

using static MemberSite.Extensions.MethodExtension;

namespace MemberSite.Controllers.Api
{

    [Route("api/[controller]/[action]")]
    [ApiController]
    public class FPSController : ControllerBase
    {
        private readonly AuthorizeBLL _authorizeBll;
        private readonly ILogger<FPSController> _logger;
        private readonly FPSBLL _fpsBll;
        private readonly Tools _tools;

        public FPSController(IServiceProvider provider)
        {
            _authorizeBll = provider.GetRequiredService<AuthorizeBLL>();
            _logger = provider.GetRequiredService<ILogger<FPSController>>();
            _fpsBll = provider.GetRequiredService<FPSBLL>();
            _tools = provider.GetRequiredService<Tools>();
        }


        [HttpPost]
        public async Task<ApiResult> ChecksAsync(FPSRequest fpsRequest)
        {
            var result = new ApiResult();
            try
            {
                var authData = _authorizeBll.GetAuthData();
                if (authData is { CustId: > 0 })
                {
                    return await _fpsBll.FPSChecksAsync(new FPSApiRequest
                    {
                        Account = authData.CustId.ToString(),
                        DI = fpsRequest.DI,
                        BI = fpsRequest.BI,
                        IP = _tools.GetClientIP(),
                        UserAgent = Request.Headers["User-Agent"],
                        DetecasAnalysis = fpsRequest.DetecasAnalysis,
                        AcceptsTypes = Request.Headers["Accept"].ToString(),
                        SessionKey = fpsRequest.SessionKey,
                        Token = fpsRequest.Token,
                        Url = fpsRequest.Url
                    });
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, GetCurrentMethod());
            }
            return result;
        }
    }
}
