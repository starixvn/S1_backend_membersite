using MemberSite.Service.BLL.Interface;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using Newtonsoft.Json;

namespace MemberSite.Service.BLL
{
    public class CloudflareTurnstileBLL
    {
        private readonly IRecaptchaApi _cloudflareTurnstileApi;
        private readonly ILogger<GoogleRecaptchaBLL> _logger;

        private readonly Dictionary<int, RecaptchaGroup> _cloudflareTurnstileConfig;
        private readonly CloudflareBaseSetting _recaptchaBaseSettingConfig;

        public CloudflareTurnstileBLL(IServiceProvider provider)
        {
            var recaptchaFactory = provider.GetRequiredService<Func<string, IRecaptchaApi>>();
            string selectedType = "Cloudflare"; 
            _cloudflareTurnstileApi = recaptchaFactory(selectedType);

            var configBll = provider.GetRequiredService<ConfigBLL>();
            _cloudflareTurnstileConfig = configBll.GetConfigValuesBySiteId<RecaptchaGroup>("CloudflareKeySetting");
            _recaptchaBaseSettingConfig = configBll.GetConfigValue<CloudflareBaseSetting>("CloudflareBaseSetting");
        }

        public RecaptchaGroup GetSetting(int siteId)
        {
            if (!_cloudflareTurnstileConfig.TryGetValue(siteId, out var config))
            {
                _logger.LogInformation($"cloudflare-{siteId} config not found.");
                return null;
            }

            if (config == null)
            {
                _logger.LogInformation($"cloudflare-{siteId} config is null.");
                return null;
            }

            return new RecaptchaGroup(CaptchaType.CloudflareTurnstile, config);
        }

        public bool Verify(RecaptchaGroup setting, CaptchaCommonRequest request)
        {
            var url = $"{_recaptchaBaseSettingConfig.ApiUrl}{_recaptchaBaseSettingConfig.SiteVerifyUrl}";
            var response = _cloudflareTurnstileApi.Verify(url, setting.Secret, request.Token);
            _logger.LogInformation($"Turnstile verify result={JsonConvert.SerializeObject(response)}");
            if (response.Success)
            {
                if (string.IsNullOrWhiteSpace(response.Action) && request.Action == request.ExpectedAction)
                {
                    return true;
                }
                else
                {
                    if (response.Action == request.Action)
                    {
                        _logger.LogInformation($"Verification successful. onse.action({response.Action}) vs request.action({request.Action})");
                        return true;
                    }
                    else
                    {
                        _logger.LogInformation($"Verification failed. response.action({response.Action}) vs request.action({request.Action}) not match.");
                        return false;
                    }
                }

            }
            return false;
        }
    }
}
