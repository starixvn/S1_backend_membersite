using MemberSite.Service.BLL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using Microsoft.Extensions.Options;

namespace MemberSite.Service.DAL;

public class ReCaptchaService
{
    private readonly IOptions<ReCaptchaSetting> _reCaptcha;
    private readonly ServerInfoBll _serverInfo;
    private readonly IHttpHelper _httpHelper;

    private readonly List<string> _extraDomain = new()
    {
        "nova88101.net", "nova88102.net", "nova88103.net", "nova88105.net", "nova88106.net", "nova88801.net",
        "nova88mas801.com", "nova88bkk801.com", "nova88india801.com", "localhost"
    };

    public ReCaptchaService(IOptions<ReCaptchaSetting> reCaptcha, IHttpHelper httpHelper, ServerInfoBll serverInfo)
    {
        _reCaptcha = reCaptcha;
        _httpHelper = httpHelper;
        _serverInfo = serverInfo;
    }

    public RecaptchaResponse Verify(string token)
    {
        var recaptcha = _reCaptcha.Value;
        var domain = _serverInfo.GetMainDomain();
        var serverInfo = _serverInfo.GetServerInfo();
        var site = recaptcha.Groups[serverInfo.SiteName];
        if (_extraDomain.Contains(domain))
        {
            site = recaptcha.Groups[$"{serverInfo.SiteName}-2"];
        }
        var postBody = $"secret={site.Secret}&response={token}";
        var response = _httpHelper.FormPost<string ,RecaptchaResponse>(recaptcha.ApiUrl, postBody);

        return response.Result;
    }
}