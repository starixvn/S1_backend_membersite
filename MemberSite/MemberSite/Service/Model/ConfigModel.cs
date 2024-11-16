namespace MemberSite.Service.Model
{
    public class JwtConfig
    {
        public bool IsJwt { get; set; }
        public string Issuer { get; set; }
        public string SignKey { get; set; }
    }

    public class SiteCloudFrontImageInfo
    {
        public string OldImageUrl { get; set; }

        public string MaintainUrl { get; set; }

        public string HotEventImageUrl { get; set; }

        public string BannerImageUrl { get; set; }
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

    }

    public class ServiceHostConfig
    {
        public string CommonService { get; set; }
        public string TpService { get; set; }
        public string PayService { get; set; }
        public string BonusService { get; set; }
        public string StoreService { get; set; }
        public string BackendService { get; set; }
    }

    public class ReCaptchaEnterpriseSetting
    {
        public string ProjectId { get; set; }

        public decimal RiskLimit { get; set; }
    }

    public class ReCaptchaHighRiskDomainSetting
    {
        public IEnumerable<string> DomainList { get; set; }

        public decimal RiskLimit { get; set; }
    }
}
