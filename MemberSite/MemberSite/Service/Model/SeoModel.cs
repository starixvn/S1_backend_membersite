namespace MemberSite.Service.Model
{
    public class SeoModel
    {
        public int SiteId { get; set; }
        public string Title { get; set; }
        public string Meta { get; set; }
        public string DefaultLang { get; set; }
        public bool ActiveDomain { get; set; }
    }
}