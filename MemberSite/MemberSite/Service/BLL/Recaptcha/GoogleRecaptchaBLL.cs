using Google.Api.Gax.ResourceNames;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.RecaptchaEnterprise.V1;
using MemberSite.Service.Model;
using Newtonsoft.Json;

namespace MemberSite.Service.BLL
{
    public class GoogleRecaptchaBLL
    {
        private readonly RecaptchaSetting _recaptcha;
        private readonly ILogger<GoogleRecaptchaBLL> _logger;

        private readonly GoogleCredential _credential;
        private readonly ReCaptchaEnterpriseSetting _reCaptchaEnterpriseSetting;
        private readonly Dictionary<int, ReCaptchaHighRiskDomainSetting> _reCaptchaHighRiskDomainSetting;

        public GoogleRecaptchaBLL(IServiceProvider provider)
        {
            _recaptcha = provider.GetRequiredService<RecaptchaSetting>();
            _logger = provider.GetRequiredService<ILogger<GoogleRecaptchaBLL>>();

            var configBll = provider.GetRequiredService<ConfigBLL>();
            _credential = GoogleCredential.FromJson(configBll.GetConfigValue("ReCaptchaEnterprise"));
            _reCaptchaEnterpriseSetting = configBll.GetConfigValue<ReCaptchaEnterpriseSetting>("ReCaptchaEnterpriseSetting");
            _reCaptchaHighRiskDomainSetting = configBll.GetConfigValuesBySiteId<ReCaptchaHighRiskDomainSetting>("ReCaptchaHighRiskDomainSetting");
        }

        #region ReCaptcha Free Policy

        //public RecaptchaResponse Verify(string token)
        //{
        //    var domain = _serverInfoBll.GetMainDomain();
        //    var name = _recaptchaSvc.GetGroup(domain).Result;
        //    var setting = _recaptcha.Groups[name];

        //    return _recaptchaApi.Verify(_recaptcha.ApiUrl, setting.Secret, token);
        //}

        //public async Task<RecaptchaSiteInfo> GetInfo(string clientIp)
        //{
        //    var setting = await GetRecaptchaSetting();

        //    var country = await _checkIpBll.CheckIp(clientIp);
        //    return new RecaptchaSiteInfo()
        //    {
        //        Country = country.Code,
        //        SiteKey = setting.SiteKey
        //    };
        //}

        #endregion

        public RecaptchaGroup GetSetting(string groupName)
        {
            return new RecaptchaGroup(Model.enums.CaptchaType.GoogleRecaptcha, _recaptcha.Groups[groupName]);
        }

        #region ReCaptcha Enterprise

        public async Task<bool> VerifyAsync(RecaptchaGroup setting, CaptchaCommonRequest request)
        {
            var result = await CreateAssessmentAsync(setting, request);
            _logger.LogInformation($"reCaptcha verify result={JsonConvert.SerializeObject(result)}");
            if (!result.success)
            {
                return false;
            }
            return true;
        }

        private async Task<AssessmentResult> CreateAssessmentAsync(RecaptchaGroup setting, CaptchaCommonRequest request)
        {
            var message = string.Empty;
            Assessment response = null;
            try
            {
                var client = new RecaptchaEnterpriseServiceClientBuilder
                {
                    Credential = _credential
                }.Build();

                ProjectName projectName = new ProjectName(_reCaptchaEnterpriseSetting.ProjectId);

                CreateAssessmentRequest createAssessmentRequest = new CreateAssessmentRequest()
                {
                    Assessment = new Assessment()
                    {
                        Event = new Event()
                        {
                            SiteKey = setting.SiteKey,
                            Token = request.Token,
                            ExpectedAction = request.Action
                        },
                    },
                    ParentAsProjectName = projectName
                };
                //_logger.LogInformation($"ReCaptcha Enterprise AssessmentRequest={JsonConvert.SerializeObject(createAssessmentRequest)}");
                response = await client.CreateAssessmentAsync(createAssessmentRequest);
                //.LogInformation($"ReCaptcha Enterprise AssessmentResponse={JsonConvert.SerializeObject(response)}");
            }
            catch (Exception ex)
            {
                message = $"Google CreateAssessment Exception happen: {ex.Message}";
                _logger.LogError(ex, message);
                return new AssessmentResult(false, false, ex.Message);
            }

            if (response.TokenProperties.Valid == false)
            {
                message = $"The CreateAssessment call failed because the token was: {response.TokenProperties.InvalidReason.ToString()}";

                _logger.LogInformation(message);
                return new AssessmentResult(false, message: "Token Valid Fail.");
            }

            if (response.TokenProperties.Action != request.Action)
            {
                message = $"The action attribute in reCAPTCHA tag is: {response.TokenProperties.Action}\n" +
                    $"The action attribute in the reCAPTCHA tag does not match the action you are expecting to score.\n" +
                    $"InvalidReason = {response.TokenProperties.InvalidReason.ToString()}";

                _logger.LogInformation(message);
                return new AssessmentResult(false, message: "Action Valid Fail.");
            }

            message = $"The reCAPTCHA score is: {(decimal)response.RiskAnalysis.Score}\n" +
                    $"InvalidReason = {response.TokenProperties.InvalidReason.ToString()}\n" +
                    $"RiskAnalysisReasons = {string.Join("/", response.RiskAnalysis.Reasons.Select(x => x.ToString()))}";

            _logger.LogInformation(message);

            var highRiskConfig = _reCaptchaHighRiskDomainSetting[request.SiteId];
            if (highRiskConfig is not null && highRiskConfig.DomainList.Contains(request.MainDomain))
            {
                _logger.LogInformation($"This Domain Use HighRisk Check. Domain={request.MainDomain}  RiskLimit={highRiskConfig.RiskLimit}");
                return new AssessmentResult((decimal)response.RiskAnalysis.Score, highRiskConfig.RiskLimit);
            }
            return new AssessmentResult((decimal)response.RiskAnalysis.Score, _reCaptchaEnterpriseSetting.RiskLimit);
        }

        #endregion

    }
}