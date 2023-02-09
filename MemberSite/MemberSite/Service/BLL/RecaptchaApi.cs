using MemberSite.Service.BLL.Interface;
using MemberSite.Service.Library;
using MemberSite.Service.Model;

namespace MemberSite.Service.BLL
{
    public class RecaptchaApi : IRecaptchaApi
    {
        private readonly IHttpHelper _httpHelper;

        public RecaptchaApi(IHttpHelper httpHelper)
        {
            _httpHelper = httpHelper;
        }
        public RecaptchaResponse Verify(string url, string secret, string token)
        {
            var postBody = $"secret={secret}&response={token}";
            var response = _httpHelper.FormPost<string, RecaptchaResponse>(url, postBody);
            return response.Result;
        }
    }
}