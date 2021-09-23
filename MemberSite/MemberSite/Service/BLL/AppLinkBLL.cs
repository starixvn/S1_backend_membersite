using MemberSite.Service.DAL;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            List<GetAppLinkRep> result = new List<GetAppLinkRep>();
            var applink = appLinkService.GetAppLink(SiteId);

            applink.ForEach(x =>
            {
                if (!string.IsNullOrWhiteSpace(x.Domain))
                {
                    result.Add(new GetAppLinkRep
                    {
                        AppType = x.AppType,
                        AppTypeName = Enum.GetName(typeof(AppLinkAppType), x.AppType),
                        DownloadLink = $"{Enum.GetName(typeof(AppLinkProtocolType), x.ProtocolType)}://{x.Domain}"
                    });
                }               
            });

            return result;
        }
    }
}
