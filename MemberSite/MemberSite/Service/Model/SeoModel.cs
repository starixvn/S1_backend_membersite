using System.Text.Json.Serialization;

namespace MemberSite.Service.Model
{
    public class SeoModel
    {
        public int SiteId { get; set; }
        public string Title { get; set; }
        public string Meta { get; set; }
        public string DefaultLang { get; set; }
        public bool ActiveDomain { get; set; }
        public string Keyword { get; set; }

        [JsonIgnore]
        public string FollowUsData { get; set; }
        public List<FollowUs> FollowUs { get; set; }

        [JsonIgnore]
        public string PaymentMethodData { get; set; }
        public List<PaymentMethod> PaymentMethod { get; set; }
        public string Script { get; set; }
        public string XCMScript { get; set; }
    }

    public class FollowUs
    {
        public string Url { get; set; }
        public string Image { get; set; }
    }

    public class PaymentMethod
    {
        public string Image { get; set; }
    }
}