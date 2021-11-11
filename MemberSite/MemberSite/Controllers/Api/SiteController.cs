using MemberSite.Service.BLL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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
        private readonly CommonBLL _commonBll;
        private readonly IWebHostEnvironment _environment;
        private readonly AuthorizeBLL _authorizeBll;
        private readonly CasinoBll _casinoBll;
        private readonly ServerInfoBll _serverInfoBll;
        private readonly BannerBLL _bannerBll;
        private readonly AuthBLL _authBll;
        private readonly IovationBLL _iovationBll;
        private readonly FPSBLL _sFpsBll;
        private readonly GameCasinoBll _gameCasinoBll;
        private readonly IOptions<Dictionary<string, SiteConfig>> _siteConfig;
        private readonly Regex _regexUserName = new Regex(@"^[a-zA-Z0-9]{6,18}$");
        private readonly Regex _regexAffCode = new Regex(@"^[0-9]{5,20}$");
        private readonly ThirdPartyBll _thirdPartyBll;
        private readonly CacheHelper _cacheHelper;

        public SiteController(IServiceProvider provider)
        {
            _custInfoBll = provider.GetRequiredService<CustInfoBll>();
            _tools = provider.GetRequiredService<Tools>();
            _selLangBll = provider.GetRequiredService<SelLangBLL>();
            _commonBll = provider.GetRequiredService<CommonBLL>();
            _environment = provider.GetRequiredService<IWebHostEnvironment>();
            _authorizeBll = provider.GetRequiredService<AuthorizeBLL>();
            _casinoBll = provider.GetRequiredService<CasinoBll>();
            _serverInfoBll = provider.GetRequiredService<ServerInfoBll>();
            _bannerBll = provider.GetRequiredService<BannerBLL>();
            _authBll = provider.GetRequiredService<AuthBLL>();
            _iovationBll = provider.GetRequiredService<IovationBLL>();
            _sFpsBll = provider.GetRequiredService<FPSBLL>();
            _gameCasinoBll = provider.GetRequiredService<GameCasinoBll>();
            _siteConfig = provider.GetRequiredService<IOptions<Dictionary<string, SiteConfig>>>();
            _thirdPartyBll = provider.GetRequiredService<ThirdPartyBll>();
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
            var userLoginResult = _custInfoBll.UserLogin(loginModel.UserName, loginModel.Userpwd, ip, _tools.GetFromUrl(),
                Request.Headers["User-Agent"], ref customer, out var token);

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
                    new Claim(ClaimTypes.Name,customer.UserName ),
                    new Claim(ClaimTypes.NameIdentifier,customer.CustId.ToString() ),
                    new Claim("Token",token ),
                    new Claim("LangId",customer.UserLang )
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

            Response.Cookies.Delete($"Initialization{User.Identity.Name}");
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
            var userLoginResult = _custInfoBll.UserLogin(loginModel.UserName, loginModel.Userpwd, ip, _tools.GetFromUrl(),
                Request.Headers["User-Agent"], ref customer, out var token);

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
                //await HttpContext.SignOutAsync(JwtBearerDefaults.AuthenticationScheme);
                return apiResult;
            }

            var claims = new[]
                {
                    new Claim(ClaimTypes.Name,customer.UserName ),
                    new Claim(ClaimTypes.NameIdentifier,customer.CustId.ToString() ),
                    new Claim("Token",token ),
                    new Claim("LangId",customer.UserLang ),
                    //new Claim("CustId",customer.CustId.ToString() ),
                    //new Claim("SiteId",customer.SiteId.ToString() ),
                    //new Claim("UserName",customer.UserName ),
                    //new Claim(JwtRegisteredClaimNames.Sub, customer.UserName),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString() )
                };

            if (loginModel.di != null)
            {
                _ = Task.Run(async () =>
                {
                    await _sFpsBll.FPSChecksAsync(customer.CustId, "login", "Successfully", loginModel.url, loginModel.platform, loginModel.userAgent, loginModel.screen, loginModel.di, ip);
                });
            }

            //Response.Cookies.Delete($"Initialization{User.Identity.Name}");
            //var claimsIdentity = new ClaimsIdentity(claims, JwtBearerDefaults.AuthenticationScheme);
            //await HttpContext.SignInAsync(JwtBearerDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

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
        }

        [HttpPost]
        public async Task<ApiResult> RegisterAsync(RegisteredModel registeredModel)
        {
            var apiResult = new ApiResult(_selLangBll);
            var serverInfo = _serverInfoBll.GetServerInfo();
            if (User.Identity.IsAuthenticated)
            {
                apiResult.ErrorCode = 9000;
                return apiResult;
            }

            var custId = 0;

            if (!string.IsNullOrWhiteSpace(registeredModel.Email) && _custInfoBll.ExistsEmail(custId, registeredModel.Email))
            {
                apiResult.ErrorCode = -10;
                apiResult.Message = $"{registeredModel.Email} {_selLangBll.GetMsg("msg_HasExist")}";
                return apiResult;
            }

            var result = _custInfoBll.AddCustInfo(registeredModel.UserName, registeredModel.Userpwd,
                registeredModel.Email, registeredModel.CurrencyId, registeredModel.UserLang, registeredModel.RefId,
                registeredModel.AffId, registeredModel.Aff, registeredModel.AffCode, serverInfo.SiteId, out custId, Request.Host.Host);
            if (result == -1)
            {
                apiResult.ErrorCode = result;
                apiResult.Message = _selLangBll.GetMsg("err_ThisCurrencyNotSupport");
            }
            else if (result == -2)
            {
                apiResult.ErrorCode = result;
                apiResult.Message = _selLangBll.GetMsg("msg_UsernameValidate");
            }
            else if (result == -3)
            {
                apiResult.ErrorCode = result;
                apiResult.Message = _selLangBll.GetMsg(serverInfo.SiteId == (int)SiteId.Nova88 ? "msg_PasswordValidate" : "msg_PasswordValidate2");
            }
            else if (result == -4)
            {
                apiResult.ErrorCode = result;
                apiResult.Message = "";
            }
            else if (result == -5)
            {
                apiResult.ErrorCode = result;
                apiResult.Message = _selLangBll.GetMsg("msg_UsernameNotAvailable");
            }
            else if (result == -19)
            {
                apiResult.ErrorCode = result;
                apiResult.Message = _selLangBll.GetMsg("mag_AffiliateNotExist");
            }
            else if (custId == 0)
            {
                apiResult.ErrorCode = result;
                apiResult.Message = _selLangBll.GetMsg("msg_UsernameNotAvailable");
            }
            else if (result != 0)
            {
                apiResult.ErrorCode = result;
                apiResult.Message = _selLangBll.GetMsg("msg_UsernameNotAvailable");
            }
            else
            {
                _ = Task.Run(async () =>
                {
                    _commonBll.SendWelcomeMail(custId);

                    if (registeredModel.blackbox != null)
                    {
                        var apiResultIovation = await _iovationBll.IovationChecks(custId, registeredModel.CurrencyId, registeredModel.UserName, registeredModel.blackbox, IovationType.registration);
                        if (apiResultIovation.ErrorCode == 0)
                            _iovationBll.SetIovation(custId, apiResultIovation.Message, IovationType.registration);
                    }
                });

                Customer customer = null;
                string token = null;
                var userLoginResult = _custInfoBll.UserLogin(registeredModel.UserName, registeredModel.Userpwd, _tools.GetClientIP(), _tools.GetFromUrl(), Request.Headers["User-Agent"], ref customer, out token);

                if (userLoginResult == -1)
                {
                    apiResult.ErrorCode = 1101;
                    apiResult.Message = _selLangBll.GetMsg("msg_DepositProcessLogin_LoginFailed");
                }
                else if (userLoginResult == -2)
                {
                    apiResult.ErrorCode = 1001;
                    apiResult.Message = _selLangBll.GetMsg("msg_DepositProcessLogin_LoginFailed");
                }
                else if (userLoginResult == -3)
                {
                    apiResult.ErrorCode = 1102;
                    apiResult.Message = _selLangBll.GetMsg("msg_DepositProcessLogin_ACLocked");
                }
                else if (userLoginResult != 0)
                {
                    apiResult.ErrorCode = 9000;
                }
                else
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name,customer.UserName ),
                        new Claim(ClaimTypes.NameIdentifier,customer.CustId.ToString() ),
                        new Claim("Token",token ),
                        new Claim("LangId",customer.UserLang )
                    };
                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));
                    apiResult.ErrorCode = 0;
                    apiResult.Message = Url.RouteUrl("desktop", new { action = "Index", controller = "Home", LangId = _selLangBll.GetLangId() });
                }
            }

            return apiResult;
        }

        public class SignupModel : RegisteredModel
        {
            public string SignupUrl { get; set; }
            public string PhoneNo { get; set; }
            public string AreaCode { get; set; }
            public string RealName { get; set; }
            public string PromoCode { get; set; }
            public int Birth_d { get; set; }
            public int Birth_m { get; set; }
            public int Birth_y { get; set; }
        }

        [HttpPost]
        public async Task<ApiResult> SignupAsync(SignupModel signupModel)
        {
            var apiResult = new ApiResult(_selLangBll);
            var serverInfo = _serverInfoBll.GetServerInfo();
            if (User.Identity.IsAuthenticated || (string.IsNullOrEmpty(signupModel.AreaCode) && !string.IsNullOrEmpty(signupModel.PhoneNo)))
            {
                apiResult.ErrorCode = 9000;
                return apiResult;
            }

            if (!_regexUserName.IsMatch(signupModel.UserName))
            {
                apiResult.ErrorCode = -8;
                apiResult.Message = _selLangBll.GetMsg("msg_UsernameValidate");
                return apiResult;
            }

            if (!string.IsNullOrEmpty(signupModel.AffCode?.Trim()))
            {
                signupModel.AffCode = signupModel.AffCode.Trim();
                if (!_regexAffCode.IsMatch(signupModel.AffCode))
                {
                    apiResult.ErrorCode = -9;
                    apiResult.Message = _selLangBll.GetMsg("msg_AffCodeValidate");
                    return apiResult;
                }
            }

            if (string.IsNullOrEmpty(signupModel.SignupUrl))
            {
                signupModel.SignupUrl = Request.Host.Host;
            }

            // URL tag with specific affcode
            if((Request.Host.Host == "www.nova88fun.com") || (Request.Host.Host == "m.nova88fun.com"))
            {
                signupModel.AffCode = "nova88tvads";
            }

            var custId = 0;
            var result = -99;
            var phoneExists = false;
            var mailExists = false;

            if (long.TryParse(signupModel.PhoneNo, out var phoneNo))
            {
                signupModel.PhoneNo = phoneNo.ToString();
                phoneExists = _custInfoBll.ExistsPhoneNo(0, signupModel.AreaCode, signupModel.PhoneNo);
                if (phoneExists)
                    result = -6;
            }

            if (!string.IsNullOrWhiteSpace(signupModel.Email))
            {
                mailExists = _custInfoBll.ExistsEmail(0, signupModel.Email);
                if (mailExists)
                    result = -10;
            }

            if (!phoneExists && !mailExists)
            {
                result = _custInfoBll.AddCustInfo(signupModel.UserName, signupModel.Userpwd, signupModel.Email,
                    signupModel.CurrencyId, signupModel.UserLang, signupModel.RefId, signupModel.AffId, signupModel.Aff,
                    signupModel.AffCode, serverInfo.SiteId, out custId, signupModel.SignupUrl, signupModel.PhoneNo, signupModel.AreaCode,
                    signupModel.RealName, signupModel.PromoCode, signupModel.Birth_d, signupModel.Birth_m,
                    signupModel.Birth_y);
            }

            switch (result)
            {
                case -1:
                    apiResult.ErrorCode = result;
                    apiResult.Message = _selLangBll.GetMsg("err_ThisCurrencyNotSupport");
                    break;
                case -2:
                    apiResult.ErrorCode = result;
                    apiResult.Message = _selLangBll.GetMsg("msg_UsernameValidate");
                    break;
                case -3:
                    apiResult.ErrorCode = result;
                    apiResult.Message = _selLangBll.GetMsg(serverInfo.SiteId == (int)SiteId.Nova88 ? "msg_PasswordValidate" : "msg_PasswordValidate2");
                    break;
                case -4:
                    apiResult.ErrorCode = result;/**/
                    apiResult.Message = "";
                    break;
                case -5:
                    apiResult.ErrorCode = result;
                    apiResult.Message = _selLangBll.GetMsg("msg_UsernameNotAvailable");
                    break;
                case -6:
                    apiResult.ErrorCode = result;
                    apiResult.Message = $"{_selLangBll.GetResourceValue("lbl_Phone")} {_selLangBll.GetMsg("msg_HasExist")}";
                    break;
                case -7:
                    apiResult.ErrorCode = result;
                    apiResult.Message = _selLangBll.GetMsg("msg_PromoCodeIncorrect");
                    break;
                case -10:
                    apiResult.ErrorCode = result;
                    apiResult.Message = $"{_selLangBll.GetResourceValue("lbl_Email")} {_selLangBll.GetMsg("msg_HasExist")}";
                    break;
                case -19:
                    apiResult.ErrorCode = result;
                    apiResult.Message = _selLangBll.GetMsg("mag_AffiliateNotExist");
                    break;
                default:
                    {
                        if (custId == 0)
                        {
                            apiResult.ErrorCode = result;
                            apiResult.Message = _selLangBll.GetMsg("msg_UsernameNotAvailable");
                        }
                        else if (result != 0)
                        {
                            apiResult.ErrorCode = result;
                            apiResult.Message = _selLangBll.GetMsg("msg_UsernameNotAvailable");
                        }
                        else
                        {
                            _ = Task.Run(async () =>
                            {
                                _commonBll.SendWelcomeMail(custId);

                                if (signupModel.blackbox != null)
                                {
                                    var apiResultIovation = await _iovationBll.IovationChecks(custId, signupModel.CurrencyId, signupModel.UserName, signupModel.blackbox, IovationType.registration);
                                    if (apiResultIovation.ErrorCode == 0)
                                        _iovationBll.SetIovation(custId, apiResultIovation.Message, IovationType.registration);
                                }
                            });

                            Customer customer = null;
                            var userLoginResult = _custInfoBll.UserLogin(signupModel.UserName, signupModel.Userpwd, _tools.GetClientIP(), _tools.GetFromUrl(), Request.Headers["User-Agent"], ref customer, out var token);

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
                                        else
                                        {
                                            if (!string.IsNullOrEmpty(signupModel.RealName))
                                            {
                                                _custInfoBll.SetCustBankInfo(custId, signupModel.RealName, null);
                                            }

                                            var claims = new List<Claim>
                                    {
                                        new Claim(ClaimTypes.Name,customer.UserName ),
                                        new Claim(ClaimTypes.NameIdentifier,customer.CustId.ToString() ),
                                        new Claim("Token",token ),
                                        new Claim("LangId",customer.UserLang )
                                    };

                                            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                                            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));
                                            apiResult.ErrorCode = 0;
                                            return apiResult;
                                        }

                                        break;
                                    }
                            }
                        }

                        break;
                    }
            }

            return apiResult;
        }

        [HttpPost]
        public ApiResult<JwtAuthData> SignupApp(SignupModel signupModel)
        {
            var apiResult = new ApiResult<JwtAuthData>(_selLangBll);
            var serverInfo = _serverInfoBll.GetServerInfo();
            if (User.Identity.IsAuthenticated || (string.IsNullOrEmpty(signupModel.AreaCode) && !string.IsNullOrEmpty(signupModel.PhoneNo)))
            {
                apiResult.ErrorCode = 9000;
                return apiResult;
            }

            if (!_regexUserName.IsMatch(signupModel.UserName))
            {
                apiResult.ErrorCode = -8;
                apiResult.Message = _selLangBll.GetMsg("msg_UsernameValidate");
                return apiResult;
            }

            if (!string.IsNullOrEmpty(signupModel.AffCode?.Trim()))
            {
                signupModel.AffCode = signupModel.AffCode.Trim();
                if (!_regexAffCode.IsMatch(signupModel.AffCode))
                {
                    apiResult.ErrorCode = -9;
                    apiResult.Message = _selLangBll.GetMsg("msg_AffCodeValidate");
                    return apiResult;
                }
            }

            var custId = 0;
            var result = -99;
            var phoneExists = false;
            var mailExists = false;

            if (long.TryParse(signupModel.PhoneNo, out var phoneNo))
            {
                signupModel.PhoneNo = phoneNo.ToString();
                phoneExists = _custInfoBll.ExistsPhoneNo(0, signupModel.AreaCode, signupModel.PhoneNo);
                if (phoneExists)
                    result = -6;
            }

            if (!string.IsNullOrWhiteSpace(signupModel.Email))
            {
                mailExists = _custInfoBll.ExistsEmail(0, signupModel.Email);
                if (mailExists)
                    result = -10;
            }
            
            if (!phoneExists && !mailExists)
            {
                result = _custInfoBll.AddCustInfo(signupModel.UserName, signupModel.Userpwd, signupModel.Email,
                    signupModel.CurrencyId, signupModel.UserLang, signupModel.RefId, signupModel.AffId, signupModel.Aff,
                    signupModel.AffCode, serverInfo.SiteId, out custId, signupModel.SignupUrl, signupModel.PhoneNo, signupModel.AreaCode,
                    signupModel.RealName, signupModel.PromoCode);
            }

            switch (result)
            {
                case -1:
                    apiResult.ErrorCode = result;
                    apiResult.Message = _selLangBll.GetMsg("err_ThisCurrencyNotSupport");
                    break;
                case -2:
                    apiResult.ErrorCode = result;
                    apiResult.Message = _selLangBll.GetMsg("msg_UsernameValidate");
                    break;
                case -3:
                    apiResult.ErrorCode = result;
                    apiResult.Message = _selLangBll.GetMsg(serverInfo.SiteId == (int)SiteId.Nova88 ? "msg_PasswordValidate" : "msg_PasswordValidate2");
                    break;
                case -4:
                    apiResult.ErrorCode = result;/**/
                    apiResult.Message = "";
                    break;
                case -5:
                    apiResult.ErrorCode = result;
                    apiResult.Message = _selLangBll.GetMsg("msg_UsernameNotAvailable");
                    break;
                case -6:
                    apiResult.ErrorCode = result;
                    apiResult.Message = $"{_selLangBll.GetResourceValue("lbl_Phone")} {_selLangBll.GetMsg("msg_HasExist")}";
                    break;
                case -7:
                    apiResult.ErrorCode = result;
                    apiResult.Message = _selLangBll.GetMsg("msg_PromoCodeIncorrect");
                    break;
                case -10:
                    apiResult.ErrorCode = result;
                    apiResult.Message = $"{_selLangBll.GetResourceValue("lbl_Email")} {_selLangBll.GetMsg("msg_HasExist")}";
                    break;
                case -19:
                    apiResult.ErrorCode = result;
                    apiResult.Message = _selLangBll.GetMsg("mag_AffiliateNotExist");
                    break;
                default:
                    {
                        if (custId == 0)
                        {
                            apiResult.ErrorCode = result;
                            apiResult.Message = _selLangBll.GetMsg("msg_UsernameNotAvailable");
                        }
                        else if (result != 0)
                        {
                            apiResult.ErrorCode = result;
                            apiResult.Message = _selLangBll.GetMsg("msg_UsernameNotAvailable");
                        }
                        else
                        {
                            _ = Task.Run(async () =>
                            {
                                _commonBll.SendWelcomeMail(custId);

                                if (signupModel.blackbox != null)
                                {
                                    var apiResultIovation = await _iovationBll.IovationChecks(custId, signupModel.CurrencyId, signupModel.UserName, signupModel.blackbox, IovationType.registration);
                                    if (apiResultIovation.ErrorCode == 0)
                                        _iovationBll.SetIovation(custId, apiResultIovation.Message, IovationType.registration);
                                }
                            });

                            Customer customer = null;
                            var userLoginResult = _custInfoBll.UserLogin(signupModel.UserName, signupModel.Userpwd, _tools.GetClientIP(), _tools.GetFromUrl(), Request.Headers["User-Agent"], ref customer, out var token);

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
                                        else
                                        {
                                            if (!string.IsNullOrEmpty(signupModel.RealName))
                                            {
                                                _custInfoBll.SetCustBankInfo(custId, signupModel.RealName, null);
                                            }

                                            var claims = new[] {
                                        new Claim(ClaimTypes.Name,customer.UserName ),
                                        new Claim(ClaimTypes.NameIdentifier,customer.CustId.ToString() ),
                                        new Claim("Token",token ),
                                        new Claim("LangId",customer.UserLang ),
                                        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString() )
                                    };


                                            var accessToken = _tools.GenerateJwtTokens(claims);
                                            var data = new JwtAuthData
                                            {
                                                AccessToken = accessToken
                                            };

                                            apiResult.Data = data;
                                            apiResult.Message = "";
                                            apiResult.ErrorCode = 0;
                                            apiResult.ErrorCode = 0;
                                            return apiResult;
                                        }

                                        break;
                                    }
                            }
                        }

                        break;
                    }
            }

            return apiResult;
        }

        [HttpPost]
        public ApiResult<bool> ExistsUserName(RegisteredModel registeredModel)
        {
            var apiResult = new ApiResult<bool>(_selLangBll);
            if (!_regexUserName.IsMatch(registeredModel.UserName))
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

        //[HttpOptions]
        //public IActionResult ExistsUserName()
        //{
        //    return NoContent();
        //}
        public ApiResult<bool> ExistsEmail(RegisteredModel registeredModel)
        {
            var apiResult = new ApiResult<bool>(_selLangBll)
            {
                Data = _custInfoBll.ExistsEmail(0, registeredModel.Email),
                ErrorCode = 0
            };
            return apiResult;
        }

        public class TpInfoNologin
        {
            public int TpId { set; get; }
            public Dictionary<string, string> inputs { set; get; }
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

                Customer custInfo = null;

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

                var resetPasswordUrl = Url.RouteUrl("desktop", new { action = "", controller = "resetPassword", LangId = forgetPwdReq.flag == 1 ? _selLangBll.GetLangId() : "" }, protocol: Url.ActionContext.HttpContext.Request.Scheme, host: Url.ActionContext.HttpContext.Request.Host.Host);

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
                if (User.Identity.IsAuthenticated)
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

        public ApiResult<List<Provider>> GetGameList()
        {
            var apiResult = new ApiResult<List<Provider>>();

            var providers = new List<Provider> {
                new Provider { Value = 6, Name = "GPI", DisplayText = _selLangBll.GetResourceValue("lbl_GPI"), PlayFree = true, DisplayKey = "lbl_GPI" },
                new Provider { Value = 8, Name = "YGG", DisplayText = "YGG", PlayFree = true },
                new Provider { Value = 9, Name = "Joker", DisplayText = "Joker", PlayFree = false },
                new Provider { Value = 10, Name = "SG", DisplayText = "SG", PlayFree = true },
                new Provider { Value = 5, Name = "PP", DisplayText = "PP", PlayFree = true },
                new Provider { Value = 14, Name = "PG", DisplayText = "PG", PlayFree = true },
                new Provider { Value = 16, Name = "CQ9", DisplayText = "CQ9", PlayFree = true },
                new Provider { Value = 24, Name = "Iconic", DisplayText = "ICG", PlayFree = true }
            };

            IEnumerable<int> tpIds = new int[] { };

            if (User.Identity.IsAuthenticated)
            {
                var authData = _authorizeBll.GetAuthData();
                var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
                tpIds = _casinoBll.GetProviderIdByCurrency(custInfo.CurrencyId, custInfo.SiteId);
            }
            else
            {
                var serverInfo = _serverInfoBll.GetServerInfo();
                tpIds = _casinoBll.GetProviderIdByCurrency(0, serverInfo.SiteId);
            }

            apiResult.Data = providers?.Where(x => tpIds.Contains(x.Value)).ToList();
            foreach (var provider in providers)
            {
                var datePath = Path.Combine(_environment.WebRootPath, "datas", provider.Name, "GameList.json");
                if (System.IO.File.Exists(datePath))
                    provider.GameList = JsonConvert.DeserializeObject<GameDesc>(System.IO.File.ReadAllText(datePath));
            }
            apiResult.ErrorCode = 0;
            return apiResult;
        }

        public ApiResult<IEnumerable<int>> GetTpIds()
        {
            var apiResult = new ApiResult<IEnumerable<int>>();
            if (User.Identity.IsAuthenticated)
            {
                var authData = _authorizeBll.GetAuthData();
                var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
                apiResult.Data = _casinoBll.GetProviderIdByCurrency(custInfo.CurrencyId, custInfo.SiteId);
            }
            else
            {
                var serverInfo = _serverInfoBll.GetServerInfo();
                apiResult.Data = _casinoBll.GetProviderIdByCurrency(0, serverInfo.SiteId);
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
            var _bannerSetting = new BannerSetting();
            if (System.IO.File.Exists(dataPath))
            {
                _bannerSetting = JsonConvert.DeserializeObject<BannerSetting>(System.IO.File.ReadAllText(dataPath));
            }

            var perferCurr = 0;
            var beforeLogin = true;
            if (User.Identity.IsAuthenticated)
            {
                var authData = _authorizeBll.GetAuthData();
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
                var banner_CurrencyId = !User.Identity.IsAuthenticated && perferCurr == 13 ? 2 : perferCurr;
                //var bannerFromDB = bannerBLL.GetBannerFromDB(banner_CurrencyId, selLangBLL.GetLangId());
                var bannerFromJSON = _bannerSetting.Promotion.Where(x => x.currencyId == null || beforeLogin || x.currencyId.Any(y => y == perferCurr));
                //result.Data = bannerFromJSON.Concat(bannerFromDB);
                result.Data = bannerFromJSON;

                if (filterHideInHome)
                {
                    result.Data = result.Data.Where(x => !x.hideInHome);
                }
            }
            else if (FolderName == "Casino")
            {
                result.Data = _bannerSetting.Casino.Where(x => x.currencyId == null || beforeLogin || x.currencyId.Any(y => y == perferCurr));
            }
            else if (FolderName == "Sports")
            {
                result.Data = _bannerSetting.Sports.Where(x => x.currencyId == null || beforeLogin || x.currencyId.Any(y => y == perferCurr));
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
        }

        [HttpPost]
        public ApiResult<IEnumerable<BannerV2>> GetBannerV2(BannerReq bannerReq)
        {
            var apiResult = new ApiResult<IEnumerable<BannerV2>>();

            var serverInfo = _serverInfoBll.GetServerInfo();
            var perferCurr = 0;
            var SiteId = 0;
            var ShowStatus = 1;
            if (User.Identity.IsAuthenticated)
            {
                var authData = _authorizeBll.GetAuthData();
                var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
                SiteId = custInfo.SiteId;
                perferCurr = custInfo.CurrencyId;
                ShowStatus = 2;
            }
            else
            {
                perferCurr = _selLangBll.GetPreferCurrencyId();
                SiteId = serverInfo.SiteId;
            }

            var banner_CurrencyId = !User.Identity.IsAuthenticated && perferCurr == 13 ? 2 : perferCurr;

            apiResult.ErrorCode = 0;
            apiResult.Data = _bannerBll.GetBannerV2(SiteId, bannerReq.BannerType, ShowStatus, banner_CurrencyId, _selLangBll.GetLangId());

            return apiResult;
        }

        [HttpPost]
        public ApiResult<IEnumerable<BannerV3>> GetBannerV3(BannerReq bannerReq)
        {
            var apiResult = new ApiResult<IEnumerable<BannerV3>>();

            var serverInfo = _serverInfoBll.GetServerInfo();
            var perferCurr = 0;
            var SiteId = 0;
            var ShowStatus = 1;
            if (User.Identity.IsAuthenticated)
            {
                var authData = _authorizeBll.GetAuthData();
                var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
                SiteId = custInfo.SiteId;
                perferCurr = custInfo.CurrencyId;
                ShowStatus = 2;
            }
            else
            {
                perferCurr = _selLangBll.GetPreferCurrencyId();
                SiteId = serverInfo.SiteId;
            }

            var banner_CurrencyId = !User.Identity.IsAuthenticated && perferCurr == 13 ? 2 : perferCurr;

            apiResult.ErrorCode = 0;
            apiResult.Data = _bannerBll.GetBannerV3(SiteId, bannerReq.BannerType, ShowStatus, banner_CurrencyId, _selLangBll.GetLangId(), serverInfo);

            return apiResult;
        }

        public class HotGameReq
        {
            public int DeviceType { get; set; }
        }

        [HttpPost]
        public async Task<ApiResult<IEnumerable<HotGame>>> GetHotGames(HotGameReq req)
        {
            var domain = _serverInfoBll.GetMainDomain();
            var data = _serverInfoBll.GetSeoWithCache(domain);

            int currencyId;
            int siteId;
            if (data != null && !string.IsNullOrEmpty(data.DefaultLang))
            {
                currencyId = _selLangBll.GetPreferCurrencyIdByLang(data.DefaultLang);
                siteId = data.SiteId;
            }
            else
            {
                var serverInfo = _serverInfoBll.GetServerInfo();
                siteId = serverInfo.SiteId;
                currencyId = _selLangBll.GetPreferCurrencyId();
                currencyId = currencyId == 13 ? 2 : currencyId;
            }
            
            var apiResult = new ApiResult<IEnumerable<HotGame>>
            {
                ErrorCode = 0, 
                Data = await _bannerBll.GetHotGames(siteId, currencyId, req.DeviceType)
            };

            return apiResult;
        }

        [HttpPost]
        public async Task<ApiResult> CheckResetPwdTokenAsync([FromForm] string key)
        {
            var apiResult = new ApiResult(_selLangBll);
            if (_authBll.Authenticate(key, out CustInfoProfile custInfoProfile))
            {
                Customer customer = null;
                var userLoginResult = _custInfoBll.UserLoginForResetPwd(custInfoProfile.UserName, _tools.GetClientIP(), _tools.GetFromUrl(), Request.Headers["User-Agent"], ref customer, out var token);
                switch (userLoginResult)
                {
                    case 0:
                    {
                        if (User.Identity.IsAuthenticated && _authorizeBll.GetAuthData().CustId != customer.CustId)
                        {
                            apiResult.ErrorCode = 1;
                        }
                        else
                        {
                            var claims = new List<Claim>
                            {
                                new Claim(ClaimTypes.Name, custInfoProfile.UserName),
                                new Claim(ClaimTypes.NameIdentifier, customer.CustId.ToString()),
                                new Claim("Token", token),
                                new Claim("LangId", custInfoProfile.UserLang)
                            };
                            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));
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
        public ApiResult<GameInfo> GetGameListV2()
        {
            var apiResult = new ApiResult<GameInfo>();
            var resultData = new GameInfo
            {
                ProviderList = new List<Provider>()
            };

            //不分Tpid取出GameCasino資料
            var GameCasinoList = _gameCasinoBll.GetGameCasinoList();

            //GameCasinoList裡總共有多少種TpId
            var GameCasinoHasTpid = GameCasinoList.Select(x => x.TpId).Distinct();
            var providers = _gameCasinoBll.GetProviderList();

            IEnumerable<int> tpIds = null;
            IEnumerable<GameCasionByCurrencyAndSite> gameCasionByCurrencyAndSite = null;

            var serverInfo = _serverInfoBll.GetServerInfo();
            if (User.Identity.IsAuthenticated)
            {
                var authData = _authorizeBll.GetAuthData();
                var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
                tpIds = _casinoBll.GetProviderIdByCurrency(custInfo.CurrencyId, custInfo.SiteId);
                //取出有byCurrency跟Site設定的遊戲資訊
                gameCasionByCurrencyAndSite = _casinoBll.GetGameCasinoByCurrencyAndSite(custInfo.CurrencyId, custInfo.SiteId);
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
            if (User.Identity.IsAuthenticated)
            {
                var authData = _authorizeBll.GetAuthData();
                var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
                tpIds = _casinoBll.GetProviderIdByCurrency(custInfo.CurrencyId, custInfo.SiteId);
                //取出有byCurrency跟Site設定的遊戲資訊
                gameCasionByCurrencyAndSite = _casinoBll.GetGameCasinoByCurrencyAndSite(custInfo.CurrencyId, custInfo.SiteId);
                
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
            var result = _custInfoBll.GetAffiliateAgentBll(affiliateAgentReq.AffId);
            apiResult.ErrorCode = 0;
            if (result > 0)
            {
                apiResult.Data.CurrencyId = result;
            }
            else if (result == -1)
            {
                apiResult.ErrorCode = result;
                apiResult.Message = "no AffId";
            }
            else if (result == -19)
            {
                apiResult.ErrorCode = result;
                apiResult.Message = _selLangBll.GetMsg("mag_AffiliateNotExist");
            }
            else
            {
                apiResult.ErrorCode = result;
            }

            return apiResult;
        }

        [HttpPost]
        public ApiResult<IEnumerable<Domainlist>> GetSupportDomainList()
        {
            var apiResult = new ApiResult<IEnumerable<Domainlist>>();
            var siteId = 0;
            var currencyId = 0;
            if (User.Identity.IsAuthenticated)
            {
                var authData = _authorizeBll.GetAuthData();
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
        
        [HttpGet, HttpPost]
        public ApiResult<SeoModel> Seo()
        {
            var domain = _serverInfoBll.GetMainDomain();
            var data = _serverInfoBll.GetSeoWithCache(domain);

            if (data == null ||
                string.IsNullOrEmpty(data.Meta) ||
                string.IsNullOrEmpty(data.Title))
            {
                data = null;
            }

            return new ApiResult<SeoModel>()
            {
                ErrorCode = 0,
                Data = data
            };
        }

        [HttpPost]
        public async Task<ApiResult<IEnumerable<MenuOfGame>>> GetMenuOfGames()
        {
            var domain = _serverInfoBll.GetMainDomain();
            var data = _serverInfoBll.GetSeoWithCache(domain);
            var apiResult = new ApiResult<IEnumerable<MenuOfGame>>()
            {
                ErrorCode = 0
            };
            if (data != null && !string.IsNullOrEmpty(data.DefaultLang))
            {
                apiResult.Data = await _serverInfoBll.GetMenuOfGames(data.SiteId, data.DefaultLang);
            }

            return apiResult;
        }
    }
}