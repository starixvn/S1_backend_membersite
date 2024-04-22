using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Sockets;
using System.Security.Claims;
using System.Text.RegularExpressions;

using MemberSite.Attributes;
using MemberSite.Service.BLL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

using Newtonsoft.Json;

using static MemberSite.Extensions.MethodExtension;

namespace MemberSite.Controllers.Api
{
    [AllowAnonymous]
    [Route("api/[controller]/[action]")]
    [Route("api/[controller]/[action]/{LangId}")]
    [ApiController]
    public class SiteController : ControllerBase
    {
        private readonly CustInfoBll _custInfoBll;
        private readonly Tools _tools;
        private readonly SelLangBLL _selLangBll;
        private readonly IWebHostEnvironment _environment;
        private readonly AuthorizeBLL _authorizeBll;
        private readonly CasinoBll _casinoBll;
        private readonly ServerInfoBll _serverInfoBll;
        private readonly BannerBLL _bannerBll;
        private readonly AuthBLL _authBll;
        private readonly Regex _regexUserName = new(@"^[a-zA-Z0-9]{6,18}$");
        private readonly Regex _regexPhoneNo = new(@"^\d+$");
        private readonly ThirdPartyBll _thirdPartyBll;
        private readonly SiteBll _siteBll;
        private readonly ICacheHelper _cacheHelper;
        private readonly RecaptchaBLL _recaptchaBll;
        private readonly ILogger<SiteController> _logger;
        private readonly CheckAttackBLL _checkAttackBll;
        private readonly StringVaildHelper _stringVaildHelper;
        private readonly CheckIpBLL _checkIpBll;
        private readonly FPSBLL _FPSBll;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly CommonBLL _commonBll;
        

        public SiteController(IServiceProvider provider)
        {
            _custInfoBll = provider.GetRequiredService<CustInfoBll>();
            _tools = provider.GetRequiredService<Tools>();
            _selLangBll = provider.GetRequiredService<SelLangBLL>();
            _environment = provider.GetRequiredService<IWebHostEnvironment>();
            _authorizeBll = provider.GetRequiredService<AuthorizeBLL>();
            _casinoBll = provider.GetRequiredService<CasinoBll>();
            _serverInfoBll = provider.GetRequiredService<ServerInfoBll>();
            _bannerBll = provider.GetRequiredService<BannerBLL>();
            _authBll = provider.GetRequiredService<AuthBLL>();
            _thirdPartyBll = provider.GetRequiredService<ThirdPartyBll>();
            _siteBll = provider.GetRequiredService<SiteBll>();
            _cacheHelper = provider.GetRequiredService<ICacheHelper>();
            _recaptchaBll = provider.GetRequiredService<RecaptchaBLL>();
            _logger = provider.GetRequiredService<ILogger<SiteController>>();
            _checkAttackBll = provider.GetRequiredService<CheckAttackBLL>();
            _stringVaildHelper = provider.GetRequiredService<StringVaildHelper>();
            _checkIpBll = provider.GetRequiredService<CheckIpBLL>();
            _checkAttackBll = provider.GetRequiredService<CheckAttackBLL>();
            _stringVaildHelper = provider.GetRequiredService<StringVaildHelper>();
            _FPSBll = provider.GetRequiredService<FPSBLL>();
            _httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
            _commonBll = provider.GetRequiredService<CommonBLL>();
            
        }

        public class LoginModel
        {
            public string UserName { get; set; }
            public string Userpwd { get; set; }
            public bool isPersistent { get; set; }
            public string url { get; set; }
            public string platform { get; set; }
            public string userAgent { get; set; }
            public string screen { get; set; }
            public FPSRequest FPSRequest { get; set; }
        }

        [HttpPost]
        [TypeFilter(typeof(PublicAuthAttribute))]
        public async Task<ApiResult> SignIn(LoginModel loginModel)
        {
            var apiResult = new ApiResult(_selLangBll);
            
            if (!_environment.EnvironmentName.Equals("Local", StringComparison.OrdinalIgnoreCase))
            {
                if (_checkAttackBll.CheckAttackForSignIn(loginModel, true, Request))
                {
                    Request.HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                    return null;
                }
            }

            if (!_regexUserName.IsMatch(loginModel.UserName))
            {
                apiResult.ErrorCode = 1103;
                apiResult.Message = _selLangBll.GetMsg("msg_UsernameValidate");
                return apiResult;
            }

            Customer customer = null;
            var ip = _tools.GetClientIP();
            var fromUrl = _tools.GetFromUrl();
            var deviceType = (int)_tools.GetDeviceId(fromUrl);
            var userLoginResult = _custInfoBll.UserLogin(false, 0, loginModel.UserName, loginModel.Userpwd, ip, fromUrl,
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
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return apiResult;
            }

            var claims = new List<Claim>
                {
                    new(ClaimTypes.Name,customer.UserName ),
                    new(ClaimTypes.NameIdentifier,customer.CustId.ToString() ),
                    new("Token",token ),
                    new("LangId",customer.UserLang )
                };

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

            #region FPS

            if (loginModel.FPSRequest != null)
            {
                var context = _httpContextAccessor.HttpContext;
                _ = Task.Run(async () =>
                {
                    var request = new FPSApiRequest
                    {
                        Account = customer.CustId.ToString(),
                        DI = loginModel.FPSRequest.DI,
                        BI = loginModel.FPSRequest.BI,
                        IP = ip,
                        UserAgent = Request.Headers["User-Agent"],
                        DetecasAnalysis = loginModel.FPSRequest.DetecasAnalysis,
                        AcceptsTypes = context?.Request.Headers["Accept"].ToString(),
                        SessionKey = loginModel.FPSRequest.SessionKey,
                        Token = loginModel.FPSRequest.Token,
                        Url = loginModel.url
                    };
                    await _FPSBll.FPSChecksAsync(request);
                });
            }

            #endregion

            await _cacheHelper.StringFireAndForgetSetAsync(customer.CustId.ToString(), token, TimeSpan.FromMinutes(30), RedisDataBase.Token);

            Response.Cookies.Delete($"Initialization{User.Identity?.Name}");
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
            apiResult.Message = Url.RouteUrl("desktop", new { action = Request.RouteValues["action"], controller = Request.RouteValues["controller"], LangId = _selLangBll.GetLangId() });
            apiResult.ErrorCode = 0;
            return apiResult;
        }

