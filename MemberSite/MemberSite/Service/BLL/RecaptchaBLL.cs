using Google.Api.Gax.ResourceNames;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.RecaptchaEnterprise.V1;
using MemberSite.Service.BLL.Interface;
using MemberSite.Service.DAL;
using MemberSite.Service.Model;
using Newtonsoft.Json.Linq;

namespace MemberSite.Service.BLL
{
    public class RecaptchaBLL
    {
        private readonly RecaptchaSetting _recaptcha;
        private readonly RecaptchaService _recaptchaSvc;
        private readonly ServerInfoBll _serverInfoBll;
        private readonly IRecaptchaApi _recaptchaApi;
        private readonly CheckIpBLL _checkIpBll;
        private readonly ILogger<RecaptchaBLL> _logger;

        private readonly GoogleCredential _credential;
        private readonly ReCaptchaEnterpriseSetting _reCaptchaEnterpriseSetting;

        public RecaptchaBLL(IServiceProvider provider)
        {
            _recaptcha = provider.GetRequiredService<RecaptchaSetting>();
            _recaptchaSvc = provider.GetRequiredService<RecaptchaService>();
            _recaptchaApi = provider.GetRequiredService<IRecaptchaApi>();
            _serverInfoBll = provider.GetRequiredService<ServerInfoBll>();
            _checkIpBll = provider.GetRequiredService<CheckIpBLL>();
            _logger = provider.GetRequiredService<ILogger<RecaptchaBLL>>();

            var configBll = provider.GetRequiredService<ConfigBLL>();
            _credential = GoogleCredential.FromJson(configBll.GetConfigValue("ReCaptchaEnterprise"));
            _reCaptchaEnterpriseSetting = configBll.GetConfigValue<ReCaptchaEnterpriseSetting>("ReCaptchaEnterpriseSetting");
        }

        #region ReCaptcha Free Policy

        public RecaptchaResponse Verify(string token)
        {
            var domain = _serverInfoBll.GetMainDomain();
            var name = _recaptchaSvc.GetGroup(domain).Result;
            var setting = _recaptcha.Groups[name];

            return _recaptchaApi.Verify(_recaptcha.ApiUrl, setting.Secret, token);
        }

        public async Task<RecaptchaSiteInfo> GetInfo(string clientIp)
        {
            var setting = await GetRecaptchaSetting();

            var country = await _checkIpBll.CheckIp(clientIp);
            return new RecaptchaSiteInfo()
            {
                Country = country.Code,
                SiteKey = setting.SiteKey
            };
        }

        #endregion

        private async Task<RecaptchaGroup> GetRecaptchaSetting()
        {
            var domain = _serverInfoBll.GetDomain();
            var name = await _recaptchaSvc.GetGroup(domain);
            return _recaptcha.Groups[name];
        }

        #region ReCaptcha Enterprise
        public async Task<AssessmentResult> CreateAssessmentAsync(AssessmentRequest request)
        {
            var message = string.Empty;
            Assessment response = null;
            try
            {
                var setting = await GetRecaptchaSetting();
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

                response = await client.CreateAssessmentAsync(createAssessmentRequest);
            }
            catch (Exception ex)
            {
                message = $"Exception happen: {ex.Message}";
                _logger.LogError(message);
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
            return new AssessmentResult((decimal)response.RiskAnalysis.Score, _reCaptchaEnterpriseSetting.RiskLimit);
        }

        #endregion

    }
}