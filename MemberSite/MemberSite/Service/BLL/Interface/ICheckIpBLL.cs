using MemberSite.Service.Model;

namespace MemberSite.Service.BLL.Interface
{
    public interface ICheckIpBLL
    {
        public Task<IpCountryWithAccess> CheckIp(string clientIp, bool isReturnClientIp = false);
    }
}
