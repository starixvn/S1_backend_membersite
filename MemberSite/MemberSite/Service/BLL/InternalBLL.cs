using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using Newtonsoft.Json;

namespace MemberSite.Service.BLL
{
    public class InternalBLL
    {
        private readonly ILogger<InternalBLL> _logger;
        private readonly InternalService _internalService;
        private readonly ICacheHelper _cacheHelper;
        public InternalBLL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<InternalBLL>>();
            _internalService  =  provider.GetRequiredService<InternalService>();
            _cacheHelper = provider.GetRequiredService<ICacheHelper>();
            
        }
        public Task<IEnumerable<HotEventInfo>> GetHotEventDataList(int siteId)
        {
            var result = _internalService.GetHotEventDataList(siteId);
            return result;
        }
        
        public async Task<bool> RegisterRejectClickInsert(RegisterRejectClick inputs)
        {
            try
            {
                //前端打API的時間就是ClickTime
                inputs.ClickTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var jsonEntry = JsonConvert.SerializeObject(inputs);
                var score = DateTime.Parse(inputs.ClickTime).ToOADate();
                var result = await _cacheHelper.SortedSetAddAsync("RegisterRejectClickCount",jsonEntry,score,RedisDataBase.IP);
                
                return result;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return false;
            }
        }

    }
}