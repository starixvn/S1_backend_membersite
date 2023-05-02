using System.Net;
using System.Net.Sockets;
using MemberSite.Service.BLL;
using Microsoft.AspNetCore.Mvc;

namespace MemberSite.Controllers
{
    public class ServerInfoController : Controller
    {
        private readonly IWebHostEnvironment environment;
        private readonly IConfiguration configuration;
        private readonly SelLangBLL selLangBLL;
        private readonly ServerInfoBll serverInfoBLL;
        public ServerInfoController(IWebHostEnvironment environment, IConfiguration configuration, SelLangBLL selLangBLL, ServerInfoBll serverInfoBLL)
        {
            this.environment = environment;
            this.configuration = configuration;
            this.selLangBLL = selLangBLL;
            this.serverInfoBLL = serverInfoBLL;
        }
        public IActionResult Index()
        {
            var ServerInfoData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Now", DateTime.Now.ToString() },
                { "EnvironmentName", environment.EnvironmentName },
                { "LangId", selLangBLL.GetLangId() }
            };
            var host = Dns.GetHostEntry(Dns.GetHostName());
            var i = 1;
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    ServerInfoData.Add($"IP {i++}.", ip.ToString());
                }
            }
            ServerInfoData.Add("Url.Host", Request.Host.Host);
            ServerInfoData.Add("Url.Port", Request.Host.Port.ToString());
            var serverInfo = serverInfoBLL.GetServerInfo();
            ServerInfoData.Add("SiteId", serverInfo.SiteId.ToString());
            ServerInfoData.Add("SiteName", serverInfo.SiteName);
            ServerInfoData.Add("LangList", serverInfo.LangList);
            ServerInfoData.Add("CurrencyList", serverInfo.CurrencyList);
            ServerInfoData.Add("DefCurrency", serverInfo.DefCurrency.ToString());
            ServerInfoData.Add("DefLang", serverInfo.DefLang);
            var userAgent = Request.Headers["User-Agent"];
            ServerInfoData.Add("User Agent", userAgent);

            //需要的欄位LIST
            var list = new List<string>
            {
                "X-Forwarded-For",
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
                var sKey = head.Key;
                if (list.Contains(sKey, StringComparer.OrdinalIgnoreCase) && !ServerInfoData.ContainsKey(sKey))
                {
                    ServerInfoData.Add(sKey, head.Value);
                }
            }

            return View(ServerInfoData);
        }
    }
}