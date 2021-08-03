namespace MemberSite.Service.Model
{
    public class IpCountryModel
    {
        public string Code { get; set; }
        public bool Forbidden { get; set; }
        public string Message { get; set; }
        public string RemoteIpAddress { get; set; }
    }

    public class IpCountry
    {
        public string Code { get; set; }
        public bool Forbidden { get; set; }
        public string Message { get; set; }
    }
}