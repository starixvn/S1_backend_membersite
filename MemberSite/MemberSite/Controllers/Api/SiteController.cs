using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MemberSite.Service.BLL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;

namespace MemberSite.Controllers.Api
{
    [AllowAnonymous]
    [Route("api/[controller]/[action]")]
    [Route("api/[controller]/[action]/{LangId}")]
    [ApiController]
    public class SiteController : ControllerBase
    {
        private readonly CustInfoBLL custInfoBLL;
        private readonly Tools tools;
        private readonly SelLangBLL selLangBLL;
        private readonly CommonBLL commonBLL;
        private readonly IWebHostEnvironment environment;
        private readonly AuthorizeBLL authorizeBLL;
        private readonly CasinoBLL casinoBLL;
        private readonly ServerInfoBLL serverInfoBLL;
        private readonly BannerBLL bannerBLL;
        private readonly AuthBLL authBLL;

        public SiteController(IWebHostEnvironment environment, CustInfoBLL custInfoBLL, SelLangBLL selLangBLL, Tools tools, CommonBLL commonBLL, AuthorizeBLL authorizeBLL, CasinoBLL casinoBLL, ServerInfoBLL serverInfoBLL, BannerBLL bannerBLL, AuthBLL authBLL)
        {
            this.custInfoBLL = custInfoBLL;
            this.tools = tools;
            this.selLangBLL = selLangBLL;
            this.commonBLL = commonBLL;
            this.environment = environment;
            this.authorizeBLL = authorizeBLL;
            this.casinoBLL = casinoBLL;
            this.serverInfoBLL = serverInfoBLL;
            this.bannerBLL = bannerBLL;
            this.authBLL = authBLL;
        }

        public class LoginModel
        {
            public string UserName { get; set; }
            public string Userpwd { get; set; }
            public bool isPersistent { get; set; }
        }

        [HttpPost]
        public async Task<ApiResult> SignIn(LoginModel loginModel)
        {
            var apiResult = new ApiResult(selLangBLL);
            string userName = loginModel.UserName;
            string userpwd = loginModel.Userpwd;
            Customer customer = null;
            string token = null;
            var userLoginResult = custInfoBLL.UserLogin(userName, userpwd, tools.GetClientIP(), tools.GetFromUrl(), Request.Headers["User-Agent"], ref customer, out token);

            if (userLoginResult == -1)
            {
                apiResult.ErrorCode = 1101;
                apiResult.Message = selLangBLL.GetMsg("msg_DepositProcessLogin_LoginFailed");
            }
            else if (userLoginResult == -2)
            {
                apiResult.ErrorCode = 1001;
                apiResult.Message = selLangBLL.GetMsg("msg_DepositProcessLogin_LoginFailed");
            }
            else if (userLoginResult == -3)
            {
                apiResult.ErrorCode = 1102;
                apiResult.Message = selLangBLL.GetMsg("msg_DepositProcessLogin_ACLocked");
            }
            else if (userLoginResult != 0)
            {
                apiResult.ErrorCode = 9000;
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

            if (tools.IsMobile())
            {
                authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
                };
            }

            Response.Cookies.Delete($"Initialization{User.Identity.Name}");
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
            apiResult.Message = Url.RouteUrl("desktop", new { action = "Index", controller = "Home", LangId = selLangBLL.GetLangId() });
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
        }

