using Newtonsoft.Json;

namespace MemberSite.Service.Model;

public class VerifyThirdPartyTokenResponse
{
    [JsonProperty("TpUserName")]
    public string ThirdPartyUserName { get; set; }
}