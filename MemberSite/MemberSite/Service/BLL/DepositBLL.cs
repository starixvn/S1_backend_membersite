using MemberSite.Service.DAL;
using MemberSite.Service.Model;
using MemberSite.Service.Library;

namespace MemberSite.Service.BLL
{
    public class DepositBLL
    {
        private readonly DepositService _depositService;
        private readonly ICacheHelper _redisHelper;
        
        public DepositBLL(IServiceProvider provider)
        {
            _depositService = provider.GetRequiredService<DepositService>();
            _redisHelper = provider.GetRequiredService<ICacheHelper>();
        }

        public IEnumerable<DepositLimitInfo> GetDepositLimitInfoList(int siteId)
        {
            var key = $"DepositLimitInfoList:{siteId}";
            var dbData = _redisHelper.TryGetOrCreate(key, TimeSpan.FromSeconds(10),
                () => _depositService.GetDepositLimitInfoList().Where(x => x.SiteId == siteId));
            return dbData;
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
