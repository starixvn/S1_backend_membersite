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
        public LockedType LockedType { get; set; }
        public decimal RequiredAmount { get; set; }
        public decimal MaxBonus { get; set; }
        public decimal LockedAmount { get; set; }
        public decimal BonusAmount { get; set; }
        public DateTime ExpiryTime { get; set; }
        public int BonusInfoStatus { get; set; }
        public int Sort { get; set; }
        public CustRollOver custRollOver { get; set; }
        public string Details { get; set; }
        public int TpId { get; set; }
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

    public class CustPromotion
    {
        public string BonusCode { get; set; }
        public int PromotionType { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Banner { get; set; }
        public long PeriodFrom { get; set; }
        public long PeriodTo { get; set; }
        public int RollOver { get; set; }
        public string Details { get; set; }
        public decimal Bonus { get; set; }
        public LockedType LockedType { get; set; }
        public decimal RequiredAmount { get; set; }
        public decimal MaxBonus { get; set; }
        public int BonusType { get; set; }
        public int Sort { get; set; }
        public int CategoryId { get; set; }
        public int TpId { get; set; }
        public decimal CustRollOver { get; set; }
        public decimal CustTurnOver { get; set; }
        public int Status { get; set; }
        public DateTime? CustExpiryTime { get; set; }
        public bool canJoin { get; set; }
        public bool isJoin { get; set; }
        public int Result { get; set; }
        public string DepositId { get; set; }
        public decimal DepositAmount { get; set; }
        public decimal MaxAmount { get; set; }
        public IEnumerable<Ref> RefList { get; set; }
    }

    public class Ref
    {
        public string DepositId { get; set; }
        public decimal Amount { get; set; }
    }
}
