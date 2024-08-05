using MemberSite.Service.DAL;
using MemberSite.Service.Model;

namespace MemberSite.Service.BLL
{
    public class InternalBLL
    {
        private readonly ILogger<InternalBLL> _logger;
        private readonly InternalService _internalService;
        public InternalBLL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<InternalBLL>>();
            _internalService  =  provider.GetRequiredService<InternalService>();
            
        }
        public Task<IEnumerable<HotEventInfo>> GetHotEventDataList(int siteId)
        {
            var result = _internalService.GetHotEventDataList(siteId);
            return result;
        }

    }
}