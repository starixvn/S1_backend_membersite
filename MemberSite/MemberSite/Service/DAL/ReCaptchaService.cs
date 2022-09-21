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

    public ReCaptchaService(IOptions<Dictionary<string, ReCaptchaSetting>> reCaptcha, IHttpHelper httpHelper, ServerInfoBll serverInfo)
    {
        _reCaptcha = reCaptcha;
        _httpHelper = httpHelper;
        _serverInfo = serverInfo;
    }

    public RecaptchaResponse Verify(string token)
    {
        var serverInfo = _serverInfo.GetServerInfo();
        var reCaptchaConfig = _reCaptcha.Value[serverInfo.SiteName];

        var postBody = $"secret={reCaptchaConfig.Secret}&response={token}";
        var response = _httpHelper.FormPost<string ,RecaptchaResponse>(reCaptchaConfig.ApiUrl, postBody);

        return response.Result;
    }
}