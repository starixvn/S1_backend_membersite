using MemberSite.Service.DAL;
using MemberSite.Service.Model;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemberSite.Service.BLL
{
    public class DepositBLL
    {
        private readonly DepoistService depoistService;
        private readonly IMemoryCache cache;

        public DepositBLL(IMemoryCache cache, DepoistService depoistService)
        {
            this.depoistService = depoistService;
            this.cache = cache;
        }

        public IEnumerable<DepositLimitInfo> GetDepositLimitInfoList(int siteId)
        {
            string key = $"DepositLimitInfoList_{siteId}";
            var depositLimitInfoList = cache.Get<IEnumerable<DepositLimitInfo>>(key);
            if (depositLimitInfoList == null)
            {
                depositLimitInfoList = depoistService.GetDepositLimitInfoList().Where(x => x.SiteId == siteId);
                if (depositLimitInfoList != null)
                {
                    var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
                    cache.Set(key, depositLimitInfoList, cacheEntryOptions);
                }
            }

            return depositLimitInfoList;
        }
    }
}
