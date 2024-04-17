using Google.Api.Gax.ResourceNames;
using Google.Cloud.RecaptchaEnterprise.V1;
using MemberSite.Service.BLL.Interface;
using MemberSite.Service.DAL;
using MemberSite.Service.Model;

namespace MemberSite.Service.BLL
{
    public class RecaptchaBLL
    {
        private readonly RecaptchaSetting _recaptcha;
        private readonly RecaptchaService _recaptchaSvc;
        private readonly ServerInfoBll _serverInfoBll;
        private readonly IRecaptchaApi _recaptchaApi;
        private readonly CheckIpBLL _checkIpBll;

        public RecaptchaBLL(IServiceProvider provider)
        {
            _recaptcha = provider.GetRequiredService<RecaptchaSetting>();
            _recaptchaSvc = provider.GetRequiredService<RecaptchaService>();
            _recaptchaApi = provider.GetRequiredService<IRecaptchaApi>();
            _serverInfoBll = provider.GetRequiredService<ServerInfoBll>();
            _checkIpBll = provider.GetRequiredService<CheckIpBLL>();
        }

        public RecaptchaResponse Verify(string token)
        {
            var domain = _serverInfoBll.GetMainDomain();
            var name = _recaptchaSvc.GetGroup(domain).Result;
            var setting = _recaptcha.Groups[name];

            return _recaptchaApi.Verify(_recaptcha.ApiUrl, setting.Secret, token);
        }

        public async Task<RecaptchaSiteInfo> GetInfo(string clientIp)
        {
            var domain = _serverInfoBll.GetMainDomain();
            var name = await _recaptchaSvc.GetGroup(domain);
            var setting = _recaptcha.Groups[name];

            var country = await _checkIpBll.CheckIp(clientIp);
            return new RecaptchaSiteInfo()
            {
                Country = country.Code,
                SiteKey = setting.SiteKey
            };
        }

        #region reCaptcha Test Code

        // 建立評估作業，分析 UI 動作的風險。
        // projectID: 您的 Google Cloud 專案 ID。
        // recaptchaKey: 與網站/應用程式相關聯的 reCAPTCHA 金鑰
        // token: 系統產生的權杖 (從用戶端取得)。
        // recaptchaAction: 權杖對應的動作名稱。
        public void createAssessment(string projectID = "massive-cirrus-365808", string recaptchaKey = "6LdwM5cpAAAAADugVdg6OTjWp_VhlvOnATIRKFoG", string token = "action-token", string recaptchaAction = "action-name")
        {
            // 建立 reCAPTCHA 用戶端。
            // 任務：先快取產生用戶端的程式碼 (建議做法) 或呼叫 client.close()，再結束方法。
            RecaptchaEnterpriseServiceClient client = RecaptchaEnterpriseServiceClient.Create();

            ProjectName projectName = new ProjectName(projectID);

            // 建立評估要求。
            CreateAssessmentRequest createAssessmentRequest = new CreateAssessmentRequest()
            {
                Assessment = new Assessment()
                {
                    // 設定要追蹤的事件屬性。
                    Event = new Event()
                    {
                        SiteKey = recaptchaKey,
                        Token = token,
                        ExpectedAction = recaptchaAction
                    },
                },
                ParentAsProjectName = projectName
            };

            Assessment response = client.CreateAssessment(createAssessmentRequest);

            // 確認權杖是否有效。
            if (response.TokenProperties.Valid == false)
            {
                System.Console.WriteLine("The CreateAssessment call failed because the token was: " +
                    response.TokenProperties.InvalidReason.ToString());
                return;
            }

            // 確認是否已執行預期的動作。
            if (response.TokenProperties.Action != recaptchaAction)
            {
                System.Console.WriteLine("The action attribute in reCAPTCHA tag is: " +
                    response.TokenProperties.Action.ToString());
                System.Console.WriteLine("The action attribute in the reCAPTCHA tag does not " +
                    "match the action you are expecting to score");
                return;
            }

            // 取得風險分數和原因。
            // 如要進一步瞭解如何解讀評估結果，請參閱：
            // https://cloud.google.com/recaptcha-enterprise/docs/interpret-assessment
            System.Console.WriteLine("The reCAPTCHA score is: " + ((decimal)response.RiskAnalysis.Score));

            foreach (RiskAnalysis.Types.ClassificationReason reason in response.RiskAnalysis.Reasons)
            {
                System.Console.WriteLine(reason.ToString());
            }
        }

        #endregion

    }
}