using System.Net;

using MemberSite.Service.BLL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;

using Microsoft.AspNetCore.Routing.Template;
using Microsoft.Extensions.Caching.Memory;

namespace MemberSite.Extensions.Middlewares
{
    public class BlockMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly Tools _tools;
        private readonly CheckIpBLL _checkIpBll;
        private readonly IPBLL _iPBLL;
        private readonly CacheHelper _cacheHelper;


        public BlockMiddleware(IServiceProvider provider, RequestDelegate next)
        {
            _next = next;
            _tools = provider.GetRequiredService<Tools>();
            _checkIpBll = provider.GetRequiredService<CheckIpBLL>();
            _iPBLL = provider.GetRequiredService<IPBLL>();
            _cacheHelper = provider.GetRequiredService<CacheHelper>();
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {                          
                if (await IsBlockIP(context)) 
                {
                    context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                    return;
                }

                await _next(context);            
            }
            catch
            {
                await _next(context);
            }
        }

        private async Task<bool> IsBlockIP(HttpContext context)
        {
            var clientIp = _tools.GetClientIP(context);
            try
            {         
                return await _checkIpBll.CheckBlockIp(clientIp);
            }
            catch (Exception)
            {
                var _blockIp = _cacheHelper.TryGetOrCreate("GetBlockIP",
                    new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(10)),
                    i => _iPBLL.GetBlockIp<BlockIP>().ToDictionary(item => item.IP, item => item.IP));
                if (_blockIp == null || !_blockIp.Any())
                {
                    return false;
                }
                return _blockIp.TryGetValue(clientIp, out var _);
            }
        }
    }
}
