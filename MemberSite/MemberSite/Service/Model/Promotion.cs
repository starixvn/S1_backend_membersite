using MemberSite.Service.Model.enums;

namespace MemberSite.Service.Model
{
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
        public bool IsSpecial { get; set; }
        public string SpecialCode { get; set; }
        public decimal MinPayout { get; set; }
    }

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
        public int WalletType { get; set; }
        public int TpId { get; set; }
        public decimal CustRollOver { get; set; }
        public int FullFillType { get; set; }
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
        public string PromotionLink { get; set; }
        public int? DeviceType { set; get; }
        public int? PromotionAction { set; get; }
        public CashBackDetails CashBackDetails { get; set; }
        public bool BeenUsed { get; set; }
    }

    public class CashBackDetails
    {
        public decimal TotalBonus { get; set; }
        public decimal MaxBonus { get; set; }
        public IEnumerable<CashBackDetail> CashBackDetail { get; set; }
    }

    public class CashBackDetail
    {
        public int CategoryId { get; set; }
        public int TpId { get; set; }
        public string TpName { get; set; }
        public string PeriodFrom { get; set; }
        public string PeriodTo { get; set; }
        public decimal LossAmount { get; set; }
        public decimal Percentage { private get; set; }
        public string PercentageName => $"{Percentage:G29}%";
        public decimal BonusAmount { get; set; }
    }

    public class Ref
    {
        public string DepositId { get; set; }
        public decimal Amount { get; set; }
    }
    public class DepositBonus
    {
        public string TransId { set; get; }
        public string BonusCode { set; get; }
        public int CustId { set; get; }
        public int SiteId { set; get; }
        public string UserName { set; get; }
        public int CurrencyId { set; get; }
        public decimal LockedAmount { set; get; }
        public int DeviceType { set; get; }
        public decimal Amount { set; get; }
        public int DepositStatus { set; get; }
    }
    public enum DepositBonusQueueStatus : int
    {
        Undo = 1,
        Finish = 2,
        Expiry = 3,
        Cancel_other_Bonus = 4,//Rewarded other Bonus
        Cancel_not_eligible = 5,
        Cancel_Rejected = 6
    }
    public class PromotionBySpecialCode
    {
        public string BonusCode { set; get; }
        public int PromotionType { get; set; }
    }
    public class PromotionBySpecialScratchCard : PromotionBySpecialCode
    {
        public int CustId { get; set; }
    }
    public class BonusinfoByBonusCode
    {
        public int Status { get; set; }
    }
    public class CheckDepositSpecialCodeRsp
    {
        public string BonusCode { get; set; }
    }
    public class CustCondition
    {
        public byte CanJoin { set; get; }
        public decimal Amount { set; get; }
        public string RefId { set; get; }
        public int Result { set; get; }
        public string RefList { set; get; }
        public IEnumerable<Ref> reference { set; get; }
    }
}
