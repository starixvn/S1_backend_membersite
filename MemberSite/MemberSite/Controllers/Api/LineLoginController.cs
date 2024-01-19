using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Web;
using MemberSite.Service.BLL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using MemberSite.Service.Model.Line;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MemberSite.Controllers.Api
{
    [AllowAnonymous]
    [Route("api/[controller]/[action]")]
    [Route("api/[controller]/[action]/{LangId}")]
    [ApiController]
    public class LineLoginController : ControllerBase
    {
        private readonly ServerInfoBll _serverInfoBll;
        private readonly Tools _tools;
        private readonly LineLoginBLL _lineLoginBll;
        private readonly SelLangBLL _selLangBll;
        private readonly CustInfoBll _custInfoBll;
        private readonly FPSBLL _sFpsBll;
        private readonly LineLoginChannelConfig _lineLoginChannelConfig;
        private readonly ILogger<LineLoginController> _logger;

        #region Constructor

        public LineLoginController(IServiceProvider provider)
        {
            _tools = provider.GetRequiredService<Tools>();
            _lineLoginBll = provider.GetRequiredService<LineLoginBLL>();
            _selLangBll = provider.GetRequiredService<SelLangBLL>();
            _custInfoBll = provider.GetRequiredService<CustInfoBll>();
            _sFpsBll = provider.GetRequiredService<FPSBLL>();
            _logger = provider.GetRequiredService<ILogger<LineLoginController>>();
            var configBll = provider.GetRequiredService<ConfigBLL>();
            _lineLoginChannelConfig = configBll.GetConfigValueByEnvironment<LineLoginChannelConfig>("LineLoginChannelConfig");
        }

        #endregion

        #region Api Model

        public class LineLoginModel
        {
            public FlowType type { get; set; }
        }

        public class LineResponseModel
        {
            public string code { get; set; }
            public string state { get; set; }
            public string error { get; set; }
            public int errorCode { get; set; }
            public string error_description { get; set; }
            public string errorMessage { get; set; }

        }

        public class LoginModel
        {
            public bool isPersistent { get; set; }
            public string url { get; set; }
            public string platform { get; set; }
            public string userAgent { get; set; }
            public string screen { get; set; }
            public string di { get; set; }

        }

        public class LineCallbackModel
        {
            public LineResponseModel LineLoginResponse { get; set; }
            public LoginModel LoginData { get; set; }

        }

        #endregion

        #region Action

        [HttpPost]
        public ApiResult LineLoginDirect(LineLoginModel request)
        {
            #region API Example & Description

            //Reference=https://developers.line.biz/zh-hant/docs/line-login/integrate-line-login/#making-an-authorization-request
            //Example=https://access.line.me/oauth2/v2.1/authorize?response_type=code&client_id=1234567890&redirect_uri=https%3A%2F%2Fexample.com%2Fauth&state=12345abcde&scope=profile%20openid&nonce=09876xyz

            //Url= https://access.line.me/oauth2/v2.1/authorize

            //Parameter
            //response_type          (String,必須)：code(10分鐘內有效)
            //client_id              (String, 必須)：LINE Login Channel 的 Channel ID。您可以在LINE Developers console中查看。
            //redirect_uri           (String, 必須)：在 LINE Developers Console 中註冊的 Callback URL。
            //state                  (String, 必須)：用於防止跨站請求偽造(opens new window)的唯一性屬性。 請在 Web app 請求中，針對每個登入階段隨機生成。注意，不能使用 URL 編碼過後的文字。
            //scope                  (String, 必須)：向用戶請求授予權限。詳細內容請參閱「Scopes」。
            //nonce                  (String, 選填)：用於防止Replay attack(opens new window)的文字。此值會在 Response 的 ID Token中。
            //prompt                 (String, 選填)：consent。即使已授予用戶所有請求的權限，仍然強制顯示同意畫面。
            //max_age                (Number, 選填)：用戶認證後，允許的最長經過時間（以秒為單位）。相當於在OpenID Connect Core 1.0(opens new window)的「Authentication Request」區段中，所定義的max_age屬性。
            //ui_locales             (String, 選填)：在 LINE Login 中，所顯示畫面的顯示語言和文字類型。按照優先度從高到低的順序，在用空格分隔的列表中，設定在RFC 5646（BCP 47） (opens new window)中定義的語言標籤。相當於在OpenID Connect Core 1.0(opens new window)的「Authentication Request」區段中，所定義的ui_locales屬性。
            //bot_prompt             (String, 選填)：用戶登入時，會顯示將 LINE 官方帳號加入好友的選項。指定normal或aggressive。詳細內容請參閱「在登入時將 LINE 官方帳號加為好友(bot link)」。
            //initial_amr_display    (String, 選填)：若設定為 lineqr 將會顯示 QR code 登入 來取代預設的 email 登入。
            //switch_amr             (Boolean, 選填)：預設值為 true。若設定為 false，則會隱藏用於更改登錄方式的按鈕，例如 "透過 Email 登入" 或 " QR Code 登入" 的選項。
            //disable_ios_auto_login (Boolean, 選填)：預設為 false，若設定為 true，將會停止在 iOS 上的自動登入機制。

            #endregion

            var config = _lineLoginChannelConfig;

            if (!_lineLoginBll.IssueLineToken(request.type, out var errorCode, out var token))
            {
                return new ApiResult(_selLangBll) { ErrorCode = errorCode, Message = _selLangBll.GetMsg("msg_ApiServerError") };
            }

            var domainUrl = _serverInfoBll.GetDomain();
            var redirectUri = $"https://{domainUrl}/callback";

            var lineLoginUrl = string.Format("https://access.line.me/oauth2/v2.1/authorize?" +
                "response_type={0}"
                + "&client_id={1}"
                + "&redirect_uri={2}"
                + "&state={3}"
                + "&scope=profile%20openid",
                "code",
                config.ChannelId,
                HttpUtility.UrlEncode(redirectUri),
                token
            );

            return new ApiResult<string>() { Data = lineLoginUrl, ErrorCode = errorCode };
        }

        [HttpPost]
        public async Task<ApiResult> LineCallbackAsync(LineCallbackModel request)
        {
            var domainUrl = _serverInfoBll.GetDomain();
            var redirectUri = $"https://{domainUrl}/callback";

            var result = await _lineLoginBll.LineLoginCallBack(request.LineLoginResponse.code,
                request.LineLoginResponse.state,
                request.LineLoginResponse.error,
                request.LineLoginResponse.errorCode,
                request.LineLoginResponse.error_description,
                request.LineLoginResponse.errorMessage,
                redirectUri);

            if (result.ErrorCode != LineLoginErrorCode.Success)
            {
                var data = new { Type = result.Type, UserId = result.UserId };
                var message = string.Empty;

                switch (result.ErrorCode)
                {
                    case LineLoginErrorCode.RegisterLineProfileInvaild:
                    case LineLoginErrorCode.BindingLineProfileInvaild:
                        message = _selLangBll.GetMsg("msg_LineAccountAlreadyBind");
                        break;
                    case LineLoginErrorCode.CodeInvaild: //使用者於line認證跳出，選擇不繼續綁定
                        result.ErrorCode = LineLoginErrorCode.Success;
                        break;
                    default:
                        message = _selLangBll.GetMsg("msg_ApiServerError");
                        break;
                }

                return new ApiResult<dynamic>(_selLangBll)
                {
                    Data = data,
                    ErrorCode = (int)result.ErrorCode,
                    Message = message
                };
            }

            switch (result.Type)
            {
                case FlowType.Register:
                    return new ApiResult<dynamic>(_selLangBll)
                    {
                        Data = new { Type = result.Type, UserId = result.UserId },
                        ErrorCode = (int)result.ErrorCode
                    };
                case FlowType.Binding:
                    return new ApiResult<dynamic>(_selLangBll)
                    {
                        Data = new { Type = result.Type },
                        ErrorCode = (int)result.ErrorCode
                    };
                case FlowType.Login:
                    {
                        var loginReuslt = await LineLoginFlowAsync(result.CustId, request.LoginData);

                        return new ApiResult<dynamic>(_selLangBll)
                        {
                            Data = new { Type = FlowType.Login, Data = loginReuslt.Data },
                            ErrorCode = loginReuslt.ErrorCode,
                            Message = loginReuslt.Message,
                            RawErrorCode = loginReuslt.RawErrorCode,
                            RawMessage = loginReuslt.RawMessage
                        };
                    }
                case FlowType.Undefine:
                default:
                    return new ApiResult<dynamic>(_selLangBll)
                    {
                        Data = new { Type = result.Type },
                        ErrorCode = (int)result.ErrorCode,
                        Message = _selLangBll.GetMsg("msg_ApiServerError")
                    };
            }
        }

        #endregion

        public async Task<ApiResult<dynamic>> LineLoginFlowAsync(int custId, LoginModel loginData)
        {
            var apiResult = new ApiResult<dynamic>(_selLangBll);

            var desktopClaims = new List<Claim>(); //Initial Desktop & H5 Claims
            var appClaims = Array.Empty<Claim>(); //Initial App Claims

            Customer customer = null;
            var ip = _tools.GetClientIP();
            var fromUrl = _tools.GetFromUrl();
            var deviceType = (int)_tools.GetDeviceId(fromUrl);
            var userLoginResult = _custInfoBll.UserLogin(true, custId, null, null, ip, fromUrl,
                Request.Headers["User-Agent"], deviceType, ref customer, out var token);

            switch (userLoginResult)
            {
                case -1:
                    apiResult.ErrorCode = 1101;
                    apiResult.Message = _selLangBll.GetMsg("msg_DepositProcessLogin_LoginFailed");
                    break;
                case -2:
                    apiResult.ErrorCode = 1001;
                    apiResult.Message = _selLangBll.GetMsg("msg_DepositProcessLogin_LoginFailed");
                    break;
                case -3:
                    apiResult.ErrorCode = 1102;
                    apiResult.Message = _selLangBll.GetMsg("msg_DepositProcessLogin_ACLocked");
                    break;
                default:
                    {
                        if (userLoginResult != 0)
                        {
                            apiResult.ErrorCode = 9000;
                        }

                        break;
                    }
            }

            if (userLoginResult != 0)
            {
                if (deviceType != (int)DeviceId.APP)
                {
                    //Desktop & H5
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                }
                return apiResult;
            }

            if (deviceType != (int)DeviceId.APP)
            {
                //Desktop & H5
                desktopClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name,customer.UserName ),
                    new Claim(ClaimTypes.NameIdentifier,customer.CustId.ToString() ),
                    new Claim("Token",token ),
                    new Claim("LangId",customer.UserLang )
                };
            }
            else
            {
                //App
                appClaims = new[]
                {
                    new Claim(ClaimTypes.Name,customer.UserName ),
                    new Claim(ClaimTypes.NameIdentifier,customer.CustId.ToString() ),
                    new Claim("Token",token ),
                    new Claim("LangId",customer.UserLang ),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString() )
                };
            }

            // if (loginData.di != null)
            // {
            //     _ = Task.Run(async () =>
            //     {
            //         await _sFpsBll.FPSChecksAsync(customer.CustId, "login", "Successfully", loginData.url, loginData.platform, loginData.userAgent, loginData.screen, loginData.di, ip);
            //     });
            // }

            if (deviceType != (int)DeviceId.APP)
            {
                //Desktop & H5
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = false,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
                };

                if (_tools.IsMobile())
                {
                    authProperties = new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
                    };
                }

                Response.Cookies.Delete($"Initialization{User.Identity?.Name}");
                var claimsIdentity = new ClaimsIdentity(desktopClaims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

                apiResult.Message = Url.RouteUrl("desktop", new { action = Request.RouteValues["action"], controller = Request.RouteValues["controller"], LangId = _selLangBll.GetLangId() });
            }
            else
            {
                //App
                var accessToken = _tools.GenerateJwtTokens(appClaims);
                var data = new JwtAuthData { AccessToken = accessToken };
                apiResult.Data = data;
            }

            apiResult.ErrorCode = 0;
            return apiResult;
        }
    }
}
