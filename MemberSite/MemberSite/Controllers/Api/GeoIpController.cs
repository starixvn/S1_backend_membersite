using MemberSite.Service.BLL;
using MemberSite.Service.BLL.Interface;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MemberSite.Controllers.Api
{
    [AllowAnonymous]
    [Route("api/[controller]/[action]")]
    [Route("api/[controller]/[action]/{LangId}")]
    [ApiController]
    public class GeoIpController : Controller
    {
        private readonly Tools _tools;
        private readonly ICheckIpBLL _checkIpBll;

        public GeoIpController(IServiceProvider provider)
        {
            _tools = provider.GetRequiredService<Tools>();
            _checkIpBll = provider.GetRequiredService<ICheckIpBLL>();
        }

        [HttpGet, HttpPost]
        public async Task<ApiResult<IpCountryWithAccess>> CheckIp()
        {
            var result = await _checkIpBll.CheckIp(_tools.GetClientIP(), true);
            return new ApiResult<IpCountryWithAccess>()
            {
                ErrorCode = result is { Message: "Succeed" } ? (int)ErrorCode.Success : (int)ErrorCode.SystemException,
                Data = result
            };
        }
    }
}