using MemberSite.Service.BLL.Interface;
using MemberSite.Service.Library;
using MemberSite.Service.Model;

namespace MemberSite.Service.BLL
{
    public class GoogleRecaptchaApi : IRecaptchaApi
    {
        private readonly IHttpHelper _httpHelper;

        public GoogleRecaptchaApi(IHttpHelper httpHelper)
        {
            _httpHelper = httpHelper;
        }
        public CaptchaCommonResponse Verify(string url, string secret, string token)
        {
            var postBody = $"secret={secret}&response={token}";
            var response = _httpHelper.FormPost<string, GoogleRecaptchaResponse>(url, postBody);
            return new CaptchaCommonResponse(response.Result);
        }
    }
}