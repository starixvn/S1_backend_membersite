namespace MemberSite.Service.Model
{
    public class MailConfig
    {
        public string MailSender { get; set; }
        public string MailHost { get; set; }
        public string MailID { get; set; }
        public string MailPwd { get; set; }
        public int Port { get; set; }
    }
    public class JwtConfig
    {
        public bool IsJwt { get; set; }
        public string Issuer { get; set; }
        public string SignKey { get; set; }
    }
    public class SiteConfig
    {
        public string ImageURL { get; set; }
    }

    public class LoginDeviceConfig
    {
        public string Url { get; set; }
    }

    public class LineLoginChannelConfig
    {
        public string ChannelId { get; set; }

        public string ChannelSecret { get; set; }

        public int ExpiredSeconds { get; set; }
    }
    public class LineLoginConfig
    {
        public string AccessTokenApi { get; set; }

        public string ProfileApi { get; set; }
    }
    public class ShieldConfig
    {
        public string SiteId { get; set; }

        public string SecretKey { get; set; }
        public string ShieldService { get; set; }
    }
    
    public class AllowIpList
    {
        public List<string> IpRange { get; set; }
    }
}
