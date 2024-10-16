using Newtonsoft.Json;

namespace MemberSite.Service.Model
{
    public class RecaptchaResponse
    {
        public bool success { get; set; }
        [JsonProperty("error-codes")]
        public List<string> errorCodes { get; set; }
    }

    public class RecaptchaSiteInfo
    {
        public string Country { get; set; }
        public string SiteKey { get; set; }
    }

    public class RecaptchaSetting
    {
        public string ApiUrl { get; set; }
        public Dictionary<string, RecaptchaGroup> Groups { get; set; }
    }

    public class RecaptchaGroup
    {
        public string SiteKey { get; set; }
        public string Secret { get; set; }
    }

    #region 

    public class AssessmentRequest
    {
        public string Token { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;

        public AssessmentRequest(string token, string action)
        {
            Token = token;
            Action = action;
        }
    }

    public class AssessmentResult
    {
        public bool success { get { return IsRisk is false; } }
        public bool IsFinish { get; set; } = false;
        public bool IsVaild { get; set; } = false;
        public string Str_Score { get { return Int_Score.ToString(); } }
        public decimal Int_Score { get; set; } = -1;
        public decimal RiskLimit { get; set; } = decimal.Zero;
        public bool IsRisk { get { return Int_Score < RiskLimit; } }
        public List<string> errorCodes { get; set; }

        public AssessmentResult(bool isVaild, bool isFinish = true, string message = null)
        {
            IsFinish = isFinish;
            IsVaild = isVaild;
            errorCodes = new List<string>() { message };
        }

        public AssessmentResult(decimal score, decimal riskLimit, string message = null)
        {
            IsFinish = true;
            IsVaild = true;
            Int_Score = score;
            RiskLimit = riskLimit;
            errorCodes = new List<string>() { message };
        }
    }

    #endregion
}