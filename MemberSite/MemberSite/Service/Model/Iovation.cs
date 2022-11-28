namespace MemberSite.Service.Model
{
    public class IovationCheckResult
    {
        public string Result { get; set; }
        public string Reason { get; set; }
        public string DeviceId { get; set; }
        public string DeviceType { get; set; }
        public int Score { get; set; }
    }
}