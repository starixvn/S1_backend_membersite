using MemberSite.Service.Library;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.BLL
{
    public class CheckAttackBLL
    {
        private readonly ILogger<CheckAttackBLL> _logger;
        private readonly Tools _tools;
        private readonly IpHelper _ipHelper;
        private readonly ICacheHelper _cacheHelper;


        public CheckAttackBLL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<CheckAttackBLL>>();
            _tools = provider.GetRequiredService<Tools>();
            _ipHelper = provider.GetRequiredService<IpHelper>();
            _cacheHelper = provider.GetRequiredService<ICacheHelper>();
        }

        public async Task<bool> CheckAttackIp(HttpRequest Request, int limitCount)
        {
            var clientIp = _tools.GetClientIP();
            if (_cacheHelper.KeyExists($"AllowedIp:{clientIp}", RedisDataBase.IP))
            {
                return false;
            }

            if (_ipHelper.IpInRange(clientIp) || clientIp == "18.139.199.197")
            {
                return false;
            }

            // 檢查是否已經被block
            if (_cacheHelper.KeyExists($"block:{clientIp}"))
            {                
                return true;
            }

            // 根據path計算次數
            var checkCount = await _cacheHelper.StringIncrementAsync($"{Request.Path}:IP:{clientIp}", TimeSpan.FromMinutes(1));
            if (checkCount >= limitCount)
            {
                _ = await _cacheHelper.StringFireAndForgetSetAsync($"block:{clientIp}", string.Empty, TimeSpan.FromMinutes(10));
                return true;
            }

            return false;
        }  
    }
}
