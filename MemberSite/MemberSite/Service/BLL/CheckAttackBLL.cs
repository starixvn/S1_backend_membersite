using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.BLL
{
    public class CheckAttackBLL
    {
        private readonly ILogger<CheckAttackBLL> _logger;
        private readonly Tools _tools;
        private readonly IpHelper _ipHelper;
        private readonly ICacheHelper _cacheHelper;
        private readonly IPBLL _iPBll;

        public CheckAttackBLL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<CheckAttackBLL>>();
            _tools = provider.GetRequiredService<Tools>();
            _ipHelper = provider.GetRequiredService<IpHelper>();
            _cacheHelper = provider.GetRequiredService<ICacheHelper>();
            _iPBll = provider.GetRequiredService<IPBLL>();
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
                _ = await _cacheHelper.StringFireAndForgetSetAsync($"block:{clientIp}", string.Empty, TimeSpan.FromMinutes(15));
                return true;
            }

            return false;
        }
        
        public async Task<bool> CheckAttackForSignup()
        {
            var clientIp = _tools.GetClientIP();
            if (_ipHelper.IpInRange(clientIp) || clientIp == "18.139.199.197")
            {
                return false;
            }

            // 檢查白名單
            if (await _cacheHelper.KeyExistsAsync($"AllowedIp:{clientIp}", RedisDataBase.IP))
            {
                return false;
            }

            // 10分鐘相同IP註冊超過10次阻擋一小時
            var checkCountForShortTime = await _cacheHelper.StringIncrementAsync($"IP:Signup:{clientIp}", TimeSpan.FromMinutes(10));      
            if (checkCountForShortTime >= 10)
            {
                _logger.LogWarning($"auto block {clientIp} for 20 minutes.");
                _ = _cacheHelper.StringFireAndForgetSetAsync($"block:Signup:{clientIp}", string.Empty, TimeSpan.FromHours(1));
            }

            // 一小時相同IP註冊超過30次，自動封鎖IP，需要從後台手動解鎖
            var checkCountForBlock = await _cacheHelper.StringIncrementAsync($"IP:Signup:long:{clientIp}", TimeSpan.FromHours(1));
            if (checkCountForBlock >= 30)
            {
                _logger.LogWarning($"auto block {clientIp}, Please go to the backoffice to unlock it.");
                var memo = $"Auto Deny (/Site/Signup) - Attack: try Auto Signup, reason is too many requests";
                var setResult = _iPBll.SetBlockIp(new BlockIpRequest { IP = clientIp, Memo = memo, Operator = "System" });
                if (setResult > 0 && !_cacheHelper.KeyExists($"AutoDeny:Signup:{clientIp}"))
                {
                   _ = _cacheHelper.StringFireAndForgetSetAsync($"AutoDeny:Signup:{clientIp}", string.Empty, TimeSpan.FromHours(1));
                }
            }

            if (checkCountForShortTime >= 20 || checkCountForBlock >= 60 || _cacheHelper.KeyExists($"block:Signup:{clientIp}"))
            {
                return true;
            }

            return false;
        }
    }
}