        [HttpPost]
        [TypeFilter(typeof(PublicAuthAttribute))]
        public ApiResult<JwtAuthData> SignInApp(LoginModel loginModel)
        {
            var apiResult = new ApiResult<JwtAuthData>(_selLangBll);
            if (_checkAttackBll.CheckAttackForSignIn(loginModel, false, Request))
            {
                Request.HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                return null;
            }

            if (!_regexUserName.IsMatch(loginModel.UserName))
            {
                apiResult.ErrorCode = -8;
                apiResult.Message = _selLangBll.GetMsg("msg_UsernameValidate");
                return apiResult;
            }

            Customer customer = null;
            var ip = _tools.GetClientIP();
            var fromUrl = _tools.GetFromUrl();
            var deviceType = (int)_tools.GetDeviceId(fromUrl);
            var userLoginResult = _custInfoBll.UserLogin(false, 0, loginModel.UserName, loginModel.Userpwd, ip, fromUrl,
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
                return apiResult;
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, customer.UserName),
                new Claim(ClaimTypes.NameIdentifier, customer.CustId.ToString()),
                new Claim("Token", token),
                new Claim("LangId", customer.UserLang),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            if (loginModel.FPSRequest != null)
            {
                var context = _httpContextAccessor.HttpContext;
                _ = Task.Run(async () =>
                {
                    var request = new FPSApiRequest
                    {
                        Account = customer.CustId.ToString(),
                        DI = loginModel.FPSRequest.DI,
                        BI = loginModel.FPSRequest.BI,
                        IP = ip,
                        UserAgent = Request.Headers["User-Agent"],
                        DetecasAnalysis = loginModel.FPSRequest.DetecasAnalysis,
                        AcceptsTypes = context?.Request.Headers["Accept"].ToString(),
                        SessionKey = loginModel.FPSRequest.SessionKey,
                        Token = loginModel.FPSRequest.Token,
                        Url = loginModel.url
                    };
                    await _FPSBll.FPSChecksAsync(request);
                });
            }

            var accessToken = _tools.GenerateJwtTokens(claims);
            var data = new JwtAuthData
            {
                AccessToken = accessToken
            };
            apiResult.Data = data;
            apiResult.Message = "";
            apiResult.ErrorCode = 0;
            return apiResult;
        }

        public class RegisteredModel
        {
            public string UserName { get; set; }
            public string Userpwd { get; set; }
            public string Email { get; set; } = null;
            public int CurrencyId { get; set; }
            public string UserLang { get; set; }
            public string RefId { get; set; }
            public string AffId { get; set; }
            public string Aff { get; set; }
            public string AffCode { get; set; }
            public string blackbox { get; set; }
            public string LineUserId { get; set; }
            public string ReCaptchaToken { get; set; }
        }

        public class SignupModel : RegisteredModel
        {
            public string SignupUrl { get; set; }
            public string ReferralUrl { get; set; }
            public string PhoneNo { get; set; }
            public string AreaCode { get; set; }
            public string RealName { get; set; }
            public string PromoCode { get; set; }
            public string RedirectUrl { get; set; }
            public int Birth_d { get; set; }
            public int Birth_m { get; set; }
            public int Birth_y { get; set; }
        }
        public class IvorysportSignupModel : SignupModel
        {
            public int Occupation { get; set; }
            public int SourceOfIncome { get; set; }
            public string Nationality { get; set; }
            public string Country { get; set; }
            public string City { get; set; }
            public string PostCode { get; set; }
            public string LineFirst { get; set; }
            public IFormFile PhotoId { get; set; }
        }

