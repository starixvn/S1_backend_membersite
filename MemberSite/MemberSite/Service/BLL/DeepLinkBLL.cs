using MemberSite.Service.DAL;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.BLL
{
    public class DeepLinkBLL
    {
        private readonly DeepLinkService deepLinkService;
        public DeepLinkBLL(DeepLinkService appLinkService)
        {
            this.deepLinkService = appLinkService;
        }
        public List<GetDeepLinkRep> GetDeepLink(int SiteId, int DeepLinkId)
        {
            var result = new List<GetDeepLinkRep>();
            var deeplink = deepLinkService.GetDeepLink(SiteId, DeepLinkId);

            deeplink.FindAll(item => !string.IsNullOrWhiteSpace(item.Domain)).ForEach(x =>
            {
                result.Add(new GetDeepLinkRep
                {
                    Type = (int)x.Type,
                    TypeName = Enum.GetName(typeof(DeepLinkType), x.Type),
                    DeepLink = $"{GetProtocolTypeName(x.Type, x.ProtocolType)}://{x.Domain}"
                });
            });

            return result;
        }

        private string GetProtocolTypeName(DeepLinkType type, DeepLinkProtocolType protocolType)
        {
            return Enum.GetName(typeof(DeepLinkProtocolType), protocolType);
        }
    }
}
