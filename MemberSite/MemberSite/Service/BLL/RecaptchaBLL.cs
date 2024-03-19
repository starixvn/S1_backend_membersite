using MemberSite.Service.BLL.Interface;
using MemberSite.Service.DAL;
using MemberSite.Service.Model;

namespace MemberSite.Service.BLL
{
    public class RecaptchaBLL
    {
        private readonly RecaptchaSetting _recaptcha;
        private readonly RecaptchaService _recaptchaSvc;
        private readonly ServerInfoBll _serverInfoBll;
        private readonly IRecaptchaApi _recaptchaApi;
        private readonly CheckIpBLL _checkIpBll;

        public RecaptchaBLL(IServiceProvider provider)
        {
            _recaptcha = provider.GetRequiredService<RecaptchaSetting>();
            _recaptchaSvc = provider.GetRequiredService<RecaptchaService>();
            _recaptchaApi = provider.GetRequiredService<IRecaptchaApi>();
            _serverInfoBll = provider.GetRequiredService<ServerInfoBll>();
            _checkIpBll = provider.GetRequiredService<CheckIpBLL>();
        }

        public RecaptchaResponse Verify(string token)
        {
            var domain = _serverInfoBll.GetMainDomain();
            var name = _recaptchaSvc.GetGroup(domain).Result;
            var setting = _recaptcha.Groups[name];

            return _recaptchaApi.Verify(_recaptcha.ApiUrl, setting.Secret, token);
        }

        public async Task<RecaptchaSiteInfo> GetInfo(string clientIp)
        {
            var domain = _serverInfoBll.GetMainDomain();
            var name = await _recaptchaSvc.GetGroup(domain);
            var setting = _recaptcha.Groups[name];

            var country = await _checkIpBll.CheckIp(clientIp);
            return new RecaptchaSiteInfo()
            {
                Country = country.Code,
                SiteKey = setting.SiteKey
            };
        }
    }
}