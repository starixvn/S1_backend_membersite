namespace MemberSite.Service.Model
{
    public class IPCountry
    {
        public uint IpFrom { get; set; }
        public uint IpTo { get; set; }
        public string Code { get; set; }
        public string Country { get; set; }
    }

    public class IpCountryWithAccess
    {
        public bool IsAccess
        {
            get
            {
                return !Forbidden;
            }
        }

        public string Code { get; set; }

        public bool Forbidden { get; set; }

        public string Message { get; set; }

        public string RemoteIpAddress { get; set; }

        public IpCountryWithAccess(string ip, string message)
        {
            RemoteIpAddress = ip;
            Code = string.Empty;
            Forbidden = true;
            Message = $"[{ip}] {message}";
        }
    }

    public class CountryCode
    {
        public string Code { get; set; }
        public string Country { get; set; }

        public CountryCode(IPCountry iPCountry)
        {
            Code = iPCountry.Code;
            Country = iPCountry.Country.Replace("-", "").Replace("\r", "");
        }
    }
    
    public class RegisterRejectClick
    {
        public int SiteId { get; set; }
        public string Domain { get; set; }
        public string ClickTime { get; set; }
    }
}