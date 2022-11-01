using MemberSite.Service.BLL.Interface;
using MemberSite.Service.DAL;
using MemberSite.Service.Model;
using Microsoft.Extensions.Options;

namespace MemberSite.Service.BLL
{
    public class RecaptchaBLL
    {
        private readonly RecaptchaSetting _recaptcha;
        private readonly RecaptchaService _recaptchaSvc;
        private readonly ServerInfoBll _serverInfoBll;
        private readonly IGeoIpApi _geoIpApi;
        private readonly IRecaptchaApi _recaptchaApi;

        public RecaptchaBLL(IOptions<RecaptchaSetting> reCaptcha, RecaptchaService recaptchaSvc, IGeoIpApi geoIpApi, IRecaptchaApi recaptchaApi, ServerInfoBll serverInfoBll)
        {
            _recaptcha = reCaptcha.Value;
            _recaptchaSvc = recaptchaSvc;
            _geoIpApi = geoIpApi;
            _recaptchaApi = recaptchaApi;
            _serverInfoBll = serverInfoBll;
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

            var country = await _geoIpApi.CheckIp(clientIp);
            return new RecaptchaSiteInfo()
            {
                Country = country.Code,
                SiteKey = setting.SiteKey
            };
        }
    }
}