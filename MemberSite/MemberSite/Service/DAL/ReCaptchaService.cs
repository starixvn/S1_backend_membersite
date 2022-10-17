using MemberSite.Service.BLL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using Microsoft.Extensions.Options;

namespace MemberSite.Service.DAL;

public class ReCaptchaService
{
    private readonly IOptions<Dictionary<string, ReCaptchaSetting>> _reCaptcha;
    private readonly ServerInfoBll _serverInfo;
    private readonly IHttpHelper _httpHelper;

    private readonly List<string> _extraDomain = new()
    {
        "nova88101.net", "nova88102.net", "nova88103.net", "nova88105.net", "nova88106.net", "nova88801.net",
        "nova88mas801.com", "nova88bkk801.com", "nova88india801.com", "localhost"
    };

    public ReCaptchaService(IOptions<Dictionary<string, ReCaptchaSetting>> reCaptcha, IHttpHelper httpHelper, ServerInfoBll serverInfo)
    {
        _reCaptcha = reCaptcha;
        _httpHelper = httpHelper;
        _serverInfo = serverInfo;
    }

    public RecaptchaResponse Verify(string token)
    {
        var domain = _serverInfo.GetMainDomain();
        var serverInfo = _serverInfo.GetServerInfo();
        var reCaptchaConfig = _reCaptcha.Value[serverInfo.SiteName];
        if (_extraDomain.Contains(domain))
        {
            reCaptchaConfig = _reCaptcha.Value[$"{serverInfo.SiteName}-2"];
        }
        var postBody = $"secret={reCaptchaConfig.Secret}&response={token}";
        var response = _httpHelper.FormPost<string ,RecaptchaResponse>(reCaptchaConfig.ApiUrl, postBody);

        return response.Result;
    }
}