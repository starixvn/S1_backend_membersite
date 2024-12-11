namespace MemberSite.Service.Model
{
    public class CustNotificationModel
    {
        public string TransId { get; set; }
        public string IsoCode { get; set; }
        public bool FirstDeposit { get; set; }
        public decimal Amount { get; set; }
    }
}
