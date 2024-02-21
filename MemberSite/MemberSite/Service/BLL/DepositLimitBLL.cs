using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.BLL
{
    public class DepositLimitBLL
    {
        private readonly DepositService _depositService;
        private readonly ICacheHelper _redisHelper;
        private readonly CustInfoBll _custInfoBll;

        public DepositLimitBLL(IServiceProvider provider)
        {
            _depositService = provider.GetRequiredService<DepositService>();
            _redisHelper = provider.GetRequiredService<ICacheHelper>();
            _custInfoBll = provider.GetRequiredService<CustInfoBll>();
        }

        public IEnumerable<DepositLimitInfo> GetDepositLimitInfoListWithCache(int siteId)
        {
            var key = $"DepositLimitInfoList:{siteId}";

            var dbData = _redisHelper.TryGetOrCreate(key, TimeSpan.FromMinutes(5),
                    () => _depositService.GetDepositLimitInfoList().Where(x => x.SiteId == siteId));
                return dbData;
        }
        
        public DepositLimitInfo GetDepositLimitInfo(int custId, DepositType depositType)
        {
            var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
            var vipLevel = _custInfoBll.GetPointLevelWithCache(custId);
            
            return GetDepositLimitInfoListWithCache(custInfo.SiteId)?.FirstOrDefault(x =>
                x.SiteId == custInfo.SiteId &&
                x.VipLevel == vipLevel &&
                x.DepositType == depositType &&
                x.CurrencyId == custInfo.CurrencyId);
        }

        public void FixDepositLimit(IPaymentLimit paymentLimit, DepositLimitInfo depositLimitInfo)
        {
            if ((paymentLimit.MaxAmount != 0 && depositLimitInfo.DepositMin > paymentLimit.MaxAmount)
                || depositLimitInfo.DepositMax < paymentLimit.MinAmount)
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
