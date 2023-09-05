using MemberSite.Service.Model.enums;

namespace MemberSite.Service.Model
{
    public class CustPromotion
    {
        public string BonusCode { get; set; }
        public int PromotionType { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Banner { get; set; }
        public long PeriodFrom { get; set; }
        public long PeriodTo { get; set; }
        public long DisplayFrom { get; set; }
        public long DisplayTo { get; set; }
        public int WinOver { get; set; }
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
        public IEnumerable<TierAmount> TierAmountList { get; set; }
        public IEnumerable<CanJoinTierAmount> CanJoinTierAmountList { get; set; }
    }

    public class CashBackDetails
    {
        public decimal TotalBonus { get; set; }
        public decimal MaxBonus { get; set; }
        public decimal TotalLossAmount { get; set; }
        public decimal MinLossAmount { get; set; }
        public decimal OriginalTotalBonus { get; set; }
        public string Percentage { get; set; }
        public IEnumerable<CashBackDetail> CashBackDetail { get; set; }
    }

    public class CashBackDetail
    {
        public string TpName { get; set; }
        public decimal LossAmount { get; set; }
        public decimal LossAmountWithBonus { get; set; }
    }

    public class Ref
    {
        public string DepositId { get; set; }
        public decimal Amount { get; set; }
    }

    public class SpecialCodeResponse
    {
        public string BonusCode { get; set; }
    }

    public class SpecialCodeRequest
    {
        public int CustId { set; get; }
        public string SpecialCode { set; get; }
        public int DeviceType { set; get; }
        public int SiteId { set; get; }
    }

    public class DepositPromotionQueue
    {
        public string TransId { set; get; }
        public string BonusCode { set; get; }
        public decimal LockedAmount { set; get; }
        public int DeviceType { set; get; }
        public int CustId { set; get; }
        public int SiteId { set; get; }
        public string UserName { set; get; }
        public int CurrencyId { set; get; }
        public string ClientIp { set; get; }
    }

    public class TierAmount
    {
        public decimal Bonus { get; set; }
        public decimal MaxBonus { get; set; }
        public decimal RequiredAmount { get; set; }
        public int RollOver { get; set; }
        public int WinOver { get; set; }
    }

    public class CanJoinTierAmount
    {
        public decimal RequiredAmount { get; set; }
        public decimal Bonus { get; set; }
        public decimal BonusAmount { get; set; }
        public decimal LockedAmount { get; set; }
    }

    public class PopUpPromotion
    {
        public string BonusCode { get; set; }
    }
}
