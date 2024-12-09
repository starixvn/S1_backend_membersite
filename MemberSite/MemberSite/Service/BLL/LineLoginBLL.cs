using System.Net;
using System.Net.Http.Headers;
using MemberSite.Service.DAL;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using MemberSite.Service.Model.Line;
using Newtonsoft.Json;

namespace MemberSite.Service.BLL
{
    public class LineLoginBLL
    {
        private readonly CustInfoService _custInfoService;
        private readonly AuthService _authService;
        private readonly AuthorizeBLL _authorizeBll;
        private readonly LineLoginChannelConfig _lineLoginChannelConfig;
        private readonly LineLoginConfig _lineLoginConfig;
        private readonly ProxyConfig _proxyConfig;
        private readonly ILogger<LineLoginBLL> _logger;
        private HttpClient client;

        #region Constructor

        public LineLoginBLL(IServiceProvider provider)
        {
            _custInfoService = provider.GetRequiredService<CustInfoService>();
            _authService = provider.GetRequiredService<AuthService>();
            _authorizeBll = provider.GetRequiredService<AuthorizeBLL>();
            var configBll = provider.GetRequiredService<ConfigBLL>();
            _lineLoginChannelConfig = configBll.GetConfigValueByEnvironment<LineLoginChannelConfig>("LineLoginChannelConfig");
            _lineLoginConfig = configBll.GetConfigValue<LineLoginConfig>("LineLoginConfig");
            _proxyConfig = configBll.GetProxyConfig();
            _logger = provider.GetRequiredService<ILogger<LineLoginBLL>>();

            var handler = new HttpClientHandler
            {
                UseProxy = _proxyConfig != null,
                Proxy = _proxyConfig != null ? new WebProxy(_proxyConfig.ProxyHost, _proxyConfig.ProxyPort) : null
            };
            client = new HttpClient(handler);
        }

        #endregion Constructor

        public bool IssueLineToken(FlowType type, out int errorCode, out string token)
        {
            token = Guid.NewGuid().ToString();
            errorCode = 0;
            try
            {
                if (!Enum.IsDefined(typeof(FlowType), type) || type == FlowType.Undefine)
                {
                    errorCode = (int)LineLoginErrorCode.FlowTypeInvaild;
                    _logger.LogInformation($"CheckToken Fail：Flow Type Is Invalid. FlowType={type}");
                    return false;
                }

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
            var config = _lineLoginChannelConfig;
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

        public CustLineProfileModel GetCustLineProfile(int custId, string encryptUserId)
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
                _logger.LogInformation($"CheckToken Fail：LineIssueToken Expired. have to in {data.CreateTime.AddSeconds(config.ExpiredSeconds)}.\r\n" +
                                       $"Token Generate Time ={data.CreateTime}，ExpiredSeconds = {config.ExpiredSeconds}.");
                return false;
            }

            return true;
        }

        private bool GetLineUserId(LineLoginChannelConfig config, string code, string redirectUri, out string lineUserId, out LineLoginErrorCode resultCode)
        {
            resultCode = 0;
            lineUserId = string.Empty;
            try
            {
                var parameters = new Dictionary<string, string>()
                    {
                        { "grant_type", "authorization_code" },
                        { "code", code },
                        { "redirect_uri",redirectUri},
                        { "client_id", config.ChannelId},
                        { "client_secret",config.ChannelSecret}
                    };
                var tokenRequestMessage = new HttpRequestMessage();
                tokenRequestMessage.Content = new FormUrlEncodedContent(parameters);
                tokenRequestMessage.Method = HttpMethod.Post;
                tokenRequestMessage.RequestUri = new Uri(_lineLoginConfig.AccessTokenApi);
                tokenRequestMessage.Content.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded");

                var tokenResponse = client.SendAsync(tokenRequestMessage).Result;

                var tokenResponseContent = tokenResponse.Content.ReadAsStringAsync().Result;
                SentrySdk.CaptureMessage($"tokenResponseContent {tokenResponseContent}");
                _logger.LogInformation($"GetLineUserId LineTokenApi Response StatusCode = {(int)tokenResponse.StatusCode}-{tokenResponse.StatusCode}.\n"
                    + $"Content:{tokenResponseContent}");

                var tokenResponseObj = JsonConvert.DeserializeObject<IssueAccessTokenResponseModel>(tokenResponseContent);

                if (tokenResponseObj == null || string.IsNullOrWhiteSpace(tokenResponseObj.access_token))
                {
                    resultCode = LineLoginErrorCode.LineTokenNotFound;
                    _logger.LogInformation($"GetLineUserId LineTokenApi Fail：token not found.");
                    return false;
                }

                var profileRequestMessage = new HttpRequestMessage();
                profileRequestMessage.Method = HttpMethod.Get;
                profileRequestMessage.RequestUri = new Uri(_lineLoginConfig.ProfileApi);
                profileRequestMessage.Headers.TryAddWithoutValidation("Authorization", "Bearer " + tokenResponseObj.access_token);

                var profileResponse = client.SendAsync(profileRequestMessage).Result;

                var profileResponseContent = profileResponse.Content.ReadAsStringAsync().Result;
                SentrySdk.CaptureMessage($"profileResponseContent {profileResponseContent}");
                _logger.LogInformation($"GetLineUserId LineProfileApi  Response StatusCode = {(int)profileResponse.StatusCode}-{profileResponse.StatusCode}.\n"
                    + $"Content:{profileResponseContent}");

                var profileObj = JsonConvert.DeserializeObject<LineProfileResponseModel>(profileResponseContent);

                if (profileObj == null || string.IsNullOrWhiteSpace(profileObj.userId))
                {
                    resultCode = LineLoginErrorCode.LineUserIdNotFound;
                    _logger.LogInformation($"GetLineUserId LineProfileApi Fail：userId not found.");
                    return false;
                }

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