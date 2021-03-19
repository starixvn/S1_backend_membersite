using System;
using System.Collections.Generic;
using MemberSite.Service.DAL;
using MemberSite.Service.Model;
using Microsoft.Extensions.Caching.Memory;

namespace MemberSite.Service.BLL
{
    public class LatestHighlightDataBLL
    {
        private readonly LatestHighlightDataService latestHighlightDataService;

        public LatestHighlightDataBLL(IMemoryCache cache, LatestHighlightDataService latestHighlightDataService)
        {
            this.latestHighlightDataService = latestHighlightDataService;
        }

        public LatestHighlightData GetLatestHighlightData(int SiteId, string LangId)
        {
            return latestHighlightDataService.GetLatestHighlightData(SiteId, LangId);
        }
    }
}
