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

        public IEnumerable<BlockIP> GetBlockIp(bool forceUseMainDb = false)
        {
            return _iPService.GetBlockIp(forceUseMainDb);
        }
        
        public int SetBlockIp(BlockIpRequest request)
        {
            return _iPService.SetBlockIp(request);
        }
    }
}
