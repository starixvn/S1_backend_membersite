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

        public IEnumerable<BlockIP> GetBlockIP()
        {
            return _iPService.GetBlockIP();
        }
    }
}
