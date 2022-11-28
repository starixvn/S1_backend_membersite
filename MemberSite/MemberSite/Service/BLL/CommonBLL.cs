using MemberSite.Service.Library;
using MemberSite.Service.Model;

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
        
        public async Task<ApiResult<SendOTPSMSResult>> SendOTPSMSAsync(int custId)
        {
            var apiUrl = $"{commonServiceUrl}OTPSMS/SendOTPSMS";

            dynamic req = new
            {
                custId
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
        public async Task<ApiResult<SendOTPSMSResult>> GetOTPVerifyInfo(int custId)
        {
            var apiUrl = $"{commonServiceUrl}OTPSMS/GetOTPVerifyInfo";

            dynamic req = new
            {
                custId
            };

            var apiResult = await courierAdapter.MyPostAsync<SendOTPSMSResult, dynamic>(apiUrl, req);

            return apiResult;
        }
        public async Task<ApiResult<SendOTPEmailResult>> SendOTPEmailAsync(int custId, string OTPEmailUrl)
        {
            var apiUrl = $"{commonServiceUrl}Mail/OTPEmail";

            dynamic req = new
            {
                custId,
                OTPEmailUrl
            };

            var apiResult = await courierAdapter.MyPostAsync<SendOTPEmailResult, dynamic>(apiUrl, req);

            return apiResult;
        }
        public async Task<ApiResult> VerifyEmailAsync(int custId, string inputOTP)
        {
            var apiUrl = $"{commonServiceUrl}Mail/VerifyOTP";

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
