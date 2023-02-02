using MemberSite.Service.DAL;
using MemberSite.Service.Model;

namespace MemberSite.Service.BLL
{
    public class IPBLL
    {
        private readonly IPService _iPService;
        public IPBLL(IServiceProvider provider)
        {
            _iPService = provider.GetRequiredService<IPService>();
        }

        public IEnumerable<T> GetBlockIp<T>(bool forceUseMainDb = false)
        {
            return _iPService.GetBlockIp<T>(forceUseMainDb);
        }
        
        public int SetBlockIp(BlockIpRequest request)
        {
            return _iPService.SetBlockIp(request);
        }
    }
}
