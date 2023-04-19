using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.BLL
{
    public class CommonBLL
    {
        private readonly CourierAdapter _courierAdapter;
        private readonly string _commonServiceUrl;
        private readonly ICacheHelper _redisHelper;
        private readonly DateTimeHelper _dateTimeHelper;

        public CommonBLL(IServiceProvider provider)
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            _commonServiceUrl = configuration.GetValue<string>("CommonService");
            _courierAdapter = provider.GetRequiredService<CourierAdapter>();
            _redisHelper = provider.GetRequiredService<ICacheHelper>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
        }

        public void SendWelcomeMail(int custId)
        {
            var apiUrl = $"{_commonServiceUrl}Mail/SendWelcomeMail";

            dynamic req = new
            {
                CustId = custId,
            };

            _courierAdapter.MyPostAsync<bool, dynamic>(apiUrl, req);
        }

        public void SendResetPwdMail(int custId, string resetPasswordUrl)
        {
            var apiUrl = $"{_commonServiceUrl}Mail/RestPassword";

            dynamic req = new
            {
                CustId = custId,
                resetPasswordUrl
            };

            _courierAdapter.MyPostAsync<bool, dynamic>(apiUrl, req);
        }
        public async Task<ApiResult> SendResetPwdMailAsync(int custId, string resetPasswordUrl)
        {
            var apiUrl = $"{_commonServiceUrl}Mail/RestPassword";

            dynamic req = new
            {
                CustId = custId,
                resetPasswordUrl
            };

            ApiResult result = await _courierAdapter.MyPostAsync<bool, dynamic>(apiUrl, req);

            return result;
        }
        
        public async Task<ApiResult<SendOTPSMSResult>> SendOTPSMSAsync(int custId, int currencyId)
        {
            var time = await _dateTimeHelper.ToCurrencyZone(DateTime.UtcNow, currencyId);
            var key = $"SendOTPSMS:{time:yyyyMMdd}:{custId}";
            var cnt = _redisHelper.TryGetOrCreate(key, TimeSpan.FromDays(1), () => 0, RedisDataBase.Shared);

            if (cnt >= 5)
            {
                return new ApiResult<SendOTPSMSResult>(-1,"Exceeded the daily sending limit.", null);
            }
            
            var apiUrl = $"{_commonServiceUrl}OTPSMS/SendOTPSMS";

            var req = new
            {
                custId
            };

            var apiResult = await _courierAdapter.MyPostAsync<SendOTPSMSResult, object>(apiUrl, req);
            if (apiResult.ErrorCode == (int) ErrorCode.Success)
            {
                _redisHelper.StringSet(key, cnt + 1, TimeSpan.FromDays(1), RedisDataBase.Shared);
            }

            return apiResult;
        }
        public async Task<ApiResult> VerifyOTPAsync(int custId, string inputOTP)
        {
            var apiUrl = $"{_commonServiceUrl}OTPSMS/VerifyOTP";

            dynamic req = new
            {
                CustId = custId,
                InputOTP = inputOTP
            };

            ApiResult apiResult = await _courierAdapter.MyPostAsync<bool, dynamic>(apiUrl, req);
            return apiResult;
        }
        public async Task<ApiResult<SendOTPSMSResult>> GetOTPVerifyInfo(int custId)
        {
            var apiUrl = $"{_commonServiceUrl}OTPSMS/GetOTPVerifyInfo";

            dynamic req = new
            {
                custId
            };

            var apiResult = await _courierAdapter.MyPostAsync<SendOTPSMSResult, dynamic>(apiUrl, req);

            return apiResult;
        }
        public async Task<ApiResult<SendOTPEmailResult>> SendOTPEmailAsync(int custId, string OTPEmailUrl)
        {
            var apiUrl = $"{_commonServiceUrl}Mail/OTPEmail";

            dynamic req = new
            {
                custId,
                OTPEmailUrl
            };

            var apiResult = await _courierAdapter.MyPostAsync<SendOTPEmailResult, dynamic>(apiUrl, req);

            return apiResult;
        }
        public async Task<ApiResult> VerifyEmailAsync(int custId, string inputOTP)
        {
            var apiUrl = $"{_commonServiceUrl}Mail/VerifyOTP";

            dynamic req = new
            {
                CustId = custId,
                InputOTP = inputOTP
            };

            ApiResult apiResult = await _courierAdapter.MyPostAsync<bool, dynamic>(apiUrl, req);
            return apiResult;
        }
    }
}
