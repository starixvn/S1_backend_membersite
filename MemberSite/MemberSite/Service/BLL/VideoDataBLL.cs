using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using Microsoft.Extensions.Caching.Memory;

namespace MemberSite.Service.BLL
{
    public class VideoDataBll
    {
        private readonly VideoDataService _videoDataService;
        private readonly CacheHelper _cacheHelper;

        public VideoDataBll(IServiceProvider provider)
        {
            _videoDataService = provider.GetRequiredService<VideoDataService>();
            _cacheHelper = provider.GetRequiredService<CacheHelper>();
        }

        public VideoData GetVideoData(int SiteId,int CurrencyId, string LangId)
        {
            var key = $"GetVideoData_{SiteId}_{CurrencyId}_{LangId}";
            return _cacheHelper.TryGetOrCreate(key,
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(1)),
                i => _videoDataService.GetVideoData(SiteId,CurrencyId, LangId));
        }
    }
}
