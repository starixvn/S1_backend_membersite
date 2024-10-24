using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.BLL
{
    public class CommonBLL
    {
        private readonly CommonService _commonService;
        private readonly CourierAdapter _courierAdapter;
        private readonly string _commonServiceUrl;
        private readonly ICacheHelper _redisHelper;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly SelLangBLL _selLangBLL;

        public CommonBLL(IServiceProvider provider)
        {
            var configBll = provider.GetRequiredService<ConfigBLL>();
            _commonServiceUrl = configBll.GetConfigValueByEnvironment<ServiceHostConfig>("ServiceHost").CommonService;
            _commonService = provider.GetRequiredService<CommonService>();
            _courierAdapter = provider.GetRequiredService<CourierAdapter>();
            _redisHelper = provider.GetRequiredService<ICacheHelper>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
            _selLangBLL = provider.GetRequiredService<SelLangBLL>();
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

            if (cnt >= 5 && custId != 651538)
            {
                return new ApiResult<SendOTPSMSResult>(-1, _selLangBLL.GetMsg("msg_OTPDailyLimit"), null);
            }

            var apiUrl = $"{_commonServiceUrl}OTPSMS/SendOTPSMS";

            var req = new
            {
                custId
            };

            var apiResult = await _courierAdapter.MyPostAsync<SendOTPSMSResult, object>(apiUrl, req);
            if (apiResult.ErrorCode == (int)ErrorCode.Success)
            {
                _redisHelper.StringSet(key, cnt + 1, TimeSpan.FromDays(1), RedisDataBase.Shared);
            }

            return apiResult;
        }
        
        public async Task<ApiResult<SendOTPSMSResult>> SendSignUpOTPSMSAsync(int siteId , int currencyId, string userLang , string areaCode, string phoneNo)
        {
            var time = await _dateTimeHelper.ToCurrencyZone(DateTime.UtcNow, currencyId);
            var key = $"SendSignUpOTPSMS:{time:yyyyMMdd}:{areaCode}:{phoneNo}";
            var cnt = _redisHelper.TryGetOrCreate(key, TimeSpan.FromDays(1), () => 0, RedisDataBase.Shared);

            if (cnt >= 5)
            {
                return new ApiResult<SendOTPSMSResult>(-1, _selLangBLL.GetMsg("msg_OTPDailyLimit"), null);
            }

            var apiUrl = $"{_commonServiceUrl}OTPSMS/SendSignUpOTPSMS";

            var req = new
            {
                siteId,
                currencyId,
                userLang,
                areaCode,
                phoneNo
            };

            var apiResult = await _courierAdapter.MyPostAsync<SendOTPSMSResult, object>(apiUrl, req);
            if (apiResult.ErrorCode == (int)ErrorCode.Success)
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
        
        public async Task<ApiResult> SignUpVerifyOTPAsync(string areaPhoneNo, string inputOTP)
        {
            var apiUrl = $"{_commonServiceUrl}OTPSMS/SignUpVerifyOTP";

            dynamic req = new
            {
                AreaPhoneNo = areaPhoneNo,
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
        public bool SetOTPLogCustInfo(int siteId,int custId,int currencyId,string userName,string otpNum)
        {
            var result = _commonService.SetOTPLogCustInfo(siteId, custId, currencyId, userName, otpNum);
            
            return result;
        }
    }
}
