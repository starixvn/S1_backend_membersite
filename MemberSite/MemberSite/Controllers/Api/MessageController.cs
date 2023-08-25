using System.Text;

using MemberSite.Service.BLL;
using MemberSite.Service.BLL.Interface;
using MemberSite.Service.Library;
using MemberSite.Service.Model;

using Microsoft.AspNetCore.Mvc;

using Newtonsoft.Json;

using static MemberSite.Extensions.MethodExtension;

namespace MemberSite.Controllers.Api
{
    [Route("api/[controller]/[action]/{LangId}")]
    [ApiController]
    public class MessageController : Controller
    {
        private readonly SelLangBLL _selLangBll;
        private readonly ServerInfoBll _serverInfoBll;
        private readonly AuthorizeBLL _authorizeBll;
        private readonly CustInfoBll _customerInfoBll;
        private readonly IAnnouncementBLL _announcementBLL;

        private readonly ILogger<MessageController> _logger;

        public MessageController(SelLangBLL selLangBll, ServerInfoBll serverInfoBll, AuthorizeBLL authorizeBll, CustInfoBll customerInfoBll, IAnnouncementBLL announcementBLL, ILogger<MessageController> logger)
        {
            _selLangBll = selLangBll;
            _serverInfoBll = serverInfoBll;
            _authorizeBll = authorizeBll;
            _customerInfoBll = customerInfoBll;
            _announcementBLL = announcementBLL;
            _logger = logger;
        }

        public class GetAnnouncementRequest
        {
            public int? CurrencyId { get; set; }
        }
        [HttpPost]
        public async Task<ApiResult<IEnumerable<Announcement>>> GetAnnouncement()
        {
            var result = new ApiResult<IEnumerable<Announcement>>();
            try
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                var content = await reader.ReadToEndAsync();
                var request = JsonConvert.DeserializeObject<GetAnnouncementRequest>(content);

                int? currency = request?.CurrencyId;

                var authData = _authorizeBll.GetAuthData();
                if (authData is { CustId: > 0 })
                {
                    var customerInfo = _customerInfoBll.GetCustInfoWithCache(authData.CustId);
                    currency = customerInfo.CurrencyId;
                }
                var serverInfo = _serverInfoBll.GetServerInfo();
                var language = LanguageObject.Create(currency, _selLangBll.GetLangId(), serverInfo.DefLang);

                result.Data = await _announcementBLL.GetAnnouncements(serverInfo.SiteId, language);
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