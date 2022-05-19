using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.Line;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

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
        private readonly string _channelId;
        private readonly string _channelSecret;
        private readonly string _redirectUri;
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
            _channelId = "1656472592"; //之後改成用config設定
            _channelSecret = "0ffb666498ba7ec9780384f779119ac0"; //之後改成用config設定
            _redirectUri = "http://www.n88dev.com/callback";//之後改成用config設定
        }

        #endregion

        public bool IssueLineToken(FlowType type, string paramString, out string token)
        {
            token = Guid.NewGuid().ToString();
            try
            {
                if (!_authService.IssueLineToken((int)type, paramString, token)) return false;
                return true;
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
                return false;
            }
        }

        public async Task<CallBackReponseModel> LineLoginCallBack(string code, string state, string error, int errorCode, string error_description, string errorMessage)
        {
            var message = string.Empty;
            var lineUserId = string.Empty;

            if (!CheckLineResponse(code, state, error, errorCode, error_description, errorMessage, out message))
            {
                return new CallBackReponseModel { Type = FlowType.Undefine, ErrorCode = -1, Message = message };
            }

            var lineIssueToken = await _authService.GetLineToken(state);

            if (!CheckToken(lineIssueToken, out message))
            {
                return new CallBackReponseModel { Type = lineIssueToken.FlowType, ErrorCode = -1, Message = message };
            }

            if (!GetLineUserId(code, out lineUserId, out message))
            {
                return new CallBackReponseModel { ErrorCode = -2, Message = message };
            }

            var encryptUserId = _tools.MD5Encrypt(lineUserId);

            var authData = _authorizeBll.GetAuthData();

            var custLineProfile = await GetCustLinePrfile(0, encryptUserId);

            switch (lineIssueToken.FlowType)
            {
                case FlowType.Register:
                    {
                        _logger.LogInformation("This is Register Flow.");
                        if (authData != null && authData.CustId != 0)
                        {
                            message = "flow error. please back to try again.";
                            _logger.LogError("Register Flow Error: This is Register Token But authData is not null and CustId != 0.");
                            return new CallBackReponseModel { Type = lineIssueToken.FlowType, ErrorCode = -1, Message = message };
                        }
                        if (custLineProfile != null)
                        {
                            message = "flow error. please back to try again.";
                            _logger.LogError("Register Flow Error: This is Register Token But custLineProfile is not null.");
                            return new CallBackReponseModel { Type = lineIssueToken.FlowType, ErrorCode = -1, Message = message };
                        }
                        return new CallBackReponseModel { Type = lineIssueToken.FlowType, ErrorCode = 0, UserId = encryptUserId };
                    }
                case FlowType.Login:
                    {
                        if (custLineProfile == null)
                        {
                            message = "haven't binding Line. please back to try again.";
                            _logger.LogError("Login Flow Error: This is Login Token But custLineProfile is null.");
                            return new CallBackReponseModel { Type = lineIssueToken.FlowType, ErrorCode = -1, Message = message };
                        }
                        return new CallBackReponseModel { Type = lineIssueToken.FlowType, ErrorCode = 0, CustId = custLineProfile.CustId, LoginInfo = lineIssueToken.Info };
                    }
                case FlowType.Binding:
                    {
                        _logger.LogInformation("This is Binding Flow.");
                        if (authData == null || authData.CustId == 0)
                        {
                            message = "flow error. please back to try again.";
                            _logger.LogError("Register Flow Error: This is Binding Token But authData is null and CustId = 0.");
                            return new CallBackReponseModel { Type = lineIssueToken.FlowType, ErrorCode = -1, Message = message };
                        }
                        if (custLineProfile != null)
                        {
                            message = "already exist line binding.";
                            _logger.LogError("Binding Flow Error: This is Binding Token But custLineProfile already exist.");
                            return new CallBackReponseModel { Type = lineIssueToken.FlowType, ErrorCode = -1, Message = message };
                        }
                        if (!InsertCustLineProfile(authData.CustId, encryptUserId, out message))
                        {
                            return new CallBackReponseModel { Type = lineIssueToken.FlowType, ErrorCode = -1, Message = message };
                        }
                        return new CallBackReponseModel { Type = lineIssueToken.FlowType, ErrorCode = 0 };
                    }
                default:
                    {
                        message = "flow error. please back to try again.";
                        _logger.LogInformation("Flow Error: Can't define which flow it is.");
                        return new CallBackReponseModel { Type = lineIssueToken.FlowType, ErrorCode = -1, Message = message };
                    }
            }
        }

        public async Task<CustLineProfileModel> GetCustLinePrfile(int custId, string encryptUserId)
        {
            return await _custInfoService.GetCustLineProfile(custId, encryptUserId);
        }

        public bool InsertCustLineProfile(int custId, string lineUserId, out string message)
        {
            message = string.Empty;
            try
            {
                if (!_custInfoService.InsertCustLineProfile(custId, lineUserId))
                {
                    message = "error happen. please contact to CSD.";
                    _logger.LogInformation("InsertCustLineProfile Fail.");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                message = "error happen. please contact to CSD.";
                _logger.LogInformation($"InsertCustLineProfile Fai: Exception happend. message={ex.Message}.");
                return false;
            }
        }

        private bool CheckLineResponse(string code, string state, string error, int errorCode, string error_description, string errormessage, out string message)
        {
            message = string.Empty;

            if (!string.IsNullOrWhiteSpace(error))
            {
                message = "Line Error Happened. Please Cantanct to CSD.";
                _logger.LogError($"CheckLineResponse Fail：error hapeen. error={error}.errorCode={errorCode}. error_description={error_description}. errormessage={errormessage}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(code))
            {
                //用戶在Line驗證畫面選擇取消，應該不算錯誤？使用者選擇不綁了
                message = "Cancel And Back To Pervious.";
                _logger.LogError("CheckLineResponse Fail：code is null or empty.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(state))
            {
                message = "Somthing Wrong.Please Back To Try Again.";
                _logger.LogError("CheckLineResponse Fail：state (token) is null or empty.");
                return false;
            }

            return true;
        }

        private bool CheckToken(LineIssueTokenModel data, out string message)
        {
            message = string.Empty;

            if (data == null)
            {
                message = "Somthing Wrong. Please Back To Try Again.";
                _logger.LogError("CheckToken Fail：not found this token.");
                return false;
            }

            //TODO:檢查Enum 是否有效
            //if (Enum.IsDefined(FlowType, data.FlowType))
            //{
            //    // Do something
            //}

            if (data.FlowType == FlowType.Login && string.IsNullOrWhiteSpace(data.Info))
            {
                message = "Somthing Wrong. Please Back To Try Again.";
                _logger.LogError("CheckToken Fail：not found this login token's info.");
                return false;
            }

            //TODO:Token 時效性???
            if (data.CreateTime <= DateTime.UtcNow.AddMinutes(0 - 10))
            {
                message = "Expired. Please Back To Try Again.";
                _logger.LogError($"CheckToken Fail：Token要求時間超出有效時間{ data.CreateTime.AddMinutes(10)}.\r\n" +
                                 $"Token Generate Time ={data.CreateTime}，有效時間為 10 分鐘以內。");
                return false;
            }

            return true;
        }

        private bool GetLineUserId(string code, out string lineUserId, out string message)
        {
            message = string.Empty;
            WebClient wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            wc.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
            string result = string.Empty;
            NameValueCollection nvc = new NameValueCollection();
            lineUserId = string.Empty;
            try
            {
                #region OAuth (Get Access Token)

                string ApiUrl_Token = "https://api.line.me/oauth2/v2.1/token";
                nvc.Add("grant_type", "authorization_code");
                nvc.Add("code", code);
                nvc.Add("redirect_uri", _redirectUri);
                nvc.Add("client_id", _channelId);
                nvc.Add("client_secret", _channelSecret);
                string JsonStr = Encoding.UTF8.GetString(wc.UploadValues(ApiUrl_Token, "POST", nvc));
                IssueAccessTokenResponseModel ToKenObj = JsonConvert.DeserializeObject<IssueAccessTokenResponseModel>(JsonStr);
                wc.Headers.Clear();

                #endregion

                //取回User Profile
                string ApiUrl_Profile = "https://api.line.me/v2/profile";
                wc.Headers.Add("Authorization", "Bearer " + ToKenObj.access_token);
                string UserProfile = wc.DownloadString(ApiUrl_Profile);
                var ProfileObj = JsonConvert.DeserializeObject<LineProfileResponseModel>(UserProfile);

                if (string.IsNullOrWhiteSpace(ProfileObj.userId))
                {
                    message = "Line Error Happend.";
                    _logger.LogError($"GetLineUserId Fail：userId not found.");
                    return false;
                }

                lineUserId = ProfileObj.userId;

                return true;
            }
            catch (Exception ex)
            {
                message = "Exception happend.";
                _logger.LogError($"GetLineUserId Fail：Exception happen.message={ex.Message}.");
                return false;
            }
        }

    }
}
