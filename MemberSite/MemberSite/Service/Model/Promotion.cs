using MemberSite.Service.Model.enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemberSite.Service.Model
{
    //public interface IPromotion
    //{
    //    public string BonusCode { get; set; }
    //    public string Title { get; set; }
    //    public string Description { get; set; }
    //    public PromotionType PromotionType { get; set; }
    //    public int CategoryId { get; set; }
    //    public string CategoryName { get; set; }
    //    public int CurrencyId { get; set; }
    //}
    public class Promotion
    {
        public string BonusCode { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public PromotionType PromotionType { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public int ExpiryDays { get; set; }
        public int PeriodTo { get; set; }
        public int CurrencyId { get; set; }
        public int RollOver { get; set; }
        public decimal Bonus { get; set; }
        public BonusType BonusType { get; set; }
        public decimal MaxBonus { get; set; }
        public decimal LockedAmount { get; set; }
        public decimal BonusAmount { get; set; }
        public DateTime ExpiryTime { get; set; }
        public int BonusInfoStatus { get; set; }
        public int Sort { get; set; }
        public CustRollOver custRollOver { get; set; }
        public string Details { get; set; }
    }

    public class PromotionBanner
    {
        public string imgURL { get; set; }
        public string actionLink { get; set; }
    }

    //public class ClaimPromotion : IPromotion
    //{
    //    public string BonusCode { get; set; }
    //    public string Title { get; set; }
    //    public string Description { get; set; }
    //    public PromotionType PromotionType { get; set; }
    //    public int CategoryId { get; set; }
    //    public string CategoryName { get; set; }
    //    public int CurrencyId { get; set; }
    //}
}
