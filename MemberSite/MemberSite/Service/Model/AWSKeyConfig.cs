namespace MemberSite.Service.Model
{
    public class AWSKeyConfig
    {
        public string AccessKey { get; set; }
        public string SecretKey { get; set; }
        public string InternalServiceUrl { get; set; }
    }

    public class AWSPublicServiceUrl
    {
        public string InternalServiceUrl { get; set; }
        public string ExternalServiceUrl { get; set; }
    }
}
