using Newtonsoft.Json;

namespace MemberSite.Service.Model;

public class SocialChannelRequest
{
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public int SiteId { get; set; }
    public int CurrencyId { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string LangId { get; set; }

    public int DisplayPage { get; set; }
}

public class SocialChannelResponse
{
    public int Id { get; set; }
    public string ChannelName { get; set; }
    public string Title { get; set; }
    public string Link { get; set; }
    public int LinkType { get; set; }
    public SocialChannelApp Scheme { get; set; }
    public string SocialChannelLogoLink { get; set; }
}

public class SocialChannel : SocialChannelApp
{
    public int Id { get; set; }
    public string ChannelName { get; set; }
    public string Title { get; set; }
    public string Link { get; set; }
    public int LinkType { get; set; }
}

public class SocialChannelApp
{
    public string Ios { get; set; }
    public string Android { get; set; }
}