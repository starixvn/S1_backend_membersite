namespace MemberSite.Service.Model
{
    public class SiteConfigObject
    {
        public int SiteId { get; set; }
        public string SiteName { get; set; }
        public Dictionary<string, string> Config { get; set; }

        public SiteConfigObject(ServerInfo serverinfo, Dictionary<string, string> config)
        {
            SiteId = serverinfo.SiteId;
            SiteName = serverinfo.SiteName;
            Config = config;
        }
    }
}
