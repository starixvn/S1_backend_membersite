using MemberSite.Service.Model;

namespace MemberSite.Service.BLL.Interface
{
    public interface IGeoIpApi
    {
        Task<IpCountry> CheckIp(string clientIp);
    }
}