using MemberSite.Service.DAL;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using MemberSite.Service.Model.Line;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Collections.Specialized;
using System.Net;
using System.Text;

namespace MemberSite.Service.BLL
{
    public class LineLoginBLL
    {
        private readonly CustInfoService _custInfoService;
        private readonly AuthService _authService;
        private readonly AuthorizeBLL _authorizeBll;
        private readonly IOptions<LineLoginChannelConfig> _lineLoginChannelConfig;
        private readonly ILogger<LineLoginBLL> _logger;

        #region Constructor

        public LineLoginBLL(IServiceProvider provider)
        {
            _custInfoService = provider.GetRequiredService<CustInfoService>();
            _authService = provider.GetRequiredService<AuthService>();
            _authorizeBll = provider.GetRequiredService<AuthorizeBLL>();
            _lineLoginChannelConfig = provider.GetRequiredService<IOptions<LineLoginChannelConfig>>();
            _logger = provider.GetRequiredService<ILogger<LineLoginBLL>>();
        }

        #endregion

        public bool IssueLineToken(FlowType type, out int errorCode, out string token)
        {
            token = Guid.NewGuid().ToString();
            errorCode = 0;
            try
            {
                if (type == FlowType.Binding)
                {
                    //如果走綁定流程，先檢查是否已經綁定，已綁定則不可再次載綁定
                    var authData = _authorizeBll.GetAuthData();
                    if (authData != null && authData.CustId != 0)
                    {
                        var custLineProfile = GetCustLineProfile(authData.CustId, null);
                        if (custLineProfile != null)
                        {
                            errorCode = (int)LineLoginErrorCode.MemberAccountAlreadyBinding;
                            _logger.LogInformation($"IssueLineToken Error：Binding Flow But Already Binding. Can't Bind Again. authData={JsonConvert.SerializeObject(authData)}");
                            return false;
                        }
                    }
                }

                if (!_authService.IssueLineToken((int)type, token))
                {
                    errorCode = (int)LineLoginErrorCode.IssueLineTokenFail;
                    _logger.LogInformation($"IssueLineToken Fail.");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                errorCode = (int)LineLoginErrorCode.IssueLineTokenException;
                _logger.LogError(ex, $"IssueLineToken Fail: Exception happened. Message={ex.Message}");
                return false;
            }
        }

        public async Task<CallBackReponseModel> LineLoginCallBack(string code, string state, string error, int errorCode, string errorDescription, string errorMessage, string redirectUri)
        {
            var lineUserId = string.Empty;
            var config = _lineLoginChannelConfig.Value;
            var resultCode = LineLoginErrorCode.Success;

            if (!CheckLineResponse(code, state, error, errorCode, errorDescription, errorMessage, out resultCode))
            {
                return new CallBackReponseModel { Type = FlowType.Undefine, ErrorCode = resultCode };
            }

            var lineIssueToken = await _authService.GetLineToken(state);

            if (!CheckToken(config, lineIssueToken, out resultCode))
            {
                return new CallBackReponseModel { Type = lineIssueToken.FlowType, ErrorCode = resultCode };
            }

            if (!GetLineUserId(config, code, redirectUri, out lineUserId, out resultCode))
            {
                return new CallBackReponseModel { Type = lineIssueToken.FlowType, ErrorCode = resultCode };
            }

            try
            {
                var authData = _authorizeBll.GetAuthData();

                var custLineProfile = GetCustLineProfile(0, lineUserId);

                switch (lineIssueToken.FlowType)
                {
                    case FlowType.Register:
                        {
                            if (authData != null && authData.CustId != 0)
                            {
                                resultCode = LineLoginErrorCode.RegisterAuthInvaild;
                                _logger.LogInformation("Register Flow Error: This is Register Token But authData is not null and CustId != 0.");
                                return new CallBackReponseModel { Type = lineIssueToken.FlowType, ErrorCode = resultCode };
                            }
                            if (custLineProfile != null)
                            {
                                resultCode = LineLoginErrorCode.RegisterLineProfileInvaild;
                                _logger.LogInformation("Register Flow Error: This is Register Token But custLineProfile is not null.");
                                return new CallBackReponseModel { Type = lineIssueToken.FlowType, ErrorCode = resultCode };
                            }
                            return new CallBackReponseModel { Type = lineIssueToken.FlowType, ErrorCode = resultCode, UserId = lineUserId };
                        }
                    case FlowType.Login:
                        {
                            if (custLineProfile == null)
                            {
                                resultCode = LineLoginErrorCode.LoginLineProfileInvaild;
                                _logger.LogInformation("Login Flow Error: This is Login Token But custLineProfile is null.");
                                return new CallBackReponseModel { Type = lineIssueToken.FlowType, ErrorCode = resultCode };
                            }
                            return new CallBackReponseModel { Type = lineIssueToken.FlowType, ErrorCode = resultCode, CustId = custLineProfile.CustId };
                        }
                    case FlowType.Binding:
                        {
                            if (authData == null || authData.CustId == 0)
                            {
                                resultCode = LineLoginErrorCode.BindingAuthInvaild;
                                _logger.LogInformation("Binding Flow Error: This is Binding Token But authData is null and CustId = 0.");
                                return new CallBackReponseModel { Type = lineIssueToken.FlowType, ErrorCode = resultCode };
                            }
                            if (custLineProfile != null)
                            {
                                resultCode = LineLoginErrorCode.BindingLineProfileInvaild;
                                _logger.LogInformation("Binding Flow Error: This is Binding Token But custLineProfile already exist.");
                                return new CallBackReponseModel { Type = lineIssueToken.FlowType, ErrorCode = resultCode };
                            }
                            if (!InsertCustLineProfile(authData.CustId, lineUserId, out resultCode))
                            {
                                return new CallBackReponseModel { Type = lineIssueToken.FlowType, ErrorCode = resultCode };
                            }
                            return new CallBackReponseModel { Type = lineIssueToken.FlowType, ErrorCode = resultCode };
                        }
                    default:
                        {
                            resultCode = LineLoginErrorCode.UndefineFlow;
                            _logger.LogInformation("Flow Error: UnDefine flow.");
                            return new CallBackReponseModel { Type = lineIssueToken.FlowType, ErrorCode = resultCode };
                        }
                }
            }
            catch (Exception ex)
            {
                resultCode = LineLoginErrorCode.CallBackException;
                _logger.LogError(ex, $"LineLoginCallBack Fail：Exception happen.");
                return new CallBackReponseModel { Type = lineIssueToken.FlowType, ErrorCode = resultCode };
            }
        }

        private CustLineProfileModel GetCustLineProfile(int custId, string encryptUserId)
        {
            return _custInfoService.GetCustLineProfile(custId, encryptUserId);
        }

        private bool InsertCustLineProfile(int custId, string lineUserId, out LineLoginErrorCode resultCode)
        {
            resultCode = LineLoginErrorCode.Success;
            try
            {
                if (!_custInfoService.InsertCustLineProfile(custId, lineUserId))
                {
                    resultCode = LineLoginErrorCode.InsertLineProfileFail;
                    _logger.LogInformation("InsertCustLineProfile Fail.");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                resultCode = LineLoginErrorCode.InsertLineProfileException;
                _logger.LogError(ex, $"InsertCustLineProfile Fail: Exception happened.");
                return false;
            }
        }

        private bool CheckLineResponse(string code, string state, string error, int errorCode, string errorDescription, string errorMessage, out LineLoginErrorCode resultCode)
        {
            resultCode = 0;
            if (!string.IsNullOrWhiteSpace(error))
            {
                resultCode = LineLoginErrorCode.LineCallbackError;
                _logger.LogInformation($"CheckLineResponse Fail：error happened. error={error}.errorCode={errorCode}. error_description={errorDescription}. errormessage={errorMessage}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(code))
            {
                //用戶在Line驗證畫面選擇取消
                resultCode = LineLoginErrorCode.CodeInvaild;
                _logger.LogInformation("CheckLineResponse Fail：code is null or empty.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(state))
            {
                resultCode = LineLoginErrorCode.StateInvaid;
                _logger.LogInformation("CheckLineResponse Fail：state (token) is null or empty.");
                return false;
            }

            return true;
        }

        private bool CheckToken(LineLoginChannelConfig config, LineIssueTokenModel data, out LineLoginErrorCode resultCode)
        {
            resultCode = 0;

            if (data == null)
            {
                resultCode = LineLoginErrorCode.NotFoundToken;
                _logger.LogInformation("CheckToken Fail：not found this token.");
                return false;
            }

            if (!Enum.IsDefined(typeof(FlowType), data.FlowType) || data.FlowType == FlowType.Undefine)
            {
                resultCode = LineLoginErrorCode.FlowTypeInvaild;
                _logger.LogInformation($"CheckToken Fail：Flow Type Is Invalid. FlowType={data.FlowType}");
                return false;
            }


            if (data.CreateTime <= DateTime.UtcNow.AddSeconds(0 - config.ExpiredSeconds))
            {
                resultCode = LineLoginErrorCode.TokenExpired;
                _logger.LogInformation($"CheckToken Fail：LineIssueToken Expired. have to in { data.CreateTime.AddSeconds(config.ExpiredSeconds)}.\r\n" +
                                       $"Token Generate Time ={data.CreateTime}，ExpiredSeconds = {config.ExpiredSeconds}.");
                return false;
            }

            return true;
        }

        private bool GetLineUserId(LineLoginChannelConfig config, string code, string redirectUri, out string lineUserId, out LineLoginErrorCode resultCode)
        {

            resultCode = 0;
            lineUserId = string.Empty;
            string ApiUrl_Token = "https://api.line.me/oauth2/v2.1/token";
            string ApiUrl_Profile = "https://api.line.me/v2/profile";
            try
            {
                WebClient wc = new WebClient();
                wc.Encoding = Encoding.UTF8;
                wc.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                NameValueCollection nvc = new NameValueCollection();

                #region OAuth (Access Token)

                nvc.Add("grant_type", "authorization_code");
                nvc.Add("code", code);
                nvc.Add("redirect_uri", redirectUri);
                nvc.Add("client_id", config.ChannelId);
                nvc.Add("client_secret", config.ChannelSecret);

                var tokenResponseStr = string.Empty;

                try
                {
                    tokenResponseStr = Encoding.UTF8.GetString(wc.UploadValues(ApiUrl_Token, "POST", nvc));
                    _logger.LogInformation($"GetLineUserId LineTokenApi Success:{tokenResponseStr}");
                }
                catch (WebException ex)
                {
                    resultCode = LineLoginErrorCode.GetLineTokenFail;
                    using (var hr = (HttpWebResponse)ex.Response)
                    {
                        if (hr != null)
                        {
                            int statusCode = (int)hr.StatusCode;
                            var sb = new StringBuilder();
                            var sr = new StreamReader(hr.GetResponseStream(), Encoding.UTF8);
                            sb.Append(sr.ReadToEnd());
                            _logger.LogError(ex, $"GetLineUserId LineTokenApi Fail: StatusCode:{statusCode},Content:{sb}");
                        }
                    }
                    return false;
                }

                if (string.IsNullOrWhiteSpace(tokenResponseStr))
                {
                    resultCode = LineLoginErrorCode.LineTokenInvaild;
                    _logger.LogInformation($"GetLineUserId LineTokenApi Fail：token not found.");
                    return false;
                }

                var tokenResponse = JsonConvert.DeserializeObject<IssueAccessTokenResponseModel>(tokenResponseStr);

                if (tokenResponse == null || string.IsNullOrWhiteSpace(tokenResponse.access_token))
                {
                    resultCode = LineLoginErrorCode.LineTokenNotFound;
                    _logger.LogInformation($"GetLineUserId LineProfileApi Fail：userId not found.");
                    return false;
                }

                #endregion

                wc.Headers.Clear();

                #region Profile (User Profile)

                wc.Headers.Add("Authorization", "Bearer " + tokenResponse.access_token);
                var userProfileResponseStr = string.Empty;

                try
                {
                    userProfileResponseStr = wc.DownloadString(ApiUrl_Profile);
                    _logger.LogInformation($"GetLineUserId LineProfileApi Success:{userProfileResponseStr}");
                }
                catch (WebException ex)
                {
                    resultCode = LineLoginErrorCode.GetLineUserProfileFail;
                    using (var hr = (HttpWebResponse)ex.Response)
                    {
                        if (hr != null)
                        {
                            var statusCode = (int)hr.StatusCode;
                            var sb = new StringBuilder();
                            var sr = new StreamReader(hr.GetResponseStream(), Encoding.UTF8);
                            sb.Append(sr.ReadToEnd());
                            _logger.LogError(ex, $"GetLineUserId LineProfileApi Fail: StatusCode:{statusCode},Content:{sb}");
                        }
                    }
                    return false;
                }

                if (string.IsNullOrWhiteSpace(userProfileResponseStr))
                {
                    resultCode = LineLoginErrorCode.LineUserProfileInvaild;
                    _logger.LogInformation($"GetLineUserId LineProfileApi Fail：UserProfile is null.");
                    return false;
                }

                var profileObj = JsonConvert.DeserializeObject<LineProfileResponseModel>(userProfileResponseStr);

                if (profileObj == null || string.IsNullOrWhiteSpace(profileObj.userId))
                {
                    resultCode = LineLoginErrorCode.LineUserIdNotFound;
                    _logger.LogInformation($"GetLineUserId LineProfileApi Fail：userId not found.");
                    return false;
                }

                #endregion

                lineUserId = profileObj.userId;

                return true;
            }
            catch (Exception ex)
            {
                resultCode = LineLoginErrorCode.LineGetUserIdException;
                _logger.LogError(ex, $"GetLineUserId Fail：Exception happen.");
                return false;
            }
        }

    }
}
