using MemberSite.Service.Model.enums;

namespace MemberSite.Service.Model.Line
{
    public class IssueAccessTokenResponseModel
    {
        public string access_token { get; set; }
        public string token_type { get; set; }
        public string refresh_token { get; set; }
        public int expires_in { get; set; }
        public string scope { get; set; }
        public string id_token { get; set; }
    }

    public class LineProfileResponseModel
    {
        public string userId { get; set; }
        public string displayName { get; set; }
        public string pictureUrl { get; set; }
        public string statusMessage { get; set; }
    }

    public class CustLineProfileModel
    {
        public int CustId { get; set; }
        public string userId { get; set; }
        public DateTime CreateTime { get; set; }
    }

    public class LineIssueTokenModel
    {
        public FlowType FlowType { get; set; }
        public string Token { get; set; }
        public DateTime CreateTime { get; set; }
    }

    public class CallBackReponseModel
    {
        public FlowType Type { get; set; }
        public LineLoginErrorCode ErrorCode { get; set; }
        public string Message { get; set; }
        public int CustId { get; set; }
        public string UserId { get; set; }
    }

    public class LineLoginDataModel
    {
        public FlowType Type { get; set; }
        public bool IsPersistent { get; set; }
        public string Url { get; set; }
        public string Platform { get; set; }
        /// <summary>
        /// 原本的登入流程兩個值都會用到，先行保留
        /// </summary>
        public string RequestHeaderUserAgent { get; set; }
        public string UserAgent { get; set; }
        public string Screen { get; set; }
        public string Di { get; set; }
    }

    public enum FlowType
    {
        Undefine = 0,
        Register = 1,
        Login = 2,
        Binding = 3
    }

}
