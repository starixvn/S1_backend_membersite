using MemberSite.Service.Library;
using MemberSite.Service.Model;
using Microsoft.Extensions.Options;

namespace MemberSite.Service.DAL;

public class ReCaptchaService
{
    private readonly IOptions<ReCaptchaSetting> _reCaptcha;
    private readonly IHttpHelper _httpHelper;

    public ReCaptchaService(IOptions<ReCaptchaSetting> reCaptcha, IHttpHelper httpHelper)
    {
        _reCaptcha = reCaptcha;
        _httpHelper = httpHelper;
    }

    public RecaptchaResponse Verify(string token)
    {
        var req = new
        {
            secret = _reCaptcha.Value.Secret,
            response = token
        };

        var postBody = $"secret={_reCaptcha.Value.Secret}&response={token}";
        var response = _httpHelper.FormPost<string ,RecaptchaResponse>(_reCaptcha.Value.ApiUrl, postBody);

        return response.Result;
    }
}