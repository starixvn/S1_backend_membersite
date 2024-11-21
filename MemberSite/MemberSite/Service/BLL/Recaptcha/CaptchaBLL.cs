using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.BLL;

public class CaptchaBLL
{
    private readonly RecaptchaService _recaptchaSvc;
    private readonly ServerInfoBll _serverInfoBll;
    private readonly CheckIpBLL _checkIpBll;
    private readonly GoogleRecaptchaBLL _googleRecaptchaBll;
    private readonly CloudflareTurnstileBLL _cloudflareTurnstileBll;
    private readonly NumberCaptchaBLL _numberCaptchaBll;
    private readonly Tools _tools;
    private readonly ILogger<CaptchaBLL> _logger;

    public CaptchaBLL(IServiceProvider provider)
    {
        _recaptchaSvc = provider.GetRequiredService<RecaptchaService>();
        _serverInfoBll = provider.GetRequiredService<ServerInfoBll>();
        _checkIpBll = provider.GetRequiredService<CheckIpBLL>();

        _googleRecaptchaBll = provider.GetRequiredService<GoogleRecaptchaBLL>();
        _cloudflareTurnstileBll = provider.GetRequiredService<CloudflareTurnstileBLL>();
        _numberCaptchaBll = provider.GetRequiredService<NumberCaptchaBLL>();
        _tools = provider.GetRequiredService<Tools>();

        _logger = provider.GetRequiredService<ILogger<CaptchaBLL>>();
    }

    public async Task<RecaptchaSiteInfo> GetInfo()
    {
        var clientIp = _tools.GetClientIP();
        var setting = await GetCaptchaSetting();

        var country = await _checkIpBll.CheckIp(clientIp);
        return new RecaptchaSiteInfo()
        {
            Country = country.Code,
            SiteKey = setting.SiteKey,
            Type = setting.Type
        };
    }

    public NumberCaptchaResult GetNumberCaptcha(string clientIp, string action)
    {
        if (!_numberCaptchaBll.GenPicture(clientIp, action, out byte[] picture, out int seconds))
        {
            _logger.LogInformation("GenPicture Fail.");
            return null;
        }
        return new NumberCaptchaResult() { Seconds = seconds, Picture = picture };
    }

    public async Task<bool> VerifyAsync(CaptchaType type, CaptchaCommonRequest request)
    {
        var resetRequest = ResetValue(request);
        switch (type)
        {
            case CaptchaType.GoogleRecaptcha:
                {
                    var setting = await GetCaptchaSetting();
                    return await _googleRecaptchaBll.VerifyAsync(setting, resetRequest);
                }
            case CaptchaType.CloudflareTurnstile:
                {
                    var setting = await GetCaptchaSetting();
                    return _cloudflareTurnstileBll.Verify(setting, resetRequest);
                }
            case CaptchaType.Number:
                {
                    var clientIp = _tools.GetClientIP();
                    return _numberCaptchaBll.Verify(clientIp, resetRequest.Token, resetRequest.Action);
                }
            default:
                return false;
        }
    }

    private CaptchaCommonRequest ResetValue(CaptchaCommonRequest request)
    {
        request.MainDomain = _serverInfoBll.GetMainDomain();
        request.SiteId = _serverInfoBll.GetServerInfo().SiteId;
        return request;
    }

    private async Task<RecaptchaGroup> GetCaptchaSetting()
    {
        var domain = _serverInfoBll.GetMainDomain();
        var site = _serverInfoBll.GetServerInfo();
        var recaptchaGroup = await _recaptchaSvc.GetGroup(domain);

        // recaptchaGroup是turnstile的話，代表走cloudflare turnstile驗證
        // recaptchaGroup是空白或是null的話，代表Siebog.Site沒有設定，走數字驗證碼
        // 其他狀況屬google驗證, 會啟用google recaptcha 企業版驗證
        switch (recaptchaGroup)
        {
            case "turnstile":
                return _cloudflareTurnstileBll.GetSetting(site.SiteId);
            case var value when string.IsNullOrWhiteSpace(value):
                return new RecaptchaGroup(CaptchaType.Number);
            default:
                return _googleRecaptchaBll.GetSetting(recaptchaGroup);
        }
    }


}
