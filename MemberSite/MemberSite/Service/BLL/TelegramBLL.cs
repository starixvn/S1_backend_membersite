using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace MemberSite.Service.BLL
{
    public class TelegramBLL
    {
        private readonly ILogger<TelegramBLL> _logger;
        private readonly TelegramService _telegramService;
        private readonly ICacheHelper _redisHelper;
        private readonly SelLangBLL _selLangBll;
        public TelegramBLL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<TelegramBLL>>();
            _telegramService = provider.GetRequiredService<TelegramService>();
            _redisHelper = provider.GetRequiredService<ICacheHelper>();
            _selLangBll = provider.GetRequiredService<SelLangBLL>();
        }

        public Telegram GetTelegramUrl(string url, string vendorId, string vendorMemberId)
        {
            var client = new HttpClient();
            var httpRequestMessage = new HttpRequestMessage();
            var langId = _selLangBll.GetLangId();

            var parameters = new Dictionary<string, string>()
            {
                { "vendor_id", vendorId },
                { "vendor_member_id", vendorMemberId },
                { "user_language",GetBotLang(langId)},
            };
            //httpRequestMessage.Content = new StringContent($"vendor_id={vendorId}&vendor_member_id={vendorMemberId}&user_language=en");
            httpRequestMessage.Content = new FormUrlEncodedContent(parameters);
            httpRequestMessage.Method = HttpMethod.Post;
            httpRequestMessage.RequestUri = new Uri($"{url}/GetTelegramUrl");
            httpRequestMessage.Content.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded");

            var response = client.SendAsync(httpRequestMessage).Result;
            var sb = response.Content.ReadAsStringAsync().Result;

            _logger.LogInformation($"GetTelegramUrl Response StatusCode = {(int)response.StatusCode}-{response.StatusCode}.\n"
                    + $"Content:{sb}");

            var telegramData = JsonConvert.DeserializeObject<Telegram>(sb);

            return telegramData;
        }
        public IEnumerable<ConfigObject> GetTpSiteConfig()
        {
            return _redisHelper.TryGetOrCreate($"SiteConfig:TP", TimeSpan.FromMinutes(10),
                () => _telegramService.GetTpSiteConfig(),
                RedisDataBase.Shared);
        }

        private string GetBotLang(string lang)
        {
            return lang switch
            {
                "id" => "id",
                "vn" => "vn",
                "cs" => "cs",
                _ => "en"
            };
        }
    }
}
