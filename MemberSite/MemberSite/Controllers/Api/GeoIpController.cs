using MemberSite.Service.BLL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;

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
        private readonly GeoIpBLL _geoIpBll;

        public GeoIpController(IServiceProvider provider)
        {
            _tools = provider.GetRequiredService<Tools>();
            _geoIpBll = provider.GetRequiredService<GeoIpBLL>();
        }

        [HttpGet, HttpPost]
        public async Task<ApiResult<IpCountryModel>> CheckIp()
        {
            var clientIp = _tools.GetClientIP();
            return await _geoIpBll.CheckIp(clientIp);
        }
    }
}