using Newtonsoft.Json;

namespace MemberSite.Service.Model;

public class GetTokenRequest
{
    public string UserName { get; set; }
    public int SiteId { get; set; }
}