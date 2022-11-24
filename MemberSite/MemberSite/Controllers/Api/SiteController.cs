using MemberSite.Service.BLL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Sockets;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Amazon.S3.Model;
using MemberSite.Service.Model.Mapping;
using Microsoft.AspNetCore.Cors;
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
        private readonly FPSBLL _sFpsBll;
        private readonly GameCasinoBll _gameCasinoBll;
        private readonly IOptions<Dictionary<string, SiteConfig>> _siteConfig;
        private readonly Regex _regexUserName = new(@"^[a-zA-Z0-9]{6,18}$");
        private readonly ThirdPartyBll _thirdPartyBll;
        private readonly SiteBll _siteBll;
        private readonly RecaptchaBLL _recaptchaBll;
        private readonly ILogger<SiteController> _logger;
        private readonly ICacheHelper _cacheHelper;
        private readonly Random _random = new();
        private readonly IEnumerable<AreaCodeMap> _areaCodeMap;
        private readonly IpHelper _ipHelper;
        private readonly IPBLL _iPBll;
        private readonly AWSS3Helper _s3Helper;
        private readonly string _fileName;
        private readonly string _folder;

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
            _sFpsBll = provider.GetRequiredService<FPSBLL>();
            _gameCasinoBll = provider.GetRequiredService<GameCasinoBll>();
            _siteConfig = provider.GetRequiredService<IOptions<Dictionary<string, SiteConfig>>>();
            _thirdPartyBll = provider.GetRequiredService<ThirdPartyBll>();
            _siteBll = provider.GetRequiredService<SiteBll>();
            _recaptchaBll = provider.GetRequiredService<RecaptchaBLL>();
            _logger = provider.GetRequiredService<ILogger<SiteController>>();
            _cacheHelper = provider.GetRequiredService<ICacheHelper>();
            _areaCodeMap = provider.GetRequiredService<IEnumerable<AreaCodeMap>>();
            _ipHelper = provider.GetRequiredService<IpHelper>();
            _iPBll = provider.GetRequiredService<IPBLL>();
            _s3Helper = provider.GetRequiredService<AWSS3Helper>();
            var env = provider.GetRequiredService<IWebHostEnvironment>(); 
            _fileName = env.IsProduction() || env.EnvironmentName == "Release" ? "blockIp.txt" : "blockIp_test.txt";
            _folder = Path.Combine(env.ContentRootPath, "NginxConfig");
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
            public string di { get; set; }
        }

        [HttpPost]
        public async Task<ApiResult> SignIn(LoginModel loginModel)
        {
            var apiResult = new ApiResult(_selLangBll);
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

            if (loginModel.di != null)
            {
                _ = Task.Run(async () =>
                {
                    await _sFpsBll.FPSChecksAsync(customer.CustId, "login", "Successfully", loginModel.url, loginModel.platform, loginModel.userAgent, loginModel.screen, loginModel.di, ip);
                });
            }

            await _cacheHelper.StringFireAndForgetSetAsync(customer.CustId.ToString(), token, TimeSpan.FromMinutes(30), RedisDataBase.Token);
            Response.Cookies.Delete($"Initialization{User.Identity?.Name}");
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
            apiResult.Message = Url.RouteUrl("desktop", new { action = "Index", controller = "Home", LangId = _selLangBll.GetLangId() });
            apiResult.ErrorCode = 0;
            return apiResult;
        }

        [HttpPost]
        public ApiResult<JwtAuthData> SignInApp(LoginModel loginModel)
        {
            var apiResult = new ApiResult<JwtAuthData>(_selLangBll);
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

            if (loginModel.di != null)
            {
                _ = Task.Run(async () =>
                {
                    await _sFpsBll.FPSChecksAsync(customer.CustId, "login", "Successfully", loginModel.url, loginModel.platform, loginModel.userAgent, loginModel.screen, loginModel.di, ip);
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
            public string Email { get; set; }
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

        [HttpPost]
        public async Task<ApiResult> SignupAsync(SignupModel signupModel)
        {
            var apiResult = new ApiResult(_selLangBll);
            var isAuthenticated = User.Identity is { IsAuthenticated: true };
            var customer = _siteBll.Signup(signupModel, apiResult, isAuthenticated, Request.Host.Host,
                Request.Headers["User-Agent"]);
            
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
            var customer = _siteBll.Signup(signupModel, apiResult, isAuthenticated, Request.Host.Host,
                Request.Headers["User-Agent"]);
            
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
        [HttpPost]
        public ApiResult ForgetPwd(ForgetPwdReq forgetPwdReq)
        {
            var apiResult = new ApiResult<bool>(_selLangBll);
            try
            {
                Customer custInfo;
                var host = Request.Host.ToString();
                var deviceType = (int)_tools.GetDeviceId(host);
                var resetPasswordUrl = Url.RouteUrl("desktop", new { action = "ResetPassword", controller = "Home", LangId = _selLangBll.GetLangId() }, protocol: Url.ActionContext.HttpContext.Request.Scheme, host: Url.ActionContext.HttpContext.Request.Host.Host);
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

                    _custInfoBll.ForgetPwd1(custInfo.CustId, resetPasswordUrl);
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
            }
            catch (Exception ex)
            {
                apiResult.RawMessage = ex.Message;
            }

            return apiResult;
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

                if (forgetPwdReq.flag == 1)
                {
                    resetPasswordUrl = $"{Url.ActionContext.HttpContext.Request.Scheme}://{Url.ActionContext.HttpContext.Request.Host.Host}/{_selLangBll.GetLangId()}/resetPassword";
                }
                else if (forgetPwdReq.flag == 2)
                {
                    if (Url.ActionContext.HttpContext.Request.Host.Host == "app.n17881788.com")
                    {
                        resetPasswordUrl = $"{Url.ActionContext.HttpContext.Request.Scheme}://m.n17881788.com/{_selLangBll.GetLangId()}/resetPassword";
                    }
                    else if (Url.ActionContext.HttpContext.Request.Host.Host == "app.bwing.com")
                    {
                        resetPasswordUrl = $"{Url.ActionContext.HttpContext.Request.Scheme}://m.bwing.com/{_selLangBll.GetLangId()}/resetPassword";
                    }
                    else if (Url.ActionContext.HttpContext.Request.Host.Host == "app.goal123.com")
                    {
                        resetPasswordUrl = $"{Url.ActionContext.HttpContext.Request.Scheme}://m.goal123.com/{_selLangBll.GetLangId()}/resetPassword";
                    }
                    else
                    {
                        //H5
                        resetPasswordUrl = $"{Url.ActionContext.HttpContext.Request.Scheme}://{Url.ActionContext.HttpContext.Request.Host.Host}/resetPassword";
                    }
                }
                else
                {
                    resetPasswordUrl = $"{Url.ActionContext.HttpContext.Request.Scheme}://{Url.ActionContext.HttpContext.Request.Host.Host}/resetPassword";
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

        public ApiResult<IEnumerable<int>> GetTpIds()
        {
            var apiResult = new ApiResult<IEnumerable<int>>();
            try
            {
                var authData = _authorizeBll.GetAuthData();
                if (authData is { CustId: > 0 })
                {
                    var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
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
                    apiResult.Data = _casinoBll.GetProviderIdByCurrency(0, serverInfo.SiteId);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, GetCurrentMethod());
            }

            apiResult.ErrorCode = 0;
            return apiResult;
        }

        [HttpPost]
        public ApiResult<IEnumerable<BannerSettingItem>> GetBanner([FromForm] string FolderName, [FromForm] string refId, [FromForm] bool filterHideInHome)
        {
            var result = new ApiResult<IEnumerable<BannerSettingItem>>();

            var serverInfo = _serverInfoBll.GetServerInfo();
            var dataPath = Path.Combine(_environment.WebRootPath, $"datas/{serverInfo.SiteName}_bannerSetting.json");
            var bannerSetting = new BannerSetting();
            if (System.IO.File.Exists(dataPath))
            {
                bannerSetting = JsonConvert.DeserializeObject<BannerSetting>(System.IO.File.ReadAllText(dataPath));
            }

            var perferCurr = 0;
            var beforeLogin = true;
            var authData = _authorizeBll.GetAuthData();
            if (authData is { CustId: > 0 })
            {
                var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
                perferCurr = custInfo.CurrencyId;
                beforeLogin = false;
            }
            else
            {
                perferCurr = _selLangBll.GetPreferCurrencyId();
            }

            if (FolderName == "Promotion")
            {
                var bannerFromJSON = bannerSetting.Promotion.Where(x => x.currencyId == null || beforeLogin || x.currencyId.Any(y => y == perferCurr));
                result.Data = bannerFromJSON;

                if (filterHideInHome)
                {
                    result.Data = result.Data.Where(x => !x.hideInHome);
                }
            }
            else if (FolderName == "Casino")
            {
                result.Data = bannerSetting.Casino.Where(x => x.currencyId == null || beforeLogin || x.currencyId.Any(y => y == perferCurr));
            }
            else if (FolderName == "Sports")
            {
                result.Data = bannerSetting.Sports.Where(x => x.currencyId == null || beforeLogin || x.currencyId.Any(y => y == perferCurr));
            }

            if (!string.IsNullOrEmpty(refId))
            {
                result.Data = result.Data.Where(x => x.refId != null && x.refId.Any(r => string.Equals(r, refId, StringComparison.OrdinalIgnoreCase)));
            }

            result.ErrorCode = 0;
            return result;
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

            try
            {
                var serverInfo = _serverInfoBll.GetServerInfo();
                int preferCurr;
            	int siteId;
                var showStatus = 1;
                var authData = _authorizeBll.GetAuthData();
                if (authData is { CustId: > 0 })
                {
                    var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
                    siteId = custInfo.SiteId;
                    preferCurr = custInfo.CurrencyId;
                    showStatus = 2;
                    langId = _selLangBll.GetLangId();
                }
                else
                {
                    preferCurr = bannerReq.CurrencyId ?? _selLangBll.GetPreferCurrencyId();
                    siteId = serverInfo.SiteId;
                    langId = _selLangBll.CheckLangId(langId) ? langId : _selLangBll.GetLangId();
                }
                apiResult.Data = _bannerBll.GetBannerV3(siteId, bannerReq.BannerType, showStatus, preferCurr, langId, serverInfo);
                var domain = _serverInfoBll.GetHttpDomain();
	            foreach (var data in apiResult.Data)
	            {
	                if (!string.IsNullOrEmpty(data.BannerLink))
	                {
	                    data.BannerLink = data.BannerLink.Trim().TrimStart('/');
	                }
	                data.BannerLink = $"{domain}/{data.BannerLink}";
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
                "SERVER_NAME"
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

        [HttpPost]
        public ApiResult<bool> ExistsPhoneNo(RrofileReq req)
        {
            var apiResult = new ApiResult<bool>(_selLangBll);

            try
            {
                if (CheckAttack(req, out var result))
                {
                    return result;
                }

                if (long.TryParse(req.PhoneNo, out var phoneNo))
                {
                    req.PhoneNo = phoneNo.ToString();
                    apiResult.Data = _custInfoBll.ExistsPhoneNo(0, req.AreaCode, req.PhoneNo);
                }
                else
                {
                    apiResult.Data = true;
                }
                apiResult.ErrorCode = 0;
                return apiResult;
            }
            catch (Exception e)
            {
                _logger.LogError(e, GetCurrentMethod());
                return apiResult;
            }
        }

        private bool CheckAttack(RrofileReq req, out ApiResult<bool> result)
        {
            result = new ApiResult<bool>();
            var clientIp = _tools.GetClientIP();
            if (_ipHelper.IpInRange(clientIp))
            {
                return false;
            }
            
            var serverInfo = _serverInfoBll.GetServerInfo();
            var checkCountForShortTime = _cacheHelper.StringIncrementAsync($"IP:{clientIp}", TimeSpan.FromMinutes(1)).Result;
            var checkCountForLongTime = _cacheHelper.StringIncrementAsync($"IP:10:{clientIp}", TimeSpan.FromMinutes(20)).Result;
            var checkCountForBlock = _cacheHelper.StringIncrementAsync($"IP:hour:{clientIp}", TimeSpan.FromHours(1)).Result;
            if (checkCountForShortTime >= 5 || checkCountForLongTime >= 15)
            {
                _cacheHelper.StringFireAndForgetSetAsync($"block:{clientIp}", string.Empty, TimeSpan.FromHours(1));
            }

            var isDart = Request.Headers["User-Agent"].ToString().Contains("Dart");

            if (checkCountForBlock >= 60 || isDart)
            {
                var reason = isDart ? "agent use Dart" : "too many requests";
                var memo = $"Auto Deny - Attack: get user phone number, reason is {reason}";
                var setResult = _iPBll.SetBlockIp(new BlockIpRequest { IP = clientIp, Memo = memo, Operator = "System" });
                if (setResult > 0 && !_cacheHelper.KeyExists($"AutoDeny:{clientIp}"))
                {
                    _cacheHelper.StringFireAndForgetSetAsync($"AutoDeny:{clientIp}", string.Empty, TimeSpan.FromMinutes(1));
                    _ = Task.Run(async () => { _ = await Upload(); });
                }
            }

            if ((serverInfo.SiteId is (int)SiteId.Bwing or (int)SiteId.Goal123 && req.AreaCode != "+84")
                || (serverInfo.SiteId is (int)SiteId.Nova88 && _serverInfoBll.GetRequestUrl().Contains("indo") &&
                    req.AreaCode != "+62" && req.AreaCode != "+91")
                || isDart
                || _areaCodeMap.All(i => i.AreaCode != req.AreaCode)
                || checkCountForShortTime >= 5
                || checkCountForLongTime >= 15
                || checkCountForBlock >= 60
                || _cacheHelper.KeyExists($"block:{clientIp}"))
            {
                _logger.LogInformation($"Attack: AreaCode:{req.AreaCode}, PhoneNo:{req.PhoneNo}, REQUEST URL:{_serverInfoBll.GetRequestUrl()}, " +
                                       $"IP: {Request.Headers["X-Forwarded-For"]}, {Request.Headers["HTTP_X_FORWARDED_FOR"]}, {Request.Headers["HTTP_INCAP_CLIENT_IP"]}, {Request.Headers["HTTP_TRUE_CLIENT_IP"]}, {Request.Headers["HTTP_X_REAL_IP"]}, {Request.Headers["REMOTE_ADDR"]}, {HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString()}");

                result.ErrorCode = 0;
                result.Data = _random.NextDouble() >= 0.5; //亂數回傳 true false
                return true;
            }

            return false;
        }

        private async Task<bool> Upload()
        {
            try
            {
                var ipList = _iPBll.GetBlockIp(true).Select(i => i.IP).ToList();
                Directory.CreateDirectory(_folder);
                var filePath = Path.Combine(_folder, _fileName);
                await System.IO.File.WriteAllLinesAsync(filePath, ipList);
                using var destination = new MemoryStream();
                await using (var source = System.IO.File.Open(filePath, FileMode.Open))
                {
                    await source.CopyToAsync(destination);
                }

                var putRequest = new PutObjectRequest
                {
                    BucketName = "ailab-config",
                    ContentType = "text/plain",
                    InputStream = destination,
                    Key = _fileName
                };
                return await _s3Helper.PutObjectAsync(putRequest);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Upload S3 Fail.");
                return false;
            }
        }

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

        public class GetGameListByDeviceReq
        {
            public int DeviceType { get; set; }
        }
        public ApiResult<GameInfo> GetGameListByDevice(GetGameListByDeviceReq getGameListByDeviceReq)
        {
            var apiResult = new ApiResult<GameInfo>();
            var resultData = new GameInfo
            {
                ProviderList = new List<Provider>()
            };

            //不分Tpid取出GameCasino資料
            var GameCasinoList = _gameCasinoBll.GetGameCasinoListByDevice(getGameListByDeviceReq.DeviceType);

            if (getGameListByDeviceReq.DeviceType == 3)
            {
                GameCasinoList = GameCasinoList.Where(x => x.TpId != 27).ToList();
            }

            //GameCasinoList裡總共有多少種TpId
            var GameCasinoHasTpid = GameCasinoList.Select(x => x.TpId).Distinct();
            var providers = _gameCasinoBll.GetProviderList();

            IEnumerable<int> tpIds = null;
            IEnumerable<GameCasionByCurrencyAndSite> gameCasionByCurrencyAndSite = null;

            var serverInfo = _serverInfoBll.GetServerInfo();
            var authData = _authorizeBll.GetAuthData();
            if (authData is { CustId: > 0 })
            {
                var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
                tpIds = _casinoBll.GetProviderIdByCurrency(custInfo.CurrencyId, custInfo.SiteId);
                //取出有byCurrency跟Site設定的遊戲資訊
                gameCasionByCurrencyAndSite =
                    _casinoBll.GetGameCasinoByCurrencyAndSite(custInfo.CurrencyId, custInfo.SiteId);

                var blockGames = _casinoBll.GetTpDisableUserByCustId(custInfo.CustId);
                _casinoBll.ExcludeBlockGames(GameCasinoList, providers, blockGames);
            }
            else
            {
                tpIds = _casinoBll.GetProviderIdByCurrency(0, serverInfo.SiteId);
                //取出有byCurrency跟Site設定的遊戲資訊
                gameCasionByCurrencyAndSite = _casinoBll.GetGameCasinoByCurrencyAndSite(_selLangBll.GetPreferCurrencyId(), serverInfo.SiteId);
            }

            //濾掉不符合權限的

            var filterCon = GameCasinoHasTpid?.Where(x => tpIds.Contains(x)).ToDictionary(x => Convert.ToInt16(x), x => false);

            var dictgameCasionByCurrencyAndSite = gameCasionByCurrencyAndSite?.ToDictionary(x => Convert.ToInt32(x.GameCasinoId), x => x);

            foreach (var provider in providers?.Where(thisProvider => filterCon.ContainsKey(thisProvider.Value)))
            {
                provider.GameList = new GameDesc
                {
                    ImgUrl = "",
                    Data = new List<Datum>()
                };

                GameCasinoList.FindAll(x => x.TpId == provider.Value).ForEach(y =>
                {
                    var nameItem = JsonConvert.DeserializeObject<Dictionary<string, string>>(y.Name);
                    string imgName = null;

                    if (nameItem.ContainsKey("en"))
                    {
                        imgName = nameItem["en"];
                    }

                    var imgNameD = nameItem.ToDictionary(x => x.Key, x => $"{OrgFileNameFilter($"{imgName}-{x.Key}")}.jpg");
                    if (dictgameCasionByCurrencyAndSite != null && dictgameCasionByCurrencyAndSite.ContainsKey(y.GameCasinoId))
                    {
                        if (dictgameCasionByCurrencyAndSite[y.GameCasinoId].Status)
                        {
                            provider.GameList.Data.Add(new Datum
                            {
                                Name = nameItem,
                                ImgName = imgNameD,
                                AppId = y.AppId,
                                GameId = y.GameId,
                                Category1 = Enum.GetName(typeof(TypeId), y.TypeId),
                                Paylines = y.Paylines,
                                Rtp = y.Rtp,
                                Reels = string.IsNullOrWhiteSpace(y.Reel) ? string.Empty : y.Reel,
                                LaunchDate = y.LaunchDate,
                                Hot = dictgameCasionByCurrencyAndSite[y.GameCasinoId].Hot,
                                New = y.LaunchDate.AddDays(30) > DateTime.Now,
                                ImageExist = true,
                                promotion = dictgameCasionByCurrencyAndSite[y.GameCasinoId].Promotion ? 1 : 0,
                                featured = dictgameCasionByCurrencyAndSite[y.GameCasinoId].Featured,
                                PlayFree = y.Fun
                            });
                        }
                    }
                    else
                    {
                        provider.GameList.Data.Add(new Datum
                        {
                            Name = nameItem,
                            ImgName = imgNameD,
                            AppId = y.AppId,
                            GameId = y.GameId,
                            Category1 = Enum.GetName(typeof(TypeId), y.TypeId),
                            Paylines = y.Paylines,
                            Rtp = y.Rtp,
                            Reels = string.IsNullOrWhiteSpace(y.Reel) ? string.Empty : y.Reel,
                            LaunchDate = y.LaunchDate,
                            Hot = y.Hot,
                            New = y.LaunchDate.AddDays(30) > DateTime.Now,
                            ImageExist = true,
                            promotion = y.promotion ? 1 : 0,
                            featured = y.featured,
                            PlayFree = y.Fun
                        });
                    }
                });
                resultData.ProviderList.Add(provider);
            }
            apiResult.Data = resultData;
            apiResult.Data.ImgUrl = _siteConfig.Value[serverInfo.SiteName].ImageURL;
            apiResult.ErrorCode = 0;
            return apiResult;
        }

        private string OrgFileNameFilter(string fileName)
        {
            return fileName = fileName.Replace(" ", "-")
                                       .Replace("&", "-")
                                       .Replace(":", "-")
                                       .Replace("'", "")
                                       .Replace("’", "")
                                       .Replace(".", "")
                                       .Replace("™", "")
                                       .Replace("---", "-")
                                       .Replace("--", "-")
                                       .Replace("\u200B", "")
                                       .Replace(",", "")
                                       .ToLower();
        }


        public class AffiliateAgentReq
        {
            public string AffId { get; set; }
        }
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
    }
}
