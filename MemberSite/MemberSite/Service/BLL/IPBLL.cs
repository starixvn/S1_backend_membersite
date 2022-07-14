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

        public async Task<IEnumerable<BlockIP>> GetBlockIP()
        {
            return await _iPService.GetBlockIP();
        }
    }
}
