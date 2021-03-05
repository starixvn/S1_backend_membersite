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
                if (depositLimitInfoList.Any())
                {
                    var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(1));
                    cache.Set(key, depositLimitInfoList, cacheEntryOptions);
                }
            }

            return depositLimitInfoList;
        }
        public void FixDepositLimit(IPaymentLimit paymentLimit, DepositLimitInfo depositLimitInfo)
        {
            if (depositLimitInfo.DepositMin > paymentLimit.MaxAmount || depositLimitInfo.DepositMax < paymentLimit.MinAmount)
            {
                paymentLimit.MaxAmount = -1;
            }
            else
            {
                if (paymentLimit.MaxAmount == 0 || (paymentLimit.MaxAmount > depositLimitInfo.DepositMax && depositLimitInfo.DepositMax != 0))
                {
                    paymentLimit.MaxAmount = depositLimitInfo.DepositMax;
                }

                if (paymentLimit.MinAmount == 0 || paymentLimit.MinAmount < depositLimitInfo.DepositMin)
                {
                    paymentLimit.MinAmount = depositLimitInfo.DepositMin;
                }

                if (paymentLimit.MinAmount > paymentLimit.MaxAmount)
                {
                    paymentLimit.MaxAmount = paymentLimit.MinAmount;
                }
            }
        }
    }
}
