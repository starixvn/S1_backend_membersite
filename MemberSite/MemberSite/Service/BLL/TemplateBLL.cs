using MemberSite.Service.Library;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemberSite.Service.BLL
{
    public class TemplateBLL
    {
        private readonly Tools tools;
        private readonly ServerInfoBLL serverInfoBLL;

        public TemplateBLL(Tools tools, ServerInfoBLL serverInfoBLL)
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
