using MemberSite.Service.DAL;
using MemberSite.Service.Model;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemberSite.Service.BLL
{
    public class PromotionBLL
    {
        private readonly PromotionService promotionService;
        private readonly IMemoryCache cache;
        private readonly CustInfoBLL custInfoBLL;

        public PromotionBLL(IMemoryCache cache, PromotionService promotionService, CustInfoBLL custInfoBLL)
        {
            this.promotionService = promotionService;
            this.cache = cache;
            this.custInfoBLL = custInfoBLL;
        }

        public IEnumerable<Promotion> GetPromotionList(int siteId)
        {
            return promotionService.GetPromotionList(siteId);
        }

        public IEnumerable<Promotion> GetPromotionListWithCache(int siteId)
        {
            string key = $"PromotionList_{siteId}";
            var promotionList = cache.Get<IEnumerable<Promotion>>(key);

            if (promotionList == null)
            {
                promotionList = GetPromotionList(siteId);

                if (promotionList != null)
                {
                    cache.Set(key, promotionList, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(1)));
                }
            }

            return promotionList.OrderBy(x=> x.Sort);
        }
        public IEnumerable<Promotion> GetPromotionListByCustId(int custId)
        {
            var custInfo = custInfoBLL.GetCustInfoWithCache(custId);
            return promotionService.GetPromotionListByCustId(custInfo.SiteId, custInfo.CustId, custInfo.CurrencyId)?.Where(x => GetCanJoin(custInfo.CustId, x.BonusCode) || x.BonusInfoStatus > 0 || custInfo.ActStatus == 1);
        }
        /*Radis*/
        public IEnumerable<Promotion> GetPromotionListByCustIdWithCache(int custId)
        {
            string key = $"PromotionListByCustId_{custId}";
            var promotionList = cache.Get<IEnumerable<Promotion>>(key);

            if (promotionList == null)
            {
                promotionList = GetPromotionListByCustId(custId);

                if (promotionList != null)
                {
                    cache.Set(key, promotionList, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(10)));
                }
            }

            return promotionList;
        }
        public int SetClaimPromotion(int custId, string bonusCode)
        {
            var custInfo = custInfoBLL.GetCustInfoWithCache(custId);
            return promotionService.SetClaimPromotion(custId, custInfo.UserName, custInfo.SiteId, bonusCode, custInfo.CurrencyId);
        }
        public bool GetCanJoin(int custId, string bonusCode)
        {
            var custInfo = custInfoBLL.GetCustInfoWithCache(custId);
            return promotionService.GetCanJoin(custId, custInfo.SiteId, bonusCode, custInfo.CurrencyId);
        }

        public Promotion GetPromotion(int siteId, string bonusCode, int currencyId)
        {
            string key = $"Promotion_{bonusCode}_{currencyId}_{siteId}";
            var promotion = cache.Get<Promotion>(key);

            if (promotion == null)
            {
                promotion = promotionService.GetPromotion(siteId, bonusCode, currencyId);

                if (promotion != null)
                {
                    cache.Set(key, promotion, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(1)));
                }
            }

            return promotion;
        }
    }
}
