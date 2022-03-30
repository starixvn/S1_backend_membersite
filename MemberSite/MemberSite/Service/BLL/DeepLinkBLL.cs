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
            List<GetDeepLinkRep> result = new List<GetDeepLinkRep>();
            var applink = deepLinkService.GetDeepLink(SiteId, DeepLinkId);

            applink.FindAll(item => !string.IsNullOrWhiteSpace(item.Domain)).ForEach(x =>
            {
                result.Add(new GetDeepLinkRep
                {
                    Type = x.Type,
                    TypeName = Enum.GetName(typeof(DeepLinkType), x.Type),
                    DownloadLink = $"{Enum.GetName(typeof(DeepLinkProtocolType), x.ProtocolType)}://{x.Domain}"
                });
            });

            return result;
        }
    }
}
