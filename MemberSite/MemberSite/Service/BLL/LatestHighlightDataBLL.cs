using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using Microsoft.Extensions.Caching.Memory;

namespace MemberSite.Service.BLL
{
    public class LatestHighlightDataBll
    {
        private readonly LatestHighlightDataService _latestHighlightDataService;
        private readonly CacheHelper _cacheHelper;

        public LatestHighlightDataBll(IServiceProvider provider)
        {
            _latestHighlightDataService = provider.GetRequiredService<LatestHighlightDataService>();
            _cacheHelper = provider.GetRequiredService<CacheHelper>();
        }

        public LatestHighlightData GetLatestHighlightData(int SiteId, string LangId)
        {
            var key = $"GetLatestHighlightData_{SiteId}_{LangId}";
            return _cacheHelper.TryGetOrCreate(key,
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(1)),
                i => _latestHighlightDataService.GetLatestHighlightData(SiteId, LangId));
        }
    }
}
