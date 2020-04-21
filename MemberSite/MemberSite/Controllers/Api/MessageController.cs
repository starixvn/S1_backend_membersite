using System;
using System.Collections.Generic;
using MemberSite.Service.BLL;
using MemberSite.Service.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace MemberSite.Controllers.Api
{
    [Route("api/[controller]/[action]/{LangId}")]
    [ApiController]
    public class MessageController : Controller
    {
        private readonly SelLangBLL selLangBLL;
        private readonly MessageBLL messageBLL;
        private readonly IHostEnvironment environment;
        private readonly ServerInfoBLL serverInfoBLL;

        public MessageController(MessageBLL messageBLL, SelLangBLL selLangBLL, IHostEnvironment environment, ServerInfoBLL serverInfoBLL)
        {
            this.messageBLL = messageBLL;
            this.selLangBLL = selLangBLL;
            this.environment = environment;
            this.serverInfoBLL = serverInfoBLL;
        }

        [HttpPost]
        public ApiResult<IEnumerable<Announcement>> GetAnnouncement()
        {
            var result = new ApiResult<IEnumerable<Announcement>>();
            int LimitRow = 15;
            var serverInfo = serverInfoBLL.GetServerInfo();
            result.Data = messageBLL.GetAnnouncement(selLangBLL.GetLangId(), serverInfo.DefLang, LimitRow, !environment.IsProduction(), serverInfo.SiteId);
            result.ErrorCode = 0;

            return result;
        }
    }
}