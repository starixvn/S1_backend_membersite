using MemberSite.Service.DAL;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.BLL
{
    public class AppLinkBLL
    {
        private readonly AppLinkService appLinkService;
        public AppLinkBLL(AppLinkService appLinkService)
        {
            this.appLinkService = appLinkService;
        }
        public List<GetAppLinkRep> GetAppLink(int SiteId)
        {
            var result = new List<GetAppLinkRep>();
            var applink = appLinkService.GetAppLink(SiteId);

            applink.FindAll(item => !string.IsNullOrWhiteSpace(item.Domain)).ForEach(x =>
            {
                result.Add(new GetAppLinkRep
                {
                    AppType = x.AppType,
                    AppTypeName = Enum.GetName(typeof(AppLinkAppType), x.AppType),
                    DownloadLink = $"{Enum.GetName(typeof(AppLinkProtocolType), x.ProtocolType)}://{x.Domain}"
                });
            });

            return result;
        }
    }
}
