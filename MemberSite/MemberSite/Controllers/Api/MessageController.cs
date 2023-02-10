using MemberSite.Service.BLL;
using MemberSite.Service.Model;
using Microsoft.AspNetCore.Mvc;
using static MemberSite.Extensions.MethodExtension;

namespace MemberSite.Controllers.Api
{
    [Route("api/[controller]/[action]/{LangId}")]
    [ApiController]
    public class MessageController : Controller
    {
        private readonly SelLangBLL _selLangBll;
        private readonly MessageBll _messageBll;
        private readonly IHostEnvironment _environment;
        private readonly ServerInfoBll _serverInfoBll;
        private readonly ILogger<MessageController> _logger;

        public MessageController(MessageBll messageBll, SelLangBLL selLangBll, IHostEnvironment environment, ServerInfoBll serverInfoBll,
            ILogger<MessageController> logger)
        {
            _messageBll = messageBll;
            _selLangBll = selLangBll;
            _environment = environment;
            _serverInfoBll = serverInfoBll;
            _logger = logger;
        }

        [HttpPost]
        public ApiResult<IEnumerable<Announcement>> GetAnnouncement()
        {
            var result = new ApiResult<IEnumerable<Announcement>>();
            try
            {
                const int limitRow = 15;
                var serverInfo = _serverInfoBll.GetServerInfo();
                result.Data = _messageBll.GetAnnouncement(_selLangBll.GetLangId(), serverInfo.DefLang, limitRow, !_environment.IsProduction(), serverInfo.SiteId)
                    .Select(x => new Announcement()
                    {
                        Content = Newtonsoft.Json.JsonConvert.DeserializeObject<string>(x.Content ?? ""),
                        Subject = Newtonsoft.Json.JsonConvert.DeserializeObject<string>(x.Subject ?? ""),
                        Id = x.Id,
                        StartTime = x.StartTime
                    });
            }
            catch (Exception e)
            {
                _logger.LogError(e, GetCurrentMethod());
            }
            
            result.ErrorCode = 0;
            return result;
        }
    }
}