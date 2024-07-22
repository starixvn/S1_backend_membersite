namespace MemberSite.Service.Model
{

    public class CreateAdminAdjustmentInfoLogInput
    {
        public int SiteId { get; set; }
        public int CustId { get; set; }
        public int CurrencyId { get; set; }
        public AdminAdjustmentLogMenuEnum MenuEnum { get; set; }
        public ActionType ActionType { get; set; }
        public string RefTransId { get; set; }
        public string BeforeDetailInfo { get; set; }
        public string AfterDetailInfo { get; set; }
        public string Operator { get; set; }
    }

    public enum AdminAdjustmentLogMenuEnum
    {
        IssueBonusManagement = 6,
    }
    
    public enum ActionType
    {
        Create = 1,
        Read = 2,
        Update = 3,
        Delete = 4
    }
}