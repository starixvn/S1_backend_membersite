using Newtonsoft.Json;

namespace MemberSite.Service.Model
{
    public class RecaptchaResponse
    {
        public bool success { get; set; }
        [JsonProperty("error-codes")]
        public List<string> errorCodes { get; set; }
    }

    public class RecaptchaSiteInfo
    {
        public string Country { get; set; }
        public string SiteKey { get; set; }
    }

    public class RecaptchaSetting
    {
        public string ApiUrl { get; set; }
        public Dictionary<string, RecaptchaGroup> Groups { get; set; }
    }

    public class RecaptchaGroup
    {
        public string SiteKey { get; set; }
        public string Secret { get; set; }
    }
}