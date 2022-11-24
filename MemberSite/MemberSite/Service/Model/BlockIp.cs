namespace MemberSite.Service.Model
{
    public class BlockIP
    {
        public string IP { get; set; }
    }

    public class BlockIpRequest
    {
        public string IP { get; set; }
        public string Memo { get; set; }
        public string Operator { get; set; }
    }
}