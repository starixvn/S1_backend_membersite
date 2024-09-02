using MemberSite.Service.Model.enums;

namespace MemberSite.Service.Model
{
    public class BonusWallet
    {
        public int BonusId { get; set; }
        public decimal Bonus { get; set; }
        public decimal Rollover { get; set; }
        public decimal Turnover { get; set; }
        public int FullFillType { get; set; }
        public int PromoCategoryId { get; set; }
        public int TpId { get; set; }
        public int Status { get; set; }
        public DateTime? ExpiryTime { get; set; }
    }
    
    public class BonusPayoutRequest
    {
        public int CustId { get; set; }
        public decimal Amount { get; set; }
        public string Memo { set; get; }
        public string BonusCode { get; set; }
        public string Operator { get; set; }
        public int RollOver { get; set; }
        public int PromotionType { get; set; }
        public string DepositId { get; set; }
    }

    public class BaseBonusPayoutRequest
    {
        public string BonusCode { set; get; }
        public int CustId { set; get; }
        public string custIP { set; get; }
        public decimal? LockedAmount { set; get; }
        public int DeviceType { set; get; }
    }
}