        [HttpPost]
        public async Task<ApiResult> RegisterAsync(RegisteredModel registeredModel)
        {
            var apiResult = new ApiResult(selLangBLL);
            if (User.Identity.IsAuthenticated)
            {
                apiResult.ErrorCode = 9000;
                return apiResult;
            }

            int custId = 0;

            var result = custInfoBLL.AddCustInfo(registeredModel.UserName, registeredModel.Userpwd, registeredModel.Email, registeredModel.CurrencyId, registeredModel.UserLang, registeredModel.RefId, registeredModel.AffId, registeredModel.Aff, out custId, Request.Host.Host);
            if (result == -1)
            {
                apiResult.ErrorCode = result;
                apiResult.Message = selLangBLL.GetMsg("err_ThisCurrencyNotSupport");
            }
            else if (result == -2)
            {
                apiResult.ErrorCode = result;
                apiResult.Message = selLangBLL.GetMsg("msg_UsernameReq");
            }
            else if (result == -3)
            {
                apiResult.ErrorCode = result;
                apiResult.Message = selLangBLL.GetMsg("msg_PasswordReq");
            }
            else if (result == -4)
            {
                apiResult.ErrorCode = result;/**/
                apiResult.Message = "";
            }
            else if (result == -5)
            {
                apiResult.ErrorCode = result;
                apiResult.Message = selLangBLL.GetMsg("msg_UsernameNotAvailable");
            }
            else if (result == -19)
            {
                apiResult.ErrorCode = result;
                apiResult.Message = selLangBLL.GetMsg("mag_AffiliateNotExist");
            }
            else if (custId == 0)
            {
                apiResult.ErrorCode = result;
                apiResult.Message = selLangBLL.GetMsg("msg_UsernameNotAvailable");
            }
            else if (result != 0)
            {
                apiResult.ErrorCode = result;
                apiResult.Message = selLangBLL.GetMsg("msg_UsernameNotAvailable");
            }
            else
            {
                commonBLL.SendWelcomeMail(custId);

                Customer customer = null;
                string token = null;
                var userLoginResult = custInfoBLL.UserLogin(registeredModel.UserName, registeredModel.Userpwd, tools.GetClientIP(), tools.GetFromUrl(), Request.Headers["User-Agent"], ref customer, out token);

                if (userLoginResult == -1)
                {
                    apiResult.ErrorCode = 1101;
                    apiResult.Message = selLangBLL.GetMsg("msg_DepositProcessLogin_LoginFailed");
                }
                else if (userLoginResult == -2)
                {
                    apiResult.ErrorCode = 1001;
                    apiResult.Message = selLangBLL.GetMsg("msg_DepositProcessLogin_LoginFailed");
                }
                else if (userLoginResult == -3)
                {
                    apiResult.ErrorCode = 1102;
                    apiResult.Message = selLangBLL.GetMsg("msg_DepositProcessLogin_ACLocked");
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
                    apiResult.Message = Url.RouteUrl("desktop", new { action = "Index", controller = "Home", LangId = selLangBLL.GetLangId() });
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
        }

        [HttpPost]
        public async Task<ApiResult> SignupAsync(SignupModel signupModel)
        {
            var apiResult = new ApiResult(selLangBLL);
            if (User.Identity.IsAuthenticated || (string.IsNullOrEmpty(signupModel.AreaCode) && !string.IsNullOrEmpty(signupModel.PhoneNo)))
            {
                apiResult.ErrorCode = 9000;
                return apiResult;
            }

            int custId = 0;
            var phoneExists = custInfoBLL.ExistsPhoneNo(0, signupModel.AreaCode, signupModel.PhoneNo);
            var result = 0;
            if (!phoneExists)
            {
                result = custInfoBLL.AddCustInfo(signupModel.UserName, signupModel.Userpwd, signupModel.Email, signupModel.CurrencyId, signupModel.UserLang, signupModel.RefId, signupModel.AffId, signupModel.Aff, out custId, signupModel.SignupUrl, signupModel.PhoneNo, signupModel.AreaCode, signupModel.RealName);
            }
            else
            {
                apiResult.ErrorCode = -6;
                apiResult.Message = selLangBLL.GetResourceValue("lbl_Phone") + " " + selLangBLL.GetMsg("msg_HasExist");
            }

            if (result == -1)
            {
                apiResult.ErrorCode = result;
                apiResult.Message = selLangBLL.GetMsg("err_ThisCurrencyNotSupport");
            }
            else if (result == -2)
            {
                apiResult.ErrorCode = result;
                apiResult.Message = selLangBLL.GetMsg("msg_UsernameReq");
            }
            else if (result == -3)
            {
                apiResult.ErrorCode = result;
                apiResult.Message = selLangBLL.GetMsg("msg_PasswordReq");
            }
            else if (result == -4)
            {
                apiResult.ErrorCode = result;/**/
                apiResult.Message = "";
            }
            else if (result == -5)
            {
                apiResult.ErrorCode = result;
                apiResult.Message = selLangBLL.GetMsg("msg_UsernameNotAvailable");
            }
            else if (result == -19)
            {
                apiResult.ErrorCode = result;
                apiResult.Message = selLangBLL.GetMsg("mag_AffiliateNotExist");
            }
            else if (custId == 0)
            {
                apiResult.ErrorCode = result;
                apiResult.Message = selLangBLL.GetMsg("msg_UsernameNotAvailable");
            }
            else if (result != 0)
            {
                apiResult.ErrorCode = result;
                apiResult.Message = selLangBLL.GetMsg("msg_UsernameNotAvailable");
            }
            else
            {
                commonBLL.SendWelcomeMail(custId);

                Customer customer = null;
                string token = null;
                var userLoginResult = custInfoBLL.UserLogin(signupModel.UserName, signupModel.Userpwd, tools.GetClientIP(), tools.GetFromUrl(), Request.Headers["User-Agent"], ref customer, out token);

                if (userLoginResult == -1)
                {
                    apiResult.ErrorCode = 1101;
                    apiResult.Message = selLangBLL.GetMsg("msg_DepositProcessLogin_LoginFailed");
                }
                else if (userLoginResult == -2)
                {
                    apiResult.ErrorCode = 1001;
                    apiResult.Message = selLangBLL.GetMsg("msg_DepositProcessLogin_LoginFailed");
                }
                else if (userLoginResult == -3)
                {
                    apiResult.ErrorCode = 1102;
                    apiResult.Message = selLangBLL.GetMsg("msg_DepositProcessLogin_ACLocked");
                }
                else if (userLoginResult != 0)
                {
                    apiResult.ErrorCode = 9000;
                }
                else
                {
                    if (!string.IsNullOrEmpty(signupModel.RealName))
                    {
                        custInfoBLL.SetCustBankInfo(custId, signupModel.RealName, null);
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
            }

            return apiResult;
        }

        [HttpPost]
        public ApiResult<bool> ExistsUserName(RegisteredModel registeredModel)
        {
            var apiResult = new ApiResult<bool>(selLangBLL);
            apiResult.Data = custInfoBLL.ExistsUserName(registeredModel.UserName);
            apiResult.ErrorCode = 0;
            return apiResult;
        }
        public ApiResult<bool> ExistsEmail(RegisteredModel registeredModel)
        {
            var apiResult = new ApiResult<bool>(selLangBLL);
            apiResult.Data = custInfoBLL.ExistsEmail(0, registeredModel.Email);
            apiResult.ErrorCode = 0;
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
            var apiResult = new ApiResult<bool>(selLangBLL);
            try
            {

                Customer custInfo = null;

                var resetPasswordUrl = Url.RouteUrl("desktop", new { action = "ResetPassword", controller = "Home", LangId = selLangBLL.GetLangId() }, protocol: Url.ActionContext.HttpContext.Request.Scheme, host: Url.ActionContext.HttpContext.Request.Host.Host);
                if (!string.IsNullOrEmpty(forgetPwdReq.Username))
                {
                    custInfo = custInfoBLL.GetCustInfoByUserName(forgetPwdReq.Username.Trim());
                }
                else
                {
                    custInfo = custInfoBLL.GetCustInfoByMail(forgetPwdReq.Email.Trim());
                }

                if (custInfo == null)
                {
                    apiResult.Message = selLangBLL.GetMsg("ForgetPwdFail");
                }
                else
                {
                    var custInfoProfile = custInfoBLL.GetCustInfoProfile(custInfo.CustId);

                    string pattern = @"(?<=[\w]{1})[\w-\._\+%]*(?=[\w]{1}@)";
                    string result = Regex.Replace(custInfoProfile.Email, pattern, m => new string('*', m.Length));

                    custInfoBLL.ForgetPwd1(custInfo.CustId, resetPasswordUrl);
                    if (!string.IsNullOrEmpty(forgetPwdReq.Username))
                    {
                        apiResult.Message = $"Has send to {result}.";//selLangBLL.GetMsg("ForgetPwdSuccess");
                    }
                    else
                    {
                        apiResult.Message = selLangBLL.GetMsg("ForgetPwdSuccess");
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
            var apiResult = new ApiResult<bool>(selLangBLL);
            try
            {

                Customer custInfo = null;

                var resetPasswordUrl = Url.RouteUrl("desktop", new { action = "", controller = "resetPassword", LangId = forgetPwdReq.flag == 1 ? selLangBLL.GetLangId() : "" }, protocol: Url.ActionContext.HttpContext.Request.Scheme, host: Url.ActionContext.HttpContext.Request.Host.Host);

                if (forgetPwdReq.flag == 1)
                {
                    resetPasswordUrl = $"{Url.ActionContext.HttpContext.Request.Scheme}://{Url.ActionContext.HttpContext.Request.Host.Host}/{selLangBLL.GetLangId()}/resetPassword";
                }
                else
                {
                    resetPasswordUrl = $"{Url.ActionContext.HttpContext.Request.Scheme}://{Url.ActionContext.HttpContext.Request.Host.Host}/resetPassword";
                }

                if (!string.IsNullOrEmpty(forgetPwdReq.Username))
                {
                    custInfo = custInfoBLL.GetCustInfoByUserName(forgetPwdReq.Username.Trim());
                }
                else
                {
                    custInfo = custInfoBLL.GetCustInfoByMail(forgetPwdReq.Email.Trim());
                }

                if (custInfo == null)
                {
                    apiResult.Message = selLangBLL.GetMsg("ForgetPwdFail");
                }
                else
                {
                    var custInfoProfile = custInfoBLL.GetCustInfoProfile(custInfo.CustId);

                    string pattern = @"(?<=[\w]{1})[\w-\._\+%]*(?=[\w]{1}@)";
                    string result = Regex.Replace(custInfoProfile.Email, pattern, m => new string('*', m.Length));

                    ApiResult commonResult = await custInfoBLL.ForgetPwd1Async(custInfo.CustId, resetPasswordUrl);
                    if (commonResult.ErrorCode == 0)
                    {
                        if (!string.IsNullOrEmpty(forgetPwdReq.Username))
                        {
                            apiResult.Message = $"Has send to {result}.";//selLangBLL.GetMsg("ForgetPwdSuccess");
                        }
                        else
                        {
                            apiResult.Message = selLangBLL.GetMsg("ForgetPwdSuccess");
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
                Dictionary<string, string> datas = new Dictionary<string, string>();
                foreach (var k in keys)
                {
                    var lang = selLangBLL.GetResourceValue(k) ?? selLangBLL.GetMsg(k);
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
                var serverInfo = serverInfoBLL.GetServerInfo();
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
                result.Data = selLangBLL.GetLangIdList();
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
        /*
    { value: 9, name: 'Joker', displayText: "Joker", PlayFree: false },
    { value: 10, name: 'SG', displayText: "SG" },
    { value: 5, name: 'PP', displayText: "PP" },
    { value: 14, name: 'PG', displayText: "PG" },
    { value: 16, name: 'CQ9', displayText: "CQ9" }
             */

        public ApiResult<List<Provider>> GetGameList()
        {
            ApiResult<List<Provider>> apiResult = new ApiResult<List<Provider>>();

            List<Provider> providers = new List<Provider> {
                new Provider { Value = 6, Name = "GPI", DisplayText = selLangBLL.GetResourceValue("lbl_GPI"), PlayFree = true, DisplayKey = "lbl_GPI" },
                new Provider { Value = 8, Name = "YGG", DisplayText = "YGG", PlayFree = true },
                new Provider { Value = 9, Name = "Joker", DisplayText = "Joker", PlayFree = false },
                new Provider { Value = 10, Name = "SG", DisplayText = "SG", PlayFree = true },
                new Provider { Value = 5, Name = "PP", DisplayText = "PP", PlayFree = true },
                new Provider { Value = 14, Name = "PG", DisplayText = "PG", PlayFree = true },
            };

            IEnumerable<int> tpIds = new int[] { };

            if (User.Identity.IsAuthenticated)
            {
                var authData = authorizeBLL.GetAuthData();
                var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);
                tpIds = casinoBLL.GetProviderIdByCurrency(custInfo.CurrencyId, custInfo.SiteId);
            }
            else
            {
                var serverInfo = serverInfoBLL.GetServerInfo();
                tpIds = casinoBLL.GetProviderIdByCurrency(0, serverInfo.SiteId);
            }

            apiResult.Data = providers?.Where(x => tpIds.Contains(x.Value)).ToList();
            foreach (var provider in providers)
            {
                string datePath = Path.Combine(environment.WebRootPath, "datas", provider.Name, "GameList.json");
                if (System.IO.File.Exists(datePath))
                    provider.GameList = JsonConvert.DeserializeObject<GameDesc>(System.IO.File.ReadAllText(datePath));
            }
            apiResult.ErrorCode = 0;
            return apiResult;
        }

        public ApiResult<IEnumerable<int>> GetTpIds()
        {
            ApiResult<IEnumerable<int>> apiResult = new ApiResult<IEnumerable<int>>();
            if (User.Identity.IsAuthenticated)
            {
                var authData = authorizeBLL.GetAuthData();
                var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);
                apiResult.Data = casinoBLL.GetProviderIdByCurrency(custInfo.CurrencyId, custInfo.SiteId);
            }
            else
            {
                var serverInfo = serverInfoBLL.GetServerInfo();
                apiResult.Data = casinoBLL.GetProviderIdByCurrency(0, serverInfo.SiteId);
            }

            apiResult.ErrorCode = 0;
            return apiResult;
        }

        [HttpPost]
        public ApiResult<IEnumerable<BannerSettingItem>> GetBanner([FromForm] string FolderName, [FromForm] string refId, [FromForm] bool filterHideInHome)
        {
            ApiResult<IEnumerable<BannerSettingItem>> result = new ApiResult<IEnumerable<BannerSettingItem>>();

            var serverInfo = serverInfoBLL.GetServerInfo();
            string dataPath = Path.Combine(environment.WebRootPath, $"datas/{serverInfo.SiteName}_bannerSetting.json");
            BannerSetting _bannerSetting = new BannerSetting();
            if (System.IO.File.Exists(dataPath))
            {
                _bannerSetting = JsonConvert.DeserializeObject<BannerSetting>(System.IO.File.ReadAllText(dataPath));
            }

            var perferCurr = 0;
            var beforeLogin = true;
            if (User.Identity.IsAuthenticated)
            {
                var authData = authorizeBLL.GetAuthData();
                var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);
                perferCurr = custInfo.CurrencyId;
                beforeLogin = false;
            }
            else
            {
                perferCurr = selLangBLL.GetPreferCurrencyId();
            }

            if (FolderName == "Promotion")
            {
                var banner_CurrencyId = !User.Identity.IsAuthenticated && perferCurr == 13 ? 2 : perferCurr;
                var bannerFromDB = bannerBLL.GetBannerFromDB(banner_CurrencyId, selLangBLL.GetLangId());
                var bannerFromJSON = _bannerSetting.Promotion.Where(x => x.currencyId == null || beforeLogin || x.currencyId.Any(y => y == perferCurr));
                result.Data = bannerFromDB.Concat(bannerFromJSON);

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

        [HttpPost]
        public async Task<ApiResult> CheckResetPwdTokenAsync([FromForm] string key)
        {
            var apiResult = new ApiResult(selLangBLL);
            if (User.Identity.IsAuthenticated)
            {
                apiResult.ErrorCode = 1;
            }
            else if (authBLL.Authenticate(key, out CustInfoProfile custInfoProfile))
            {
                Customer customer = null;
                int userLoginResult = custInfoBLL.UserLoginForResetPwd(custInfoProfile.UserName, tools.GetClientIP(), tools.GetFromUrl(), Request.Headers["User-Agent"], ref customer, out string token);
                if (userLoginResult == 0)
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name,custInfoProfile.UserName ),
                        new Claim(ClaimTypes.NameIdentifier, customer.CustId.ToString()),
                        new Claim("Token",token ),
                        new Claim("LangId",custInfoProfile.UserLang )
                    };
                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));
                    apiResult.ErrorCode = 0;
                }
                else if (userLoginResult == -3)
                {
                    apiResult.ErrorCode = 401;
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
                Dictionary<string, string> ServerInfoData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                result.Data = ServerInfoData;
                ServerInfoData.Add("Now", DateTime.Now.ToString());
                ServerInfoData.Add("EnvironmentName", environment.EnvironmentName);
                ServerInfoData.Add("LangId", selLangBLL.GetLangId());
                var host = Dns.GetHostEntry(Dns.GetHostName());
                int i = 1;
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        ServerInfoData.Add($"IP {i++}.", ip.ToString());
                    }
                }
                ServerInfoData.Add("Url.Host", Request.Host.Host);
                ServerInfoData.Add("Url.Port", Request.Host.Port.ToString());
                var serverInfo = serverInfoBLL.GetServerInfo();
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
                    string sKey = head.Key;
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
            var apiResult = new ApiResult<bool>(selLangBLL);
            apiResult.Data = custInfoBLL.ExistsPhoneNo(0, req.AreaCode, req.PhoneNo);
            apiResult.ErrorCode = 0;
            return apiResult;
        }

        [HttpPost]
        public ApiResult<IEnumerable<Country>> GetCountryList()
        {
            var apiResult = new ApiResult<IEnumerable<Country>>();
            apiResult.Data = selLangBLL.GetCountryList();
            apiResult.ErrorCode = 0;
            return apiResult;
        }
    }
}