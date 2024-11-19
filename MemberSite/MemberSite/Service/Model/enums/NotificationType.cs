namespace MemberSite.Service.Model.enums
{
    public enum CustNotificationType
    {
        DepositSuccess = 1,
        WithdrawalSuccess = 2,
        DepositFail = 3,
        WithdrawalFail = 4,
    }

    public class CustNotificationModel
    {
        public string TransId { get; set; }
        public string IsoCode { get; set; }
        public bool FirstDeposit { get; set; }
        public decimal Amount { get; set; }
    }
}
