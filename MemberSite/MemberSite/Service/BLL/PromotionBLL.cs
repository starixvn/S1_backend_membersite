using MemberSite.Service.DAL;
using MemberSite.Service.Library;

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

        public bool SetDepositPromotionQueue(string transId, string bonusCode, int custId, int siteId, string userName, int currencyId, int deviceType, decimal lockedAmount)
        {
            return _promotionService.SetDepositPromotionQueue(transId, bonusCode, custId, siteId, userName, currencyId, deviceType, lockedAmount, _tools.GetClientIP());
        }
    }
}
