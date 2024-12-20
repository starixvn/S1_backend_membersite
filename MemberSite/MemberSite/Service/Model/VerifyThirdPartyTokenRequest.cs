using Newtonsoft.Json;

namespace MemberSite.Service.Model;

public class VerifyThirdPartyTokenRequest
{
    [JsonProperty("TpId")]
    public int ThirdPartyId { get; set; }
    public int SiteId { get; set; }
    public TokenModel Inputs { get; set; }
}

public class TokenModel
{
    public string Token { get; set; }
}