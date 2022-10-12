using Newtonsoft.Json;

namespace MemberSite.Service.Model;

public class RecaptchaResponse
{
    public bool success { get; set; }
    [JsonProperty("error-codes")]
    public List<string> errorCodes { get; set; }
}