using Newtonsoft.Json;

namespace MemberSite.Service.Model;

public class GetThirdPartyTokenRequest
{
    [JsonProperty("TpId")]
    public int ThirdPartyId { get; set; }
    public int CustId { get; set; }
}