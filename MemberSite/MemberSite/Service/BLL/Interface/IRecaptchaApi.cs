using MemberSite.Service.Model;

namespace MemberSite.Service.BLL.Interface
{
    public interface IRecaptchaApi
    {
        RecaptchaResponse Verify(string url, string secret, string token);
    }
}

