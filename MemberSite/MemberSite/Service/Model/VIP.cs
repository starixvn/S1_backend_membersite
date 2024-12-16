namespace MemberSite.Service.Model
{
    public class VIP
    {
    }
    public class VipSystem
    {
        public int SiteId { get; set; }
        public int CurrencyId { get; set; }
        public int Level { get; set; }
        public decimal UpDeposit { get; set; }
        public decimal UpTurnover { get; set; }
        public decimal UpBonus { get; set; }
        public decimal? DownDeposit { get; set; }
        public decimal? DownTurnover { get; set; }
    }
}
