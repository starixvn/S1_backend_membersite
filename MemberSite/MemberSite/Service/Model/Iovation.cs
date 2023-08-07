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
    
    public class CustIovation
    {
        public int CustId { get; set; }
        public string DeviceId { get; set; }
        public string Iovation { get; set; }
        public string DeviceType { get; set; }
        public int Frequency { get; set; }
        public int AssociateCount { get; set; }
        public int Score { get; set; }
    }
}