using System;
using System.Collections.Generic;
using MemberSite.Service.DAL;
using MemberSite.Service.Model;
using Microsoft.Extensions.Caching.Memory;

namespace MemberSite.Service.BLL
{
    public class LatestHighlightDataBLL
    {
        private readonly IMemoryCache cache;
        private readonly LatestHighlightDataService latestHighlightDataService;

        public LatestHighlightDataBLL(IMemoryCache cache, LatestHighlightDataService latestHighlightDataService)
        {
            this.cache = cache;
            this.latestHighlightDataService = latestHighlightDataService;
        }

        public LatestHighlightData GetLatestHighlightData(int SiteId, string LangId)
        {
            var key = $"GetLatestHighlightData_{SiteId}_{LangId}";
            var latestHighlightData = cache.Get<LatestHighlightData>(key);
            if (latestHighlightData != null)
            {
                return latestHighlightData;
            }

            latestHighlightData = latestHighlightDataService.GetLatestHighlightData(SiteId, LangId);

            if (latestHighlightData != null)
            {
                cache.Set(key, latestHighlightData, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(1)));
            }

            return latestHighlightData;
        }
    }
}
