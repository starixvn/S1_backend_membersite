namespace MemberSite.Service.Model
{
    public class DeepLink
    {
        public int Type { get; set; }
        public string Domain { get; set; }
        public int ProtocolType { get; set; }
    }
    public class GetDeepLinkRep
    {
        public int Type { get; set; }
        public string TypeName { get; set; }
        public string DownloadLink { get; set; }

    }
}
