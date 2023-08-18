using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;

namespace MemberSite.Service.BLL
{
    public class PromotionBll
    {
        private readonly PromotionService _promotionService;
        private readonly Tools _tools;

        public PromotionBll(IServiceProvider provider)
        {
            _promotionService = provider.GetRequiredService<PromotionService>();
            _tools = provider.GetRequiredService<Tools>();
        }

        public bool SetDepositPromotionQueue(string transId, string bonusCode, int custId, int siteId, string userName,
            int currencyId, int deviceType, decimal lockedAmount)
        {
            return SetDepositPromotionQueue(new DepositPromotionQueue
            {
                TransId = transId,
                BonusCode = bonusCode,
                CustId = custId,
                SiteId = siteId,
                UserName = userName,
                CurrencyId = currencyId,
                DeviceType = deviceType,
                LockedAmount = lockedAmount,
                ClientIp = _tools.GetClientIP()
            });
        }

        public bool SetDepositPromotionQueue(DepositPromotionQueue request)
        {
            return _promotionService.SetDepositPromotionQueue(request);
        }

        public bool VerifyBonusCode(string bonusCode)
        {
            return !string.IsNullOrWhiteSpace(bonusCode) && string.Compare(bonusCode, "undefined", StringComparison.OrdinalIgnoreCase) != 0;
        }
    }
}
