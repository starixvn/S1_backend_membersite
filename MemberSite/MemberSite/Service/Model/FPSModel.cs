
namespace MemberSite.Service.Model
{
    public class FPSRequest
    {
        //__di
        public string DI { get; set; }
        //__bi
        public string BI { get; set; }
        //detecas-analysis
        public string DetecasAnalysis { get; set; }
        //__fdi
        public string SessionKey { get; set; }
        //tk
        public string Token { get; set; }
    }
    
    public class FPSApiRequest
    {
        public string Account { get; set; }
        //__di
        public string DI { get; set; }
        //__bi
        public string BI { get; set; }
        public string IP { get; set; }
        public string UserAgent { get; set; }
        //detecas-analysis
        public string DetecasAnalysis { get; set; }
        public string AcceptsTypes { get; set; }
        //__fdi
        public string SessionKey { get; set; }
        //tk
        public string Token { get; set; }
        public string Url { get; set; }
    }
}