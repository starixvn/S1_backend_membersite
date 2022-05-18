using MemberSite.Service.BLL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.Line;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Web;
using MemberSite.Service.Model.enums;

namespace MemberSite.Controllers.Api
{
    [AllowAnonymous]
    [Route("api/[controller]/[action]")]
    [Route("api/[controller]/[action]/{LangId}")]
    [ApiController]
    public class LineLoginController : ControllerBase
    {
        private readonly string _channelId;
        private readonly string _redirectUri;
        private readonly Tools _tools;
        private readonly LineLoginBLL _lineLoginBll;
        private readonly AuthorizeBLL _authorizeBll;
        private readonly SelLangBLL _selLangBll;
        private readonly CustInfoBll _custInfoBll;
        private readonly FPSBLL _sFpsBll;
        private readonly ILogger<LineLoginController> _logger;

        #region Constructor

        public LineLoginController(IServiceProvider provider)
        {
            _tools = provider.GetRequiredService<Tools>();
            _lineLoginBll = provider.GetRequiredService<LineLoginBLL>();
            _authorizeBll = provider.GetRequiredService<AuthorizeBLL>();
            _selLangBll = provider.GetRequiredService<SelLangBLL>();
            _custInfoBll = provider.GetRequiredService<CustInfoBll>();
            _sFpsBll = provider.GetRequiredService<FPSBLL>();
            _logger = provider.GetRequiredService<ILogger<LineLoginController>>();
            _channelId = "1656472592"; //之後改成用config設定
            _redirectUri = "http://www.n88dev.com/api/LineLogin/callback";//之後改成用config設定
        }

        #endregion

        #region Api Model

        public class LineLoginModel
        {
            public FlowType type { get; set; }
            public bool isPersistent { get; set; }
            public string url { get; set; }
            public string platform { get; set; }
            public string userAgent { get; set; }
            public string screen { get; set; }
            public string di { get; set; }
        }

        #endregion

        #region Action

        [HttpPost]
        public async Task<ApiResult> LineLoginDirectAsync(LineLoginModel request)
        {
            #region API Example & Description
            //local run 之後，要在brower 上輸入：http://localhost:5000/api/LineLogin/LineLoginDirect/en?type=1

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

            string response_type = "code";
            string client_id = _channelId;
            string redirect_uri = HttpUtility.UrlEncode(_redirectUri);
            string state = string.Empty;

            #region TODO: 搬BLL

            if (request.type == FlowType.Binding)
            {
                //如果走綁定流程，先檢查是否已經綁定，已綁定則不可再次載綁定
                var authData = _authorizeBll.GetAuthData();
                if (authData != null && authData.CustId != 0)
                {
                    var custLineProfile = await _lineLoginBll.GetCustLinePrfile(authData.CustId, null);
                    if (custLineProfile != null)
                        return new ApiResult() { ErrorCode = -1, Message = "Already Binding!" };
                }
            }

            //TODO: 如果走登入流程，要檢查參數是否有效

            var storeLoginData = new LineLoginDataModel()
            {
                Type = request.type,
                IsPersistent = request.isPersistent,
                Url = request.url,
                Platform = request.platform,
                RequestHeaderUserAgent = Request.Headers["User-Agent"],
                UserAgent = request.userAgent,
                Screen = request.screen,
                Di = request.di
            };

            var storeDataString = JsonConvert.SerializeObject(storeLoginData);

            if (!_lineLoginBll.IssueLineToken(request.type, storeDataString, out string token))
            {
                return new ApiResult() { ErrorCode = -1, Message = "Issue Line Token Error!" };
            }
            state = token;

            #endregion

            string LineLoginUrl = string.Format("https://access.line.me/oauth2/v2.1/authorize?" +
                "response_type={0}"
                + "&client_id={1}"
                + "&redirect_uri={2}"
                + "&state={3}"
                + "&scope=profile%20openid",
                response_type,
                client_id,
                redirect_uri,
                state
            );
            return new ApiResult<string>() { Data = LineLoginUrl, ErrorCode = 0 };
        }

        [HttpGet]
        public async Task<ApiResult> callbackAsync(string code, string state, string error, string error_description)
        {
            var apiResult = new ApiResult(_selLangBll);
            var result = await _lineLoginBll.LineLoginCallBack(code, state, error, error_description);

            if (result.ErrorCode != 0) //Fail
            {
                switch (result.Type)
                {
                    case FlowType.Register:
                        return new ApiResult<dynamic>()
                        {
                            Data = new { Type = result.Type, UserId = result.UserId },
                            ErrorCode = result.ErrorCode,
                            Message = result.Message
                        };
                    case FlowType.Binding:
                    case FlowType.Login:
                        return new ApiResult<dynamic>()
                        {
                            Data = new { Type = result.Type },
                            ErrorCode = result.ErrorCode,
                            Message = result.Message
                        };
                    default:
                        return new ApiResult<dynamic>()
                        {
                            Data = new { Type = result.Type },
                            ErrorCode = result.ErrorCode,
                            Message = result.Message
                        };
                }
            }

            switch (result.Type)
            {
                case FlowType.Register:
                    return new ApiResult<dynamic>()
                    {
                        Data = new { Type = result.Type, UserId = result.UserId },
                        ErrorCode = 0
                    };
                case FlowType.Binding:
                    return new ApiResult<dynamic>()
                    {
                        Data = new { Type = result.Type },
                        ErrorCode = 0
                    };
                case FlowType.Login:
                    {
                        return await LineLoginFlowAsync(result.CustId, result.LoginInfo);
                    }
                default:
                    return new ApiResult<dynamic>()
                    {
                        Data = new { Type = result.Type },
                        ErrorCode = -1,
                        Message = "Flow Type Error."
                    };
            }
        }

        #endregion

        public async Task<ApiResult> LineLoginFlowAsync(int custId, string loginInfo)
        {
            var apiResult = new ApiResult<dynamic>(_selLangBll);

            var desktopClaims = new List<Claim>(); //Initial Destop & H5 Claims
            var appClaims = new Claim[0]; //Initial App Claims

            var info = JsonConvert.DeserializeObject<LineLoginDataModel>(loginInfo);
            Customer customer = null;
            var ip = _tools.GetClientIP();
            var fromUrl = _tools.GetFromUrl();
            var deviceType = (int)_tools.GetDeviceId(fromUrl);
            var userLoginResult = _custInfoBll.UserLogin(true, custId, null, null, ip, fromUrl,
                info.RequestHeaderUserAgent, deviceType, ref customer, out var token);

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

            if (info.Di != null)
            {
                _ = Task.Run(async () =>
                {
                    await _sFpsBll.FPSChecksAsync(customer.CustId, "login", "Successfully", info.Url, info.Platform, info.UserAgent, info.Screen, info.Di, ip);
                });
            }

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

                Response.Cookies.Delete($"Initialization{User.Identity.Name}");
                var claimsIdentity = new ClaimsIdentity(desktopClaims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

                apiResult.Message = Url.RouteUrl("desktop", new { action = "Index", controller = "Home", LangId = _selLangBll.GetLangId() });
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
