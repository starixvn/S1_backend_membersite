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

        public string MaintainURL { get; set; }
    }

    public class DatabaseConnection
    {
        public string ConnectionString { get; set; }
        public string ConnectionStringReport { get; set; }
        public string ConnectionStringWager { get; set; }
        public string ConnectionStringLog { get; set; }
    }
}
