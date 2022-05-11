using Newtonsoft.Json;

namespace MemberSite.Service.Model;

public class SocialChannelRequest
{
    public int CurrencyId { get; set; }
    
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string LangId { get; set; }
    
    public int DisplayPage { get; set; }
}

public class SocialChannelResponse
{
    public string ChannelName { get; set; }
    public string Title { get; set; }
    public string Link { get; set; }
    public int LinkType { get; set; }
}