using MemberSite.Service.DAL;
using MemberSite.Service.Model;
using Microsoft.Extensions.Caching.Memory;
using MemberSite.Service.Library;

namespace MemberSite.Service.BLL
{
    public class DepositBLL
    {
        private readonly DepositService _depositService;
        private readonly CacheHelper _cacheHelper;

        public DepositBLL(IServiceProvider provider)
        {
            _depositService = provider.GetRequiredService<DepositService>();
            _cacheHelper = provider.GetRequiredService<CacheHelper>();
        }

        public IEnumerable<DepositLimitInfo> GetDepositLimitInfoList(int siteId)
        {
            var key = $"DepositLimitInfoList_{siteId}";
            return _cacheHelper.TryGetOrCreate(key,
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(1)),
                i => _depositService.GetDepositLimitInfoList().Where(x => x.SiteId == siteId));
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
