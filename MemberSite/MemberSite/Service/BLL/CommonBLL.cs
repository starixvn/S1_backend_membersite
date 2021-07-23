using MemberSite.Service.Library;
using MemberSite.Service.Model;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace MemberSite.Service.BLL
{
    public class CommonBLL
    {
        private readonly CourierAdapter courierAdapter;
        private readonly string commonServiceUrl;
        private readonly Tools tools;

        public CommonBLL(IConfiguration configuration, CourierAdapter courierAdapter, Tools tools)
        {
            this.courierAdapter = courierAdapter;
            commonServiceUrl = configuration.GetValue<string>("CommonService");
            this.tools = tools;
        }

        public void SendWelcomeMail(int custId)
        {
            var apiUrl = $"{commonServiceUrl}Mail/SendWelcomeMail";

            dynamic req = new
            {
                CustId = custId,
            };

            courierAdapter.MyPostAsync<bool, dynamic>(apiUrl, req);
        }

        public void SendResetPwdMail(int custId, string resetPasswordUrl)
        {
            var apiUrl = $"{commonServiceUrl}Mail/RestPassword";

            dynamic req = new
            {
                CustId = custId,
                resetPasswordUrl
            };

            courierAdapter.MyPostAsync<bool, dynamic>(apiUrl, req);
        }
        public async Task<ApiResult> SendResetPwdMailAsync(int custId, string resetPasswordUrl)
        {
            var apiUrl = $"{commonServiceUrl}Mail/RestPassword";

            dynamic req = new
            {
                CustId = custId,
                resetPasswordUrl
            };

            ApiResult result = await courierAdapter.MyPostAsync<bool, dynamic>(apiUrl, req);

            return result;
        }

        public async Task<ApiResult> SendVerifySMSAsync(Customer custInfo)
        {
            var apiUrl = $"{commonServiceUrl}MoviderSMS/SendVerifySMS";

            dynamic req = new
            {
                CustId = custInfo.CustId,
                ip = tools.GetClientIP()
            };

            if (custInfo.CurrencyId == 51)
            {
                apiUrl = $"{commonServiceUrl}TwilioSMS/VerificationsSMS";
            }

            ApiResult result = await courierAdapter.MyPostAsync<bool, dynamic>(apiUrl, req);

            return result;
        }
        public async Task<ApiResult> AcknowledgeVerifySMS(Customer custInfo, string siteName, string request_id, string code)
        {
            var apiUrl = $"{commonServiceUrl}MoviderSMS/AcknowledgeVerifySMS";

            dynamic req = new
            {
                siteName = siteName,
                request_id = request_id,
                code = code
            };

            if (custInfo.CurrencyId == 51)
            {
                apiUrl = $"{commonServiceUrl}TwilioSMS/VerificationCheckSMS";

                req = new
                {
                    siteName = siteName,
                    sid = request_id,
                    code = code
                };
            }

            ApiResult apiResult = await courierAdapter.MyPostAsync<bool, dynamic>(apiUrl, req);

            return apiResult;
        }
        public async Task<ApiResult<SendOTPSMSResult>> SendOTPSMSAsync(int custId)
        {
            var apiUrl = $"{commonServiceUrl}OTPSMS/SendOTPSMS";

            dynamic req = new
            {
                custId = custId
            };

            var apiResult = await courierAdapter.MyPostAsync<SendOTPSMSResult, dynamic>(apiUrl, req);

            return apiResult;
        }
        public async Task<ApiResult> VerifyOTPAsync(int custId, string inputOTP)
        {
            var apiUrl = $"{commonServiceUrl}OTPSMS/VerifyOTP";

            dynamic req = new
            {
                CustId = custId,
                InputOTP = inputOTP
            };

            ApiResult apiResult = await courierAdapter.MyPostAsync<bool, dynamic>(apiUrl, req);
            return apiResult;
        }
    }
}
