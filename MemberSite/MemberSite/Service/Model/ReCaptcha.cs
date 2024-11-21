using MemberSite.Service.Model.enums;
using Newtonsoft.Json;

namespace MemberSite.Service.Model
{
    public class RecaptchaSiteInfo
    {
        public CaptchaType Type { get; set; }
        public string Country { get; set; }
        public string SiteKey { get; set; }
    }

    public class RecaptchaGroup
    {
        public CaptchaType Type { get; set; }
        public string SiteKey { get; set; }
        public string Secret { get; set; }
        public RecaptchaGroup()
        {

        }
        public RecaptchaGroup(CaptchaType type, string siteKey = null, string secret = null)
        {
            Type = type;
            SiteKey = siteKey;
            Secret = secret;
        }
        public RecaptchaGroup(CaptchaType type, RecaptchaGroup recaptchaGroup)
        {
            Type = type;
            SiteKey = recaptchaGroup.SiteKey;
            Secret = recaptchaGroup.Secret;
        }
    }


    #region Common Captcha Use

    public class CaptchaCommonRequest
    {
        public int SiteId { get; set; }
        public string MainDomain { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string ExpectedAction { get; set; } = string.Empty;
        public CaptchaCommonRequest(string token, string action, CaptchaActionType expectedAction)
        {
            Token = token;
            Action = action;
            ExpectedAction = expectedAction.ToString();
        }
    }

    public class CaptchaCommonResponse
    {
        public bool Success { get; set; }
        public string Action { get; set; }
        public List<string> ErrorCodes { get; set; }

        public CaptchaCommonResponse()
        {

        }
        public CaptchaCommonResponse(GoogleRecaptchaResponse response)
        {
            Success = response.success;
            ErrorCodes = response.errorCodes;
        }

        public CaptchaCommonResponse(TurnstileResponse response)
        {
            Success = response.Success;
            ErrorCodes = response.ErrorCodes;
            Action = response.Action;
        }
    }

    #endregion

    #region Google Recaptcha

    public class RecaptchaSetting
    {
        public string ApiUrl { get; set; }
        public Dictionary<string, RecaptchaGroup> Groups { get; set; }
    }

    /// <summary>
    /// Free Policy Use
    /// </summary>
    public class GoogleRecaptchaResponse
    {
        public bool success { get; set; }
        [JsonProperty("error-codes")]
        public List<string> errorCodes { get; set; }
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

    #region NumberCaptcha

    public class NumberCaptchaSetting
    {
        public int Length { get; set; }
        public string RedisKeyPrefix { get; set; } = string.Empty;
        public int ExpiredMins { get; set; }
    }

    public class NumberCaptchaResult
    {
        public int Seconds { get; set; }
        public byte[] Picture { get; set; }
    }
    #endregion

    #region CloudflareTurnstile

    public class CloudflareBaseSetting
    {
        public string ApiUrl { get; set; } = string.Empty;
        public string SiteVerifyUrl { get; set; } = string.Empty;
    }

    public class TurnstileResponse
    {
        // Common field for both success and failure responses
        [JsonProperty("success")]
        public bool Success { get; set; } = false;

        // Optional fields for success response
        [JsonProperty("challenge_ts", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? ChallengeTimestamp { get; set; }

        [JsonProperty("hostname", NullValueHandling = NullValueHandling.Ignore)]
        public string Hostname { get; set; }

        [JsonProperty("action", NullValueHandling = NullValueHandling.Ignore)]
        public string Action { get; set; }

        [JsonProperty("cdata", NullValueHandling = NullValueHandling.Ignore)]
        public string Cdata { get; set; }

        // Optional field for failure response
        [JsonProperty("error-codes", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> ErrorCodes { get; set; }
    }

    #endregion
}