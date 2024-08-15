using MemberSite.Service.BLL;
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
        private readonly CheckIpBLL _checkIpBll;

        public GeoIpController(IServiceProvider provider)
        {
            _tools = provider.GetRequiredService<Tools>();
            _checkIpBll = provider.GetRequiredService<CheckIpBLL>();
        }

        [HttpPost]
        public async Task<ApiResult<IpCountryWithAccess>> CheckIp()
        {
            var result = await _checkIpBll.CheckIp(_tools.GetClientIP());
            return new ApiResult<IpCountryWithAccess>()
            {
                ErrorCode = (int)ErrorCode.Success,
                Data = result
            };
        }

        [HttpGet]
        public async Task<ApiResult<List<string>>> GetCountryList()
        {
            return new ApiResult<List<string>>()
            {
                ErrorCode = (int)ErrorCode.Success,
                Data = _checkIpBll.CountryList
            };
        }
    }
}