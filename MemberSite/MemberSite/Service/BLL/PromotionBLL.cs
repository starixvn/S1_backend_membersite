using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MemberSite.Service.BLL
{
    public class PromotionBll
    {
        private readonly PromotionService _promotionService;
        private readonly CustInfoBll _custInfoBll;
        private readonly CourierAdapter _courierAdapter;
        private readonly string _apiUrl;
        private readonly SelLangBLL _selLangBll;
        private readonly MessageBll _messageBll;
        private readonly CacheHelper _cacheHelper;
        private readonly string _bonusServiceUrl;
        private readonly Tools _tools;

        public PromotionBll(IServiceProvider provider)
        {
            _promotionService = provider.GetRequiredService<PromotionService>();
            _custInfoBll = provider.GetRequiredService<CustInfoBll>();
            _courierAdapter = provider.GetRequiredService<CourierAdapter>();
            _apiUrl = provider.GetRequiredService<IConfiguration>().GetValue<string>("TpService");
            _selLangBll = provider.GetRequiredService<SelLangBLL>();
            _messageBll = provider.GetRequiredService<MessageBll>();
            _cacheHelper = provider.GetRequiredService<CacheHelper>();
            _bonusServiceUrl = provider.GetRequiredService<IConfiguration>().GetValue<string>("BonusService");
            _tools = provider.GetRequiredService<Tools>();
        }

        public IEnumerable<Promotion> GetPromotionList(int siteId)
        {
            return _promotionService.GetPromotionList(siteId);
        }

        public IEnumerable<Promotion> GetPromotionListWithCache(int siteId)
        {
            var key = $"PromotionList_{siteId}";
            return _cacheHelper.TryGetOrCreate<IEnumerable<Promotion>>(key,
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(1)),
                i => GetPromotionList(siteId).OrderBy(x => x.Sort));
        }
        public IEnumerable<Promotion> GetPromotionListByCustId(int custId)
        {
            var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
            return _promotionService.GetPromotionListByCustId(custInfo.SiteId, custInfo.CustId, custInfo.CurrencyId)?.Where(x => GetCanJoin(custInfo.CustId, x.BonusCode) || x.BonusInfoStatus > 0 || custInfo.ActStatus == 1);
        }
        
        public IEnumerable<Promotion> GetPromotionListByCustIdWithCache(int custId)
        {
            var key = $"PromotionListByCustId_{custId}";
            return _cacheHelper.TryGetOrCreate(key,
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(10)),
                i => GetPromotionListByCustId(custId));
        }
        private class TpBonusTransfer
        {
            public int TpId { set; get; }
            public int CustId { set; get; }
            public decimal Bonus { set; get; }
            public decimal BonusAmount { set; get; }
            public decimal LockedAmount { set; get; }
            public decimal RolloverAmount { set; get; }
            public long ExpiryTime { set; get; }
            public Dictionary<string, string> inputs { set; get; }
        }

        public class PromotionReq
        {
            public string BonusCode { set; get; }
            public int CustId { set; get; }
            public string custIP { set; get; }
            public decimal? LockedAmount { set; get; }
            public int? DeviceType { set; get; }
        }
        public async Task<ApiResult> SetClaimPromotionV2Async(int custId, string bonusCode, decimal? dLockedAmount, int? deviceType)
        {
            var apiUrl = $"{_bonusServiceUrl}SetClaimPromotion";

            var req = new PromotionReq
            {
                BonusCode = bonusCode,
                CustId = custId,
                custIP = _tools.GetClientIP(),
                LockedAmount = dLockedAmount,
                DeviceType = deviceType
            };

            var result = await _courierAdapter.MyPostAsync<ApiResult, PromotionReq>(apiUrl, req);

            return result;

        }
        public bool GetCanJoin(int custId, string bonusCode)
        {
            var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
            return _promotionService.GetCanJoin(custId, custInfo.SiteId, bonusCode, custInfo.CurrencyId, out var _, out var _, out var _);
        }
        public bool GetCanJoin(int custId, string bonusCode, out int result, out string depositId, out decimal dpositAmount)
        {
            var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
            return _promotionService.GetCanJoin(custId, custInfo.SiteId, bonusCode, custInfo.CurrencyId, out result, out depositId, out dpositAmount);
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
