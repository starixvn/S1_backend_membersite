using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;

namespace MemberSite.Service.BLL
{
    public class PromotionBll
    {
        private readonly PromotionService _promotionService;
        private readonly CourierAdapter _courierAdapter;
        private readonly string _bonusServiceUrl;
        private readonly Tools _tools;

        public PromotionBll(IServiceProvider provider)
        {
            _promotionService = provider.GetRequiredService<PromotionService>();
            _courierAdapter = provider.GetRequiredService<CourierAdapter>();
            _bonusServiceUrl = provider.GetRequiredService<IConfiguration>().GetValue<string>("BonusService");
            _tools = provider.GetRequiredService<Tools>();
        }

        public async Task<ApiResult> SetClaimPromotionV2Async(int custId, string bonusCode, decimal? dLockedAmount, int? deviceType)
        {
            var result = await _promotionService.SetClaimPromotionV2Async(custId, bonusCode, dLockedAmount, deviceType);

            return result;
        }

        public List<CustPromotion> GetPromotionListV3(int SiteId, int CustId, int CurrencyId, int DeviceType, bool IsLogin, string langId, string defLang)
        {
            return _promotionService.GetPromotionListv3(SiteId, CustId, CurrencyId, DeviceType, IsLogin, langId, defLang);
        }
        public CustPromotion GetPromotionDetail(int SiteId, int CustId, int CurrencyId, bool IsLogin, string langId, string defLang, string BonusCode, int DeviceType)
        {
            return _promotionService.GetPromotionDetail(SiteId, CustId, CurrencyId, IsLogin, langId, defLang, BonusCode, DeviceType);
        }

        public List<CustPromotion> GetSpecialPromotionListV2(int SiteId, int CustId, int CurrencyId, string langId, string defLang, int deviceType)
        {
            return _promotionService.GetSpecialPromotionListV2(SiteId, CustId, CurrencyId, langId, defLang, deviceType);
        }
        public bool CheckSpecialCode(string specialCode)
        {
            return _promotionService.CheckSpecialCode(specialCode);
        }
        public bool CheckScratchCardCode(string specialCode)
        {
            return _promotionService.CheckScratchCardCode(specialCode);
        }
        public int SetScratchCardCode(int custId, string specialCode, int? deviceType)
        {
            return _promotionService.SetScratchCardCode(custId, specialCode, deviceType);
        }
        public bool SetSpecialCode(int custId, string specialCode, int? deviceType)
        {
            return _promotionService.SetSpecialCode(custId, specialCode, deviceType);
        }

        public List<CustPromotion> GetDepositPromotionList(int CustId, int DeviceType, string langId, string defLang)
        {
            return _promotionService.GetDepositPromotionList(CustId, DeviceType, langId, defLang);
        }
        public bool SetDepositPromotionQueue(string transId, string bonusCode, int custId, int siteId, string userName, int currencyId, int deviceType, decimal lockedAmount)
        {
            return _promotionService.SetDepositPromotionQueue(transId, bonusCode, custId, siteId, userName, currencyId, deviceType, lockedAmount, _tools.GetClientIP());
        }
        public PromotionBySpecialCode GetPromotionBySpecialCode(int siteId, string SpecialCode)
        {
            return _promotionService.GetPromotionBySpecialCode(siteId,SpecialCode);
        }
        public PromotionBySpecialScratchCard GetPromotionBySpecialScratchCard(int siteId, string SpecialCode)
        {
            return _promotionService.GetPromotionBySpecialScratchCard(siteId, SpecialCode);
        }
        public BonusinfoByBonusCode GetBonusinfoByBonusCode(int CustId, string BonusCode)
        {
            return _promotionService.GetBonusinfoByBonusCode(CustId, BonusCode);
        }
    }
}