        [HttpPost]
        public async Task<ApiResult> SignupAsync(SignupModel signupModel)
        {
            if (_checkAttackBll.CheckAttackForSignup())
            {
                Request.HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                return null;
            }

            var apiResult = new ApiResult(_selLangBll);
            var isAuthenticated = User.Identity is { IsAuthenticated: true };

            if (string.IsNullOrWhiteSpace(signupModel.RealName)
                || !_stringVaildHelper.VaildAccountNameForSetProfile(signupModel.RealName))
            {
                apiResult.ErrorCode = 21;
                apiResult.Message = $"{_selLangBll.GetMsg("msg_RealNameWrongFormat")}";
                return apiResult;
            }

            var customer = _siteBll.Signup(signupModel, apiResult, isAuthenticated, _tools.GetFromUrl(),
                Request.Headers["User-Agent"], true);
            if (apiResult.ErrorCode == (int)ErrorCode.Attack)
            {
                Request.HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                return null;
            }

            if (apiResult.ErrorCode != 0 || customer == null) return apiResult;

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, customer.UserName),
                new(ClaimTypes.NameIdentifier, customer.CustId.ToString()),
                new("Token", customer.Token),
                new("LangId", customer.UserLang)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity));
            await _cacheHelper.StringFireAndForgetSetAsync(customer.CustId.ToString(), customer.Token, TimeSpan.FromMinutes(30), RedisDataBase.Token);
            return apiResult;
        }

        [HttpPost]
        public ApiResult<JwtAuthData> SignupApp(SignupModel signupModel)
        {
            var apiResult = new ApiResult<JwtAuthData>(_selLangBll);
            var isAuthenticated = User.Identity is { IsAuthenticated: true };

            if (string.IsNullOrWhiteSpace(signupModel.RealName)
                || !_stringVaildHelper.VaildAccountNameForSetProfile(signupModel.RealName))
            {
                apiResult.ErrorCode = 21;
                apiResult.Message = $"{_selLangBll.GetMsg("msg_RealNameWrongFormat")}";
                return apiResult;
            }

            var customer = _siteBll.Signup(signupModel, apiResult, isAuthenticated, _tools.GetFromUrl(),
                Request.Headers["User-Agent"], false);

            if (apiResult.ErrorCode != 0 || customer == null) return apiResult;

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, customer.UserName),
                new Claim(ClaimTypes.NameIdentifier, customer.CustId.ToString()),
                new Claim("Token", customer.Token),
                new Claim("LangId", customer.UserLang),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var accessToken = _tools.GenerateJwtTokens(claims);
            var data = new JwtAuthData
            {
                AccessToken = accessToken
            };
            apiResult.Data = data;

            return apiResult;
        }

        private readonly Dictionary<string, System.Drawing.Imaging.ImageFormat> _contentTypesCheck = new Dictionary<string, System.Drawing.Imaging.ImageFormat>(comparer: StringComparer.OrdinalIgnoreCase)
        {
            {"image/png",System.Drawing.Imaging.ImageFormat.Png},
            {"image/jpeg",System.Drawing.Imaging.ImageFormat.Jpeg},
            { "image/gif",System.Drawing.Imaging.ImageFormat.Gif}
        };

        [HttpPost]
        //只給Ivorysport使用
        public async Task<ApiResult> IvorysportSignupAsync([FromForm] IvorysportSignupModel signupModel)
        {
            var apiResult = new ApiResult(_selLangBll);
            var isAuthenticated = User.Identity is { IsAuthenticated: true };
            if (string.IsNullOrWhiteSpace(signupModel.RealName)
                || !_stringVaildHelper.VaildAccountNameForSetProfile(signupModel.RealName))
            {
                apiResult.ErrorCode = 21;
                apiResult.Message = $"{_selLangBll.GetMsg("msg_RealNameWrongFormat")}";
                return apiResult;
            }

            if (!_contentTypesCheck.ContainsKey(signupModel.PhotoId.ContentType))
            {
                return new ApiResult { ErrorCode = -1, Message = _selLangBll.GetMsg("msg_UpdateFailed") };
            }

            var customer = _siteBll.IvorysportSignup(signupModel, apiResult, isAuthenticated, Request.Host.Host,
                Request.Headers["User-Agent"], true);
            if (apiResult.ErrorCode == (int)ErrorCode.Attack)
            {
                Request.HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                return null;
            }

            if (apiResult.ErrorCode != 0 || customer == null) return apiResult;



            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, customer.UserName),
                new(ClaimTypes.NameIdentifier, customer.CustId.ToString()),
                new("Token", customer.Token),
                new("LangId", customer.UserLang)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity));
            await _cacheHelper.StringFireAndForgetSetAsync(customer.CustId.ToString(), customer.Token, TimeSpan.FromMinutes(30), RedisDataBase.Token);
            return apiResult;
        }

        [HttpPost]
        public ApiResult<bool> ExistsUserName(RegisteredModel registeredModel)
        {
            var apiResult = new ApiResult<bool>(_selLangBll);
            if (string.IsNullOrWhiteSpace(registeredModel.UserName) || !_regexUserName.IsMatch(registeredModel.UserName))
            {
                apiResult.Data = true;
                apiResult.ErrorCode = 0;
                apiResult.Message = _selLangBll.GetMsg("msg_UsernameValidate");
                return apiResult;
            }

            apiResult.Data = _custInfoBll.ExistsUserName(registeredModel.UserName);
            apiResult.ErrorCode = 0;
            apiResult.Message = apiResult.Data ? _selLangBll.GetMsg("msg_UsernameNotAvailable") : string.Empty;
            return apiResult;
        }

        public ApiResult<bool> ExistsEmail(RegisteredModel registeredModel)
        {
            var apiResult = new ApiResult<bool>(_selLangBll)
            {
                Data = _custInfoBll.ExistsEmail(0, registeredModel.Email),
                ErrorCode = 0
            };
            return apiResult;
        }

        public class ForgetPwdReq
        {
            public string Email { get; set; }
            public string Username { get; set; }
        }
        public class ForgetPwdReqV2 : ForgetPwdReq
        {
            public int flag { get; set; }/*1: Has LangId*/
        }

        [HttpPost]
        public async Task<ApiResult> ForgetPwdV2Async(ForgetPwdReqV2 forgetPwdReq)
        {
            var apiResult = new ApiResult<bool>(_selLangBll);
            try
            {

                Customer custInfo = null;

                string resetPasswordUrl;
                var host = _serverInfoBll.GetDomain();
                if (forgetPwdReq.flag == 1)
                {
                    resetPasswordUrl = $"https://{host}/{_selLangBll.GetLangId()}/resetPassword";
                }
                else if (forgetPwdReq.flag == 2)
                {
                    if (host == "app.n17881788.com")
                    {
                        resetPasswordUrl = $"https://m.n17881788.com/{_selLangBll.GetLangId()}/resetPassword";
                    }
                    else if (host == "app.bwing.com")
                    {
                        resetPasswordUrl = $"https://m.bwing.com/{_selLangBll.GetLangId()}/resetPassword";
                    }
                    else if (host == "app.goal123.com")
                    {
                        resetPasswordUrl = $"https://m.goal123.com/{_selLangBll.GetLangId()}/resetPassword";
                    }
                    else
                    {
                        //H5
                        resetPasswordUrl = $"https://{host}/resetPassword";
                    }
                }
                else
                {
                    resetPasswordUrl = $"https://{host}/resetPassword";
                }

                if (!string.IsNullOrEmpty(forgetPwdReq.Username))
                {
                    custInfo = _custInfoBll.GetCustInfoByUserName(forgetPwdReq.Username.Trim());
                }
                else
                {
                    custInfo = _custInfoBll.GetCustInfoByMail(forgetPwdReq.Email.Trim());
                }

                if (custInfo == null)
                {
                    apiResult.Message = _selLangBll.GetMsg("ForgetPwdFail");
                }
                else
                {
                    var custInfoProfile = _custInfoBll.GetCustInfoProfile(custInfo.CustId);

                    var pattern = @"(?<=[\w]{1})[\w-\._\+%]*(?=[\w]{1}@)";
                    var result = Regex.Replace(custInfoProfile.Email, pattern, m => new string('*', m.Length));

                    var commonResult = await _custInfoBll.ForgetPwd1Async(custInfo.CustId, resetPasswordUrl);
                    if (commonResult.ErrorCode == 0)
                    {
                        if (!string.IsNullOrEmpty(forgetPwdReq.Username))
                        {
                            apiResult.Message = $"Has send to {result}.";//selLangBLL.GetMsg("ForgetPwdSuccess");
                        }
                        else
                        {
                            apiResult.Message = _selLangBll.GetMsg("ForgetPwdSuccess");
                        }

                        apiResult.ErrorCode = 0;
                    }
                    else
                    {
                        apiResult.ErrorCode = commonResult.ErrorCode;
                        apiResult.Message = commonResult.Message;
                        apiResult.RawErrorCode = commonResult.RawErrorCode;
                        apiResult.RawMessage = commonResult.RawMessage;
                    }
                }
            }
            catch (Exception ex)
            {
                apiResult.RawMessage = ex.Message;
            }

            return apiResult;
        }

        public class ReSetPwdByOTPReq
        {
            public string AreaCode { get; set; }
            public string PhoneNo { get; set; }
            public string Username { get; set; }
        }

        [HttpPost]
        public async Task<ApiResult<SendOTPSMSResult>> ReSetPwdByOTPAsync(ReSetPwdByOTPReq reSetPwdByOtpReq)
        {
            var apiResult = new ApiResult<SendOTPSMSResult>(_selLangBll);

            try
            {
                var checkValue = new RrofileReq
                {
                    AreaCode = reSetPwdByOtpReq.AreaCode,
                    PhoneNo = reSetPwdByOtpReq.PhoneNo
                };

                if (_checkAttackBll.CheckAttack(checkValue, Request))
                {
                    Request.HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                    return null;
                }

                if (_regexPhoneNo.IsMatch(reSetPwdByOtpReq.PhoneNo))
                {
                    var serverInfo = _serverInfoBll.GetServerInfo();
                    
                    if (_custInfoBll.ExistsPhoneNoAndUserName(reSetPwdByOtpReq.Username, reSetPwdByOtpReq.AreaCode,
                            reSetPwdByOtpReq.PhoneNo, serverInfo.SiteId))
                    {
                        var custInfoProfile = _custInfoBll.GetCustInfoProfileByUserName(serverInfo.SiteId, reSetPwdByOtpReq.Username);
                        var otpResponse = await _commonBll.SendOTPSMSAsync(custInfoProfile.CustId, custInfoProfile.CurrencyId);

                        apiResult.ErrorCode = 0;
                        return otpResponse;
                    }

                    // cant find mapping
                    apiResult.ErrorCode = -1;
                    apiResult.Message = _selLangBll.GetMsg("msg_WrongPhoneNumberAndUserName");
                }

                apiResult.ErrorCode = -1;
                return apiResult;
            }
            catch (Exception e)
            {
                _logger.LogError(e, GetCurrentMethod());
                return apiResult;
            }
        }

        public class ReSetPwdVerifyOTPReq
        {
            public string Username { get; set; }
            public string InputOTPcode { set; get; }
            public string NewPassword { set; get; }
        }

        [HttpPost]
        public async Task<ApiResult> ReSetPwdVerifyOTPAsync(ReSetPwdVerifyOTPReq req)
        {
            var serverInfo = _serverInfoBll.GetServerInfo();
            var custInfoProfile = _custInfoBll.GetCustInfoProfileByUserName(serverInfo.SiteId, req.Username);

            var apiResult = await _custInfoBll.ReSetPwdVerifyOTPAsync(custInfoProfile.CustId, req.InputOTPcode);

            if (apiResult.ErrorCode == 0)
            {
                if (_custInfoBll.SetResetPwd(custInfoProfile.CustId, req.NewPassword))
                {
                    apiResult.ErrorCode = 0;
                    apiResult.Message = _selLangBll.GetMsg("ResetPwdSuccess");
                }
                else
                {
                    apiResult.ErrorCode = -1;
                    apiResult.Message = _selLangBll.GetMsg("msg_UpdateFailed");
                }
            }
            else
            {
                apiResult.ErrorCode = -1;
                apiResult.Message = _selLangBll.GetMsg("msg_OTP_SendOTPSMS_Fail");
            }

            return apiResult;
        }

        [HttpPost]
        public ApiResult<Dictionary<string, string>> GetTranslationData(string[] keys)
        {
            var result = new ApiResult<Dictionary<string, string>>();
            try
            {
                var datas = new Dictionary<string, string>();
                foreach (var k in keys)
                {
                    var lang = _selLangBll.GetResourceValue(k) ?? _selLangBll.GetMsg(k);
                    if (!datas.ContainsKey(k))
                    {
                        datas.Add(k, lang);
                    }
                }

                result.Data = datas;
                result.ErrorCode = 0;
            }
            catch (Exception ex)
            {
                result.ErrorCode = 999;
            }

            return result;
        }

        [HttpPost]
        public ApiResult<IEnumerable<int>> GetSiteCurrencyList()
        {
            var result = new ApiResult<IEnumerable<int>>();
            try
            {
                var serverInfo = _serverInfoBll.GetServerInfo();
                result.Data = serverInfo.ICurrencyList;
                result.ErrorCode = 0;
            }
            catch (Exception ex)
            {
                result.ErrorCode = 999;
                result.RawMessage = ex.ToString();
            }

            return result;
        }

        [HttpPost]
        public ApiResult<IEnumerable<SiteLanguage>> GetSiteLangList()
        {
            var result = new ApiResult<IEnumerable<SiteLanguage>>();
            try
            {
                result.Data = _selLangBll.GetLangIdList();
                result.ErrorCode = 0;
            }
            catch (Exception ex)
            {
                result.ErrorCode = 999;
                result.RawMessage = ex.ToString();
            }

            return result;
        }

        [HttpPost]
        public ApiResult CheckSignIn()
        {
            var result = new ApiResult();
            try
            {
                if (User.Identity?.IsAuthenticated == true)
                {
                    result.ErrorCode = 0;
                }
                else
                {
                    result.ErrorCode = -1;
                }
            }
            catch (Exception ex)
            {
                result.ErrorCode = 999;
                result.RawMessage = ex.ToString();
            }

            return result;
        }

        public ApiResult<IEnumerable<int>> GetTpIds(int currencyId = 0)
        {
            var apiResult = new ApiResult<IEnumerable<int>>();
            try
            {
                var authData = _authorizeBll.GetAuthData();
                if (authData is { CustId: > 0 })
                {
                    var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
                    _logger.LogInformation($"GetTpIds by CustInfo, CurrencyId:{custInfo.CurrencyId}, SiteId:{custInfo.SiteId}");
                    var tpIds = _casinoBll.GetProviderIdByCurrency(custInfo.CurrencyId, custInfo.SiteId).ToList();
                    if (string.CompareOrdinal(custInfo.AffCode, "67253") == 0)
                    {
                        tpIds.RemoveAll(item => item == (int)Tp.TCG);
                    }

                    apiResult.Data = tpIds;
                }
                else
                {
                    var serverInfo = _serverInfoBll.GetServerInfo();
                    _logger.LogInformation($"GetTpIds not login, CurrencyId:{currencyId}, SiteId:{serverInfo.SiteId}");
                    apiResult.Data = _casinoBll.GetProviderIdByCurrency(currencyId, serverInfo.SiteId);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, GetCurrentMethod());
            }

            apiResult.ErrorCode = 0;
            return apiResult;
        }

        public class BannerReq
        {
            public int BannerType { get; set; }

            public int? CurrencyId { get; set; }
        }

        [HttpPost]
        public ApiResult<IEnumerable<BannerV3>> GetBannerV3(BannerReq bannerReq, [FromRoute] string langId = null)
        {
            var apiResult = new ApiResult<IEnumerable<BannerV3>>();

            int siteId;
            try
            {
                var serverInfo = _serverInfoBll.GetServerInfo();
                int perferCurr;
                var ShowStatus = 1;
                var authData = _authorizeBll.GetAuthData();
                if (authData is { CustId: > 0 })
                {
                    var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
                    siteId = custInfo.SiteId;
                    perferCurr = custInfo.CurrencyId;
                    ShowStatus = 2;
                    langId = _selLangBll.GetLangId();
                }
                else
                {
                    perferCurr = bannerReq.CurrencyId ?? _selLangBll.GetPreferCurrencyId();
                    siteId = serverInfo.SiteId;
                    langId = _selLangBll.CheckLangId(langId) ? langId : _selLangBll.GetLangId();
                }
                apiResult.Data = _bannerBll.GetBannerV3(siteId, bannerReq.BannerType, ShowStatus, perferCurr, langId, serverInfo);

                var domain = _serverInfoBll.GetHttpDomain();
                foreach (var data in apiResult.Data)
                {
                    if (!string.IsNullOrEmpty(data.UrlRouter) && data.ActionMode == 6)
                    {
                        data.BannerLink = $"{domain}/{data.UrlRouter}";
                    }
                }
                apiResult.ErrorCode = 0;
            }
            catch (Exception e)
            {
                _logger.LogError(e, GetCurrentMethod());
            }

            return apiResult;
        }

        public class HotGameReq
        {
            public int DeviceType { get; set; }
            public int? CurrencyId { get; set; }
        }

        [HttpPost]
        public async Task<ApiResult<IEnumerable<HotGame>>> GetHotGames(HotGameReq req)
        {
            var apiResult = new ApiResult<IEnumerable<HotGame>>();
            try
            {
                int currencyId;
                int siteId;
                var authData = _authorizeBll.GetAuthData();
                if (authData is { CustId: > 0 })
                {
                    var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
                    siteId = custInfo.SiteId;
                    currencyId = custInfo.CurrencyId;
                }
                else
                {
                    var serverInfo = _serverInfoBll.GetServerInfo();
                    siteId = serverInfo.SiteId;
                    currencyId = _selLangBll.GetPreferCurrencyId();
                    if (req.CurrencyId.HasValue)
                    {
                        currencyId = req.CurrencyId.Value;
                    }
                }

                apiResult.ErrorCode = 0;
                apiResult.Data = await _bannerBll.GetHotGames(siteId, currencyId, req.DeviceType);
            }
            catch (Exception e)
            {
                _logger.LogError(e, GetCurrentAsyncMethod());
            }

            return apiResult;
        }

        [HttpPost]
        public async Task<ApiResult> CheckResetPwdTokenAsync([FromForm] string key)
        {
            var apiResult = new ApiResult(_selLangBll);
            try
            {
                if (_authBll.Authenticate(key, out CustInfoProfile custInfoProfile))
                {
                    Customer customer = null;
                    var fromUrl = _tools.GetFromUrl();
                    var deviceType = (int)_tools.GetDeviceId(fromUrl);
                    var userLoginResult = _custInfoBll.UserLoginForResetPwd(custInfoProfile.UserName, _tools.GetClientIP(), fromUrl, Request.Headers["User-Agent"], deviceType, ref customer, out var token);
                    switch (userLoginResult)
                    {
                        case 0:
                            {
                                var authData = _authorizeBll.GetAuthData();
                                if (authData is { CustId: > 0 } && authData.CustId != customer.CustId)
                                {
                                    apiResult.ErrorCode = 1;
                                }
                                else
                                {
                                    var claims = new List<Claim>
                                {
                                    new(ClaimTypes.Name, custInfoProfile.UserName),
                                    new(ClaimTypes.NameIdentifier, customer.CustId.ToString()),
                                    new("Token", token),
                                    new("LangId", custInfoProfile.UserLang)
                                };
                                    var claimsIdentity = new ClaimsIdentity(claims,
                                        CookieAuthenticationDefaults.AuthenticationScheme);
                                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                                        new ClaimsPrincipal(claimsIdentity));
                                    apiResult.ErrorCode = 0;
                                }

                                break;
                            }

                        case -3:
                            apiResult.ErrorCode = 401;
                            break;
                    }
                }
                else
                {
                    apiResult.ErrorCode = 403;
                }
            }
            catch (Exception e)
            {
                apiResult.ErrorCode = 403;
                _logger.LogError(e, GetCurrentAsyncMethod());
            }

            return apiResult;
        }
        [HttpPost]
        public ApiResult<Dictionary<string, string>> WhoAmI()
        {
            var result = new ApiResult<Dictionary<string, string>>();
            try
            {
                var ServerInfoData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                result.Data = ServerInfoData;
                ServerInfoData.Add("Now", DateTime.Now.ToString());
                ServerInfoData.Add("EnvironmentName", _environment.EnvironmentName);
                ServerInfoData.Add("LangId", _selLangBll.GetLangId());
                var host = Dns.GetHostEntry(Dns.GetHostName());
                var i = 1;
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        ServerInfoData.Add($"IP {i++}.", ip.ToString());
                    }
                }
                ServerInfoData.Add("Url.Host", Request.Host.Host);
                ServerInfoData.Add("Url.Port", Request.Host.Port.ToString());
                var serverInfo = _serverInfoBll.GetServerInfo();
                ServerInfoData.Add("SiteId", serverInfo.SiteId.ToString());
                ServerInfoData.Add("SiteName", serverInfo.SiteName);
                ServerInfoData.Add("LangList", serverInfo.LangList);
                ServerInfoData.Add("CurrencyList", serverInfo.CurrencyList);
                ServerInfoData.Add("DefCurrency", serverInfo.DefCurrency.ToString());
                ServerInfoData.Add("DefLang", serverInfo.DefLang);
                var userAgent = Request.Headers["User-Agent"];
                ServerInfoData.Add("User Agent", userAgent);

                //需要的欄位LIST
                var list = new List<string>
            {
                "X-Forwarded-For",
                "LOCAL_ADDR",
                "server_port",
                "Http_Host",
                "HTTP_INCAP_CLIENT_IP",
                "HTTP_TRUE_CLIENT_IP",
                "HTTP_X_REAL_IP",
                "REMOTE_ADDR",
                "HTTP_X_FORWARDED_FOR",
                "HTTP_X_FORWARDED_PROTO",
                "SERVER_NAME",
                "X-Client-IP",
                "X-Real-IP"
            };

                foreach (var head in Request.Headers)
                {
                    var sKey = head.Key;
                    if (list.Contains(sKey, StringComparer.OrdinalIgnoreCase) && !ServerInfoData.ContainsKey(sKey))
                    {
                        ServerInfoData.Add(sKey, head.Value);
                    }
                }

                result.ErrorCode = 0;
            }
            catch (Exception ex)
            {
                result.ErrorCode = 999;
                result.RawMessage = ex.ToString();
            }

            return result;
        }

        // [HttpPost]
        // public ApiResult<bool> ExistsPhoneNo(RrofileReq req)
        // {
        //     var apiResult = new ApiResult<bool>(_selLangBll);
        //
        //     try
        //     {
        //         if (CheckAttack(req, out var result))
        //         {
        //             return result;
        //         }
        //
        //         if (long.TryParse(req.PhoneNo, out var phoneNo))
        //         {
        //             req.PhoneNo = phoneNo.ToString();
        //             apiResult.Data = _custInfoBll.ExistsPhoneNo(0, req.AreaCode, req.PhoneNo);
        //         }
        //         else
        //         {
        //             apiResult.Data = true;
        //         }
        //         apiResult.ErrorCode = 0;
        //         return apiResult;
        //     }
        //     catch (Exception e)
        //     {
        //         _logger.LogError(e, GetCurrentMethod());
        //         return apiResult;
        //     }
        // }

        [HttpPost]
        public ApiResult<IEnumerable<Country>> GetCountryList()
        {
            var apiResult = new ApiResult<IEnumerable<Country>>
            {
                Data = _selLangBll.GetCountryList(),
                ErrorCode = 0
            };
            return apiResult;
        }
        public enum TypeId
        {
            Slots = 1,
            Arcade = 2,
            Table = 3,
            ScratchCard = 4
        }

        public class AffiliateAgentReq
        {
            public string AffId { get; set; }
        }
        /// <summary>
        /// 取得Referral 所屬幣別
        /// </summary>
        /// <param name="affiliateAgentReq"></param>
        /// <returns></returns>
        [HttpPost]
        public ApiResult<AffiliateAgent> GetAffiliateAgent(AffiliateAgentReq affiliateAgentReq)
        {
            var apiResult = new ApiResult<AffiliateAgent>
            {
                Data = new AffiliateAgent()
            };

            if (string.IsNullOrWhiteSpace(affiliateAgentReq.AffId) || affiliateAgentReq.AffId.Length > 32)
            {
                apiResult.ErrorCode = -1;
                apiResult.Message = "no AffId";
                return apiResult;
            }

            var result = _custInfoBll.GetAffiliateAgentBll(affiliateAgentReq.AffId);
            apiResult.ErrorCode = 0;
            switch (result)
            {
                case > 0:
                    apiResult.Data.CurrencyId = result;
                    break;
                case -19:
                    apiResult.ErrorCode = result;
                    apiResult.Message = _selLangBll.GetMsg("mag_AffiliateNotExist");
                    break;
                default:
                    apiResult.ErrorCode = result;
                    break;
            }

            return apiResult;
        }


        public class AffiliateExitsReq
        {
            public string AffCode { get; set; }
        }
        /// <summary>
        /// 檢查代理是否存在
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        [HttpPost]
        public ApiResult<int> AffiliateCheck(AffiliateExitsReq req)
        {
            var apiResult = new ApiResult<int>();
            var errorCode = _custInfoBll.AffiliateAndReferralCheck(req.AffCode.Trim(), out int currencyId);
            apiResult.ErrorCode = errorCode;
            apiResult.Data = currencyId;
            apiResult.Message = errorCode switch
            {
                -9 => _selLangBll.GetMsg("msg_AffCodeValidate"),
                -10 => _selLangBll.GetMsg("msg_AffCodeNotFound"),
                _ => "",
            };
            return apiResult;
        }

        [HttpPost]
        public ApiResult<IEnumerable<Domainlist>> GetSupportDomainList()
        {
            var apiResult = new ApiResult<IEnumerable<Domainlist>>();
            var siteId = 0;
            var currencyId = 0;
            var authData = _authorizeBll.GetAuthData();
            if (authData is { CustId: > 0 })
            {
                var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
                siteId = custInfo.SiteId;
                currencyId = custInfo.CurrencyId;
            }
            else
            {
                var serverInfo = _serverInfoBll.GetServerInfo();
                siteId = serverInfo.SiteId;
                currencyId = _selLangBll.GetPreferCurrencyId();
            }

            apiResult.Data = _serverInfoBll.GetSupportDomainListWithCache(siteId, currencyId);
            apiResult.ErrorCode = 0;
            return apiResult;
        }

        [HttpGet, HttpPost]
        public ApiResult<IEnumerable<TpMaintain>> GetTpMaintainList()
        {
            var authData = _authorizeBll.GetAuthData();
            if (authData is { CustId: > 0 })
            {
                var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
                if (custInfo.IsTest == true)
                {
                    return new ApiResult<IEnumerable<TpMaintain>>()
                    {
                        ErrorCode = 0,
                        Data = new List<TpMaintain>(),
                        Message = $"UtcNow is {DateTime.UtcNow:yyyy/MM/dd HH:mm:ss}"
                    };
                }
            }

            return new ApiResult<IEnumerable<TpMaintain>>()
            {
                ErrorCode = 0,
                Data = _thirdPartyBll.GetTpMaintainListWithCache(_serverInfoBll.GetServerInfo().SiteId),
                Message = $"UtcNow is {DateTime.UtcNow:yyyy/MM/dd HH:mm:ss}"
            };
        }

        [Route("{domain?}")]
        [HttpGet, HttpPost]
        public ApiResult<SeoModel> Seo([FromRoute] string? domain)
        {
            if (string.IsNullOrWhiteSpace(domain))
            {
                domain = _serverInfoBll.GetMainDomain();
            }
            var data = _serverInfoBll.GetSeoWithCache(domain);

            if (data != null)
            {
                data.Meta = string.IsNullOrEmpty(data.Meta) ? null : data.Meta;
                data.Title = string.IsNullOrEmpty(data.Title) ? null : data.Title;
                data.DefaultLang = string.IsNullOrEmpty(data.DefaultLang) ? null : data.DefaultLang;
                data.Keyword = string.IsNullOrEmpty(data.Keyword) ? null : data.Keyword;
                data.Script = string.IsNullOrEmpty(data.Script) ? null : data.Script;
                data.XCMScript = string.IsNullOrEmpty(data.XCMScript) ? null : data.XCMScript;
                if (!string.IsNullOrEmpty(data.FollowUsData))
                {
                    data.FollowUs = JsonConvert.DeserializeObject<List<FollowUs>>(data.FollowUsData);
                }
                if (!string.IsNullOrEmpty(data.PaymentMethodData))
                {
                    data.PaymentMethod = JsonConvert.DeserializeObject<List<PaymentMethod>>(data.PaymentMethodData);
                }
            }


            return new ApiResult<SeoModel>()
            {
                ErrorCode = 0,
                Data = data
            };
        }

        [HttpGet]
        public string Domain() => $"MainDomain:{_serverInfoBll.GetMainDomain()}, SubDomain:{_serverInfoBll.GetSubDomain()}";

        [HttpPost]
        public async Task<ApiResult<IEnumerable<MenuOfGame>>> GetMenuOfGames()
        {
            var domain = _serverInfoBll.GetMainDomain();
            var data = _serverInfoBll.GetSeoWithCache(domain);
            var apiResult = new ApiResult<IEnumerable<MenuOfGame>>()
            {
                ErrorCode = 0
            };
            if (data != null && !string.IsNullOrEmpty(data.DefaultLang) && _serverInfoBll.IsSeoDomain(domain))
            {
                apiResult.Data = await _serverInfoBll.GetMenuOfGames(data.SiteId, data.DefaultLang);
            }

            return apiResult;
        }

        [HttpPost]
        [EnableCors("SocialChannel")]
        public async Task<ApiResult<IEnumerable<SocialChannelResponse>>> GetSocialChannel(SocialChannelRequest request,
            [FromRoute] string langId = "en")
        {
            var authData = _authorizeBll.GetAuthData();
            if (authData is { CustId: > 0 })
            {
                var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
                request.CurrencyId = custInfo.CurrencyId;
                request.LangId = authData.LangId;
                request.SiteId = custInfo.SiteId;
            }
            else
            {
                var serverInfo = _serverInfoBll.GetServerInfo();
                request.SiteId = serverInfo.SiteId;
            }

            request.LangId ??= langId;

            var data = await _serverInfoBll.GetSocialChannel(request);
            var apiResult = new ApiResult<IEnumerable<SocialChannelResponse>>()
            {
                ErrorCode = 0
            };

            if (data != null)
            {
                apiResult.Data = data;
            }

            return apiResult;
        }

        [HttpPost]
        public async Task<ApiResult<RecaptchaSiteInfo>> GetRecaptchaInfo()
        {
            var clientIp = _tools.GetClientIP();
            var siteInfo = await _recaptchaBll.GetInfo(clientIp);
            var apiResult = new ApiResult<RecaptchaSiteInfo>()
            {

                ErrorCode = 0,
                Data = siteInfo
            };
            return apiResult;
        }

        public class VipBannerReq
        {
            public int CurrencyId { get; set; }
        }

        [HttpPost]
        public ApiResult<VipBanner> GetVipBanner(VipBannerReq vipBannerReq)
        {
            var apiResult = new ApiResult<VipBanner>() { ErrorCode = 0 };
            int siteId;
            try
            {
                var serverInfo = _serverInfoBll.GetServerInfo();
                int perferCurr;
                var authData = _authorizeBll.GetAuthData();

                if (authData is { CustId: > 0 })
                {
                    var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
                    siteId = custInfo.SiteId;
                    perferCurr = custInfo.CurrencyId;
                }
                else
                {
                    perferCurr = vipBannerReq.CurrencyId;
                    siteId = serverInfo.SiteId;
                }
                apiResult.Data = _bannerBll.GetVipBanner(siteId, perferCurr);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, GetCurrentMethod());
            }
            return apiResult;
        }

        [HttpPost]
        public async Task<ApiResult<IpCountryWithAccess>> GetIpCountry()
        {
            return new ApiResult<IpCountryWithAccess>()
            {
                ErrorCode = (int)ErrorCode.Success,
                Data = await _checkIpBll.CheckIp(_tools.GetClientIP())
            };
        }

        //[HttpPost]
        //public async Task<ApiResult<bool>> ReCaptchaV3Test()
        //{
        //    _recaptchaBll.CreateAssessment();
        //    return new ApiResult<bool>()
        //    {
        //        ErrorCode = (int)ErrorCode.Success
        //    };
        //}
    }
}
