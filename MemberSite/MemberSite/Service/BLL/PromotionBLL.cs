using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static MemberSite.Controllers.Api.TpController;

namespace MemberSite.Service.BLL
{
    public class PromotionBLL
    {
        private readonly PromotionService promotionService;
        private readonly IMemoryCache cache;
        private readonly CustInfoBLL custInfoBLL;
        private readonly CourierAdapter courierAdapter;
        private readonly string apiUrl;
        private readonly int bonusType;
        private readonly SelLangBLL selLangBLL;

        public PromotionBLL(IConfiguration configuration, IMemoryCache cache, PromotionService promotionService, CustInfoBLL custInfoBLL, CourierAdapter courierAdapter, SelLangBLL selLangBLL)
        {
            this.promotionService = promotionService;
            this.cache = cache;
            this.custInfoBLL = custInfoBLL;
            this.courierAdapter = courierAdapter;
            apiUrl = configuration.GetValue<string>("TpService");
            bonusType = configuration.GetValue<int>("BonusType");
            this.selLangBLL = selLangBLL;
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

            return promotionList.OrderBy(x => x.Sort);
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
        public async Task<ApiResult> SetClaimPromotionAsync(int custId, string bonusCode, decimal? dLockedAmount)
        {
            var apiResult1 = new ApiResult();
            decimal lockedAmount = dLockedAmount ?? 0;
            var custInfo = custInfoBLL.GetCustInfoWithCache(custId);
            var result = promotionService.SetClaimPromotion(custId, custInfo.UserName, custInfo.SiteId, bonusCode, custInfo.CurrencyId, out decimal bonusAmount, ref lockedAmount, out decimal rollOver, out long expiryTime, out Promotion promotion);

            if (result > 0)
            {
                if (promotion?.TpId > 0)
                {
                    var myUrl = $"{apiUrl}BonusTransfer";
                    ApiResult<TransferResult> apiResult = new ApiResult<TransferResult>();
                    var tpBonusTransfer = new TpBonusTransfer
                    {
                        TpId = promotion.TpId,
                        CustId = custId,
                        Bonus = bonusAmount + lockedAmount,
                        BonusAmount = bonusAmount,
                        LockedAmount = lockedAmount,
                        RolloverAmount = rollOver,
                        ExpiryTime = expiryTime,
                        inputs = new Dictionary<string, string>()
                    };

                    var data = await courierAdapter.MyPostAsync<TransferResult, TpBonusTransfer>(myUrl, tpBonusTransfer);
                    apiResult.Data = data.Data;
                    apiResult.ErrorCode = data.ErrorCode;
                    apiResult.RawErrorCode = data.RawErrorCode;
                    apiResult.RawMessage = data.RawMessage;

                    if (apiResult.ErrorCode != 0)
                    {
                        apiResult1.ErrorCode = apiResult.ErrorCode;
                        apiResult1.RawErrorCode = apiResult.RawErrorCode;
                        apiResult1.RawMessage = apiResult.RawMessage;
                    }
                    else
                    {
                        apiResult1.ErrorCode = 0;
                    }
                }
                else
                {
                    apiResult1.ErrorCode = 0;
                }
            }
            else if (result == -2)
            {
                apiResult1.ErrorCode = -2;
                var dRequiredAmount = lockedAmount.ToString("#,##0");
                if (custInfoBLL.IsConvertRequired(custInfo.CurrencyId))
                {
                    dRequiredAmount = $"{lockedAmount / 1000:#,##0} ('000)";
                }

                apiResult1.Message = string.Format(selLangBLL.GetMsg("msg_LockedAmountInsufficient"), dRequiredAmount);
            }
            else if (result == -3)
            {
                apiResult1.ErrorCode = -3;
                apiResult1.Message = selLangBLL.GetMsg("msg_Ineligible");

                if ((custInfo.SiteId == 2 || custInfo.SiteId == 3) && promotion?.PromotionType == PromotionType.Freebet)
                {
                    apiResult1.Message = selLangBLL.GetMsg("msg_Ineligible_FreeBet");
                }
            }
            else if (result == -4)
            {
                apiResult1.ErrorCode = -4;
                var dRequiredAmount = promotion.RequiredAmount.ToString("#,##0");
                if (custInfoBLL.IsConvertRequired(custInfo.CurrencyId))
                {
                    dRequiredAmount = $"{promotion.RequiredAmount / 1000:#,##0} ('000)";
                }

                apiResult1.Message = string.Format(selLangBLL.GetMsg("msg_RequiredAmountInsufficient"), dRequiredAmount);
            }
            else if (result == -5)
            {
                apiResult1.ErrorCode = result;
                apiResult1.Message = $"{selLangBLL.GetMsg("msg_OTPHint1")}\r\n{selLangBLL.GetMsg("msg_OTPHint2")}";
            }
            else
            {
                apiResult1.Message = selLangBLL.GetMsg("msg_Ineligible");

                if ((custInfo.SiteId == 2 || custInfo.SiteId == 3))
                {
                    promotion = GetPromotionListWithCache(custInfo.SiteId).Where(x => x.BonusCode.Equals(bonusCode, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                    if (promotion?.PromotionType == PromotionType.Freebet)
                    {
                        apiResult1.Message = selLangBLL.GetMsg("msg_Ineligible_FreeBet");
                    }
                }
            }

            return apiResult1;
        }
        public bool GetCanJoin(int custId, string bonusCode)
        {
            var custInfo = custInfoBLL.GetCustInfoWithCache(custId);
            return promotionService.GetCanJoin(custId, custInfo.SiteId, bonusCode, custInfo.CurrencyId, out int _, out string _, out decimal _);
        }
        public bool GetCanJoin(int custId, string bonusCode, out int result, out string depositId, out decimal dpositAmount)
        {
            var custInfo = custInfoBLL.GetCustInfoWithCache(custId);
            return promotionService.GetCanJoin(custId, custInfo.SiteId, bonusCode, custInfo.CurrencyId, out result, out depositId, out dpositAmount);
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

        public List<CustPromotion> GetPromotionListV2(int SiteId, int CustId, int CurrencyId, bool IsLogin, string langId, string defLang)
        {
            return promotionService.GetPromotionListv2(SiteId, CustId, CurrencyId, IsLogin, langId, defLang);
        }
        public CustPromotion GetPromotionDetail(int SiteId, int CustId, int CurrencyId, bool IsLogin, string langId, string defLang, string BonusCode)
        {
            return promotionService.GetPromotionDetail(SiteId, CustId, CurrencyId, IsLogin, langId, defLang, BonusCode);
        }
        public List<CustPromotion> GetSpecialPromotionList(int SiteId, int CustId, int CurrencyId, string langId, string defLang)
        {
            return promotionService.GetSpecialPromotionList(SiteId, CustId, CurrencyId, langId, defLang);
        }
        public bool SetSpecialCode(int custId, string specialCode)
        {
            var custInfo = custInfoBLL.GetCustInfoWithCache(custId);
            return promotionService.SetSpecialCode(custId, specialCode);
        }
        public void SetSpecialCodeJoined(int custId, string specialCode, string bonusCode)
        {
            promotionService.SetSpecialCodeJoined( custId, specialCode, bonusCode);
        }
    }
}
