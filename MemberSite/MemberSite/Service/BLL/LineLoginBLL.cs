using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using MemberSite.Service.Model.Line;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace MemberSite.Service.BLL
{
    public class LineLoginBLL
    {
        private readonly Tools _tools;
        private readonly ServerInfoBll _serverInfoBll;
        private readonly CustInfoBll _custInfoBll;
        private readonly CustInfoService _custInfoService;
        private readonly AuthenticateService _authenticateService;
        private readonly AuthService _authService;
        private readonly AuthorizeBLL _authorizeBll;
        private readonly SelLangBLL _selLangBll;
        private readonly IOptions<LineLoginChannelConfig> _lineLoginChannelConfig;
        private readonly ILogger<LineLoginBLL> _logger;

        #region Constructor

        public LineLoginBLL(IServiceProvider provider)
        {
            _tools = provider.GetRequiredService<Tools>();
            _authorizeBll = provider.GetRequiredService<AuthorizeBLL>();
            _serverInfoBll = provider.GetRequiredService<ServerInfoBll>();
            _custInfoBll = provider.GetRequiredService<CustInfoBll>();
            _custInfoService = provider.GetRequiredService<CustInfoService>();
            _authenticateService = provider.GetRequiredService<AuthenticateService>();
            _authService = provider.GetRequiredService<AuthService>();
            _logger = provider.GetRequiredService<ILogger<LineLoginBLL>>();
            _selLangBll = provider.GetRequiredService<SelLangBLL>();
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
                        var custLineProfile = GetCustLinePrfile(authData.CustId, null);
                        if (custLineProfile != null)
                        {
                            errorCode = (int)LineLoginErrorCode.MemberAccountAlreadyBinding;
                            _logger.LogError($"IssueLineToken Error：Binding Flow Error: Already Binding. Can't Bind Again. authData={JsonConvert.SerializeObject(authData)}");
                            return false;
                        }
                    }
                }

                if (!_authService.IssueLineToken((int)type, token))
                {
                    errorCode = (int)LineLoginErrorCode.IssueLineTokenFail;
                    _logger.LogError($"IssueLineToken Fail.");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                errorCode = (int)LineLoginErrorCode.IssueLineTokenException;
                _logger.LogError($"IssueLineToken Fail: Exception happend. Message={ex.Message}");
                return false;
            }
        }

        public async Task<CallBackReponseModel> LineLoginCallBack(string code, string state, string error, int errorCode, string error_description, string errorMessage)
        {
            var lineUserId = string.Empty;
            var config = _lineLoginChannelConfig.Value;
            var resultCode = LineLoginErrorCode.Success;

            if (!CheckLineResponse(code, state, error, errorCode, error_description, errorMessage, out resultCode))
            {
                return new CallBackReponseModel { Type = FlowType.Undefine, ErrorCode = resultCode };
            }

            var lineIssueToken = await _authService.GetLineToken(state);

            if (!CheckToken(config, lineIssueToken, out resultCode))
            {
                return new CallBackReponseModel { Type = lineIssueToken.FlowType, ErrorCode = resultCode };
            }

            if (!GetLineUserId(config, code, out lineUserId, out resultCode))
            {
                return new CallBackReponseModel { Type = lineIssueToken.FlowType, ErrorCode = resultCode };
            }

            try
            {
                var authData = _authorizeBll.GetAuthData();

                var custLineProfile = GetCustLinePrfile(0, lineUserId);

                switch (lineIssueToken.FlowType)
                {
                    case FlowType.Register:
                        {
                            _logger.LogInformation("This is Register Flow.");
                            if (authData != null && authData.CustId != 0)
                            {
                                resultCode = LineLoginErrorCode.RegisterAuthInvaild;
                                _logger.LogError("Register Flow Error: This is Register Token But authData is not null and CustId != 0.");
                                return new CallBackReponseModel { Type = lineIssueToken.FlowType, ErrorCode = resultCode };
                            }
                            if (custLineProfile != null)
                            {
                                resultCode = LineLoginErrorCode.RegisterLineProfileInvaild;
                                _logger.LogError("Register Flow Error: This is Register Token But custLineProfile is not null.");
                                return new CallBackReponseModel { Type = lineIssueToken.FlowType, ErrorCode = resultCode };
                            }
                            return new CallBackReponseModel { Type = lineIssueToken.FlowType, ErrorCode = resultCode, UserId = lineUserId };
                        }
                    case FlowType.Login:
                        {
                            _logger.LogInformation("This is Login Flow.");
                            if (custLineProfile == null)
                            {
                                resultCode = LineLoginErrorCode.LoginLineProfileInvaild;
                                _logger.LogError("Login Flow Error: This is Login Token But custLineProfile is null.");
                                return new CallBackReponseModel { Type = lineIssueToken.FlowType, ErrorCode = resultCode };
                            }
                            return new CallBackReponseModel { Type = lineIssueToken.FlowType, ErrorCode = resultCode, CustId = custLineProfile.CustId };
                        }
                    case FlowType.Binding:
                        {
                            _logger.LogInformation($"This is Binding Flow. authData={JsonConvert.SerializeObject(authData)}");
                            if (authData == null || authData.CustId == 0)
                            {
                                resultCode = LineLoginErrorCode.BindingAuthInvaild;
                                _logger.LogError("Binding Flow Error: This is Binding Token But authData is null and CustId = 0.");
                                return new CallBackReponseModel { Type = lineIssueToken.FlowType, ErrorCode = resultCode };
                            }
                            if (custLineProfile != null)
                            {
                                resultCode = LineLoginErrorCode.BindingLineProfileInvaild;
                                _logger.LogError("Binding Flow Error: This is Binding Token But custLineProfile already exist.");
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
                            _logger.LogInformation("Flow Error: Can't define which flow it is.");
                            return new CallBackReponseModel { Type = lineIssueToken.FlowType, ErrorCode = resultCode };
                        }
                }
            }
            catch (Exception ex)
            {
                resultCode = LineLoginErrorCode.CallBackException;
                _logger.LogError($"LineLoginCallBack Fail：Exception happen.message={ex.Message}.");
                return new CallBackReponseModel { Type = lineIssueToken.FlowType, ErrorCode = resultCode };
            }
        }

        public CustLineProfileModel GetCustLinePrfile(int custId, string encryptUserId)
        {
            return _custInfoService.GetCustLineProfile(custId, encryptUserId);
        }

        public bool InsertCustLineProfile(int custId, string lineUserId, out LineLoginErrorCode resultCode)
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
                _logger.LogInformation($"InsertCustLineProfile Fai: Exception happend. message={ex.Message}.");
                return false;
            }
        }

        private bool CheckLineResponse(string code, string state, string error, int errorCode, string error_description, string errorMessage, out LineLoginErrorCode resultCode)
        {
            resultCode = 0;
            if (!string.IsNullOrWhiteSpace(error))
            {
                //message = "Line Error Happened. Please Cantanct to CSD.";
                resultCode = LineLoginErrorCode.LineCallbackError;
                _logger.LogError($"CheckLineResponse Fail：error hapeen. error={error}.errorCode={errorCode}. error_description={error_description}. errormessage={errorMessage}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(code))
            {
                //用戶在Line驗證畫面選擇取消，應該不算錯誤？使用者選擇不綁了
                //message = "Cancel And Back To Pervious.";
                resultCode = LineLoginErrorCode.CodeInvaild;
                _logger.LogError("CheckLineResponse Fail：code is null or empty.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(state))
            {
                resultCode = LineLoginErrorCode.StateInvaid;
                _logger.LogError("CheckLineResponse Fail：state (token) is null or empty.");
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
                _logger.LogError("CheckToken Fail：not found this token.");
                return false;
            }

            if (!Enum.IsDefined(typeof(FlowType), data.FlowType) || data.FlowType == FlowType.Undefine)
            {
                resultCode = LineLoginErrorCode.FlowTypeInvaild;
                _logger.LogError($"CheckToken Fail：Flow Type Is Invalid. FlowType={data.FlowType.ToString()}");
                return false;
            }


            if (data.CreateTime <= DateTime.UtcNow.AddSeconds(0 - config.ExpiredSeconds))
            {
                resultCode = LineLoginErrorCode.TokenExpired;
                _logger.LogError($"CheckToken Fail：LineIssueToken Expired. have to in { data.CreateTime.AddSeconds(config.ExpiredSeconds)}.\r\n" +
                                 $"Token Generate Time ={data.CreateTime}，ExpiredSeconds = {config.ExpiredSeconds}.");
                return false;
            }

            return true;
        }

        private bool GetLineUserId(LineLoginChannelConfig config, string code, out string lineUserId, out LineLoginErrorCode resultCode)
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
                string result = string.Empty;
                NameValueCollection nvc = new NameValueCollection();

                _logger.LogInformation($"Config={JsonConvert.SerializeObject(config)}");

                #region OAuth (Get Access Token)

                nvc.Add("grant_type", "authorization_code");
                nvc.Add("code", code);
                nvc.Add("redirect_uri", config.RedirectUri);
                nvc.Add("client_id", config.ChannelId);
                nvc.Add("client_secret", config.ChannelSecret);

                string tokenResponseStr = string.Empty;

                try
                {
                    tokenResponseStr = Encoding.UTF8.GetString(wc.UploadValues(ApiUrl_Token, "POST", nvc));
                }
                catch (WebException ex)
                {
                    resultCode = LineLoginErrorCode.GetLineTokenFail;
                    using (HttpWebResponse hr = (HttpWebResponse)ex.Response)
                    {
                        int statusCode = (int)hr.StatusCode;
                        StringBuilder sb = new StringBuilder();
                        StreamReader sr = new StreamReader(hr.GetResponseStream(), Encoding.UTF8);
                        sb.Append(sr.ReadToEnd());
                        _logger.LogError($"AccessLineToken Fail: StatusCode:{statusCode},Content:{sb}");
                    }
                    return false;
                }

                if (string.IsNullOrWhiteSpace(tokenResponseStr))
                {
                    resultCode = LineLoginErrorCode.LineTokenInvaild;
                    _logger.LogError($"GetLineUserId Fail：token not found.");
                    return false;
                }

                IssueAccessTokenResponseModel tokenResponse = JsonConvert.DeserializeObject<IssueAccessTokenResponseModel>(tokenResponseStr);
                wc.Headers.Clear();

                #endregion

                //取回User Profile
                wc.Headers.Add("Authorization", "Bearer " + tokenResponse.access_token);

                var userProfileResponseStr = string.Empty;

                try
                {
                    userProfileResponseStr = wc.DownloadString(ApiUrl_Profile);
                }
                catch (WebException ex)
                {
                    resultCode = LineLoginErrorCode.GetLineUserProfileFail;
                    using (HttpWebResponse hr = (HttpWebResponse)ex.Response)
                    {
                        int statusCode = (int)hr.StatusCode;
                        StringBuilder sb = new StringBuilder();
                        StreamReader sr = new StreamReader(hr.GetResponseStream(), Encoding.UTF8);
                        sb.Append(sr.ReadToEnd());
                        _logger.LogError($"AccessLineToken Fail: StatusCode:{statusCode},Content:{sb}");
                    }
                    return false;
                }

                if (string.IsNullOrWhiteSpace(userProfileResponseStr))
                {
                    resultCode = LineLoginErrorCode.LineUserProfileInvaild;
                    _logger.LogError($"GetLineUserId Fail：UserProfile is null.");
                    return false;
                }

                var ProfileObj = JsonConvert.DeserializeObject<LineProfileResponseModel>(userProfileResponseStr);

                if (string.IsNullOrWhiteSpace(ProfileObj.userId))
                {
                    resultCode = LineLoginErrorCode.LineUserIdNotFound;
                    _logger.LogError($"GetLineUserId Fail：userId not found.");
                    return false;
                }

                lineUserId = ProfileObj.userId;

                return true;
            }
            catch (Exception ex)
            {
                resultCode = LineLoginErrorCode.LineGetUserIdException;
                _logger.LogError($"GetLineUserId Fail：Exception happen.message={ex.Message}.");
                return false;
            }
        }

    }
}
