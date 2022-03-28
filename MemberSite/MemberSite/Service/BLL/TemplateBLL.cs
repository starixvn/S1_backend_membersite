using MemberSite.Service.Library;
using Microsoft.AspNetCore.Mvc;

namespace MemberSite.Service.BLL
{
    public class TemplateBLL
    {
        private readonly Tools tools;
        private readonly ServerInfoBll serverInfoBLL;

        public TemplateBLL(Tools tools, ServerInfoBll serverInfoBLL)
        {
            this.tools = tools;
            this.serverInfoBLL = serverInfoBLL;
        }

        public string GetTemplatePath(IUrlHelper urlHelper)
        {
            var serverInfo = serverInfoBLL.GetServerInfo();
            if (serverInfo.SiteId == 3)
            {
                var deviceName = "Desktop";

                if (tools.IsMobile())
                {
                    deviceName = "Mobile";
                }

                if (urlHelper.ActionContext.RouteData.Values["action"].ToString().Equals("Register", StringComparison.OrdinalIgnoreCase))
                {
                    return $"/_Views/{serverInfo.SiteName}/{deviceName}/{urlHelper.ActionContext.RouteData.Values["Controller"]}/Index.cshtml";
                }
                else
                {
                    return $"/_Views/{serverInfo.SiteName}/{deviceName}/{urlHelper.ActionContext.RouteData.Values["Controller"]}/{urlHelper.ActionContext.RouteData.Values["action"]}.cshtml";
                }
            }
            else
            {
                return null;
            }
        }
    }
}
