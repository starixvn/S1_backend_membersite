namespace MemberSite.Service.Model
{
    public class SendOTPSMSResult
    {
        public int Status { get; set; }
        public long StartUnixTime { get; set; }
    }

    public class Domainlist
    {
        public string DomainName { get; set; }
    }

}
