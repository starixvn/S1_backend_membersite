using MemberSite.Service.BLL;
using MemberSite.Service.Model;
using Microsoft.AspNetCore.Mvc;

namespace MemberSite.Controllers.Api
{
    [Route("api/[controller]/[action]/{LangId}")]
    [ApiController]
    public class MessageController : Controller
    {
        private readonly SelLangBLL selLangBLL;
        private readonly MessageBll messageBLL;
        private readonly IHostEnvironment environment;
        private readonly ServerInfoBll serverInfoBLL;

        public MessageController(MessageBll messageBLL, SelLangBLL selLangBLL, IHostEnvironment environment, ServerInfoBll serverInfoBLL)
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
            var LimitRow = 15;
            var serverInfo = serverInfoBLL.GetServerInfo();
            result.Data = messageBLL.GetAnnouncement(selLangBLL.GetLangId(), serverInfo.DefLang, LimitRow, !environment.IsProduction(), serverInfo.SiteId)
                .Select(x => new Announcement()
                {
                    Content = Newtonsoft.Json.JsonConvert.DeserializeObject<string>(x.Content ?? ""),
                    Subject = Newtonsoft.Json.JsonConvert.DeserializeObject<string>(x.Subject ?? ""),
                    Id = x.Id,
                    StartTime = x.StartTime
                });
            result.ErrorCode = 0;

            return result;
        }
    }
}