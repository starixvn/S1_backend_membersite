using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MemberSite.Service.BLL;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace MemberSite.Controllers
{
    public class ServerInfoController : Controller
    {
        private readonly IWebHostEnvironment environment;
        private readonly IConfiguration configuration;
        private readonly SelLangBLL selLangBLL;

        public ServerInfoController(IWebHostEnvironment environment, IConfiguration configuration, SelLangBLL selLangBLL)
        {
            this.environment = environment;
            this.configuration = configuration;
            this.selLangBLL = selLangBLL;
        }
        public IActionResult Index()
        {
            Dictionary<string, string> ServerInfoData = new Dictionary<string, string>();
            ServerInfoData.Add("Now", DateTime.Now.ToString());
            ServerInfoData.Add("EnvironmentName", environment.EnvironmentName);
            ServerInfoData.Add("LangId", selLangBLL.GetLangId());
            ServerInfoData.Add("Url.Host", Request.Host.Host);
            ServerInfoData.Add("Url.Port", Request.Host.Port.ToString());
            //需要的欄位LIST
            var list = new List<string>
            {
                "LOCAL_ADDR",
                "server_port",
                "Http_Host",
                "HTTP_INCAP_CLIENT_IP",
                "HTTP_TRUE_CLIENT_IP",
                "HTTP_X_REAL_IP",
                "REMOTE_ADDR",
                "HTTP_X_FORWARDED_FOR",
                "HTTP_X_FORWARDED_PROTO",
                "SERVER_NAME"
            };

            foreach (var head in Request.Headers)
            {
                string sKey = head.Key;
                if (list.Contains(sKey) && !ServerInfoData.ContainsKey(sKey))
                {
                    ServerInfoData.Add(sKey, head.Value);
                }
            }

            return View(ServerInfoData);
        }
    }
}