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
            if (/*tools.IsMobile() && */serverInfo.SiteId == 3)
            {
                return $"/_Views/{serverInfo.SiteName}/Mobile/{urlHelper.ActionContext.RouteData.Values["Controller"]}/{urlHelper.ActionContext.RouteData.Values["action"]}.cshtml";
            }
            else
            {
                return null;
            }
        }
    }
}
