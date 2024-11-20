using MemberSite.Service.Model;

namespace MemberSite.Service.BLL.Interface
{
    public interface IRecaptchaApi
    {
        CaptchaCommonResponse Verify(string url, string secret, string token);
    }
}

