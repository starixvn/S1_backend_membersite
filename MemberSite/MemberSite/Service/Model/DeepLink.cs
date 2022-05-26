using MemberSite.Service.Model.enums;

namespace MemberSite.Service.Model
{
    public class DeepLink
    {
        public DeepLinkType Type { get; set; }
        public string Domain { get; set; }
        public DeepLinkProtocolType ProtocolType { get; set; }
    }
    public class GetDeepLinkRep
    {
        public int Type { get; set; }
        public string TypeName { get; set; }
        public string DeepLink { get; set; }

    }
}
