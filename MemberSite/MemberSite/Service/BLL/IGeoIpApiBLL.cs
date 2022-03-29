using MemberSite.Service.Model;

namespace MemberSite.Service.BLL
{
    public interface IGeoIpApiBLL
    {
        Task<IpCountry> CheckIp(string clientIp);
    }
}