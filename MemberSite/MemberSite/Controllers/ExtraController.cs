using System.Security.Claims;
using System.Web;
using MemberSite.Service.BLL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace MemberSite.Controllers
{
    public class ExtraController : Controller
    {
        private readonly CustInfoBll _custInfoBll;
        private readonly SelLangBLL _selLangBll;
        private readonly CommonBLL _commonBll;
        private readonly Tools _tools;
        private readonly ServerInfoBll _serverInfoBll;
        private readonly ILogger<ExtraController> _logger;

        public ExtraController(IServiceProvider provider)
        {
            _custInfoBll = provider.GetRequiredService<CustInfoBll>();
            _selLangBll = provider.GetRequiredService<SelLangBLL>();
            _commonBll = provider.GetRequiredService<CommonBLL>();
            _tools = provider.GetRequiredService<Tools>();
            _serverInfoBll = provider.GetRequiredService<ServerInfoBll>();
            _logger = provider.GetRequiredService<ILogger<ExtraController>>();
        }
        public class SignupModel : Api.SiteController.RegisteredModel
        {
            public string SignupUrl { get; set; }
            public string ReferralUrl { get; set; }
            public string PhoneNo { get; set; }
            public string AreaCode { get; set; }
            public string RealName { get; set; }
            public string RedirectUrl { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> SignupAsync([FromForm] SignupModel signupModel)
        {
            var message = "";
            var errorCode = 99;
            var serverInfo = _serverInfoBll.GetServerInfo();

            if (User.Identity.IsAuthenticated)
            {
                return RedirectToRoute(new { action = "Index", controller = "Home" });
            }

            var custId = 0;
            var result = -99;
            var phoneNoValidation = true;
            var phoneExists = false;
            var mailExists = false;

            if (long.TryParse(signupModel.PhoneNo, out var phoneNo))
            {
                signupModel.PhoneNo = phoneNo.ToString();
                phoneExists = _custInfoBll.ExistsPhoneNo(0, signupModel.AreaCode, signupModel.PhoneNo);
                if (phoneExists)
                    result = -6;
            }
            else if (!string.IsNullOrEmpty(signupModel.PhoneNo))
            {
                phoneNoValidation = false;
                result = -20;
            }

            if (!string.IsNullOrWhiteSpace(signupModel.Email))
            {
                mailExists = _custInfoBll.ExistsEmail(0, signupModel.Email);
                if (mailExists)
                    result = -10;
            }

            if (!phoneExists && phoneNoValidation && !mailExists)
            {
                result = _custInfoBll.AddCustInfo(signupModel.UserName, signupModel.Userpwd, signupModel.Email,
                    signupModel.CurrencyId, signupModel.UserLang, signupModel.RefId, signupModel.AffId, signupModel.Aff,
                    signupModel.AffCode, serverInfo.SiteId, signupModel.LineUserId, out custId, signupModel.SignupUrl, signupModel.ReferralUrl, signupModel.PhoneNo, signupModel.AreaCode,
                    signupModel.RealName);
            }

            if (result == -1)
            {
                errorCode = result;
                message = _selLangBll.GetMsg("err_ThisCurrencyNotSupport");
            }
            else if (result == -2)
            {
                errorCode = result;
                message = _selLangBll.GetMsg("msg_UsernameValidate");
            }
            else if (result == -3)
            {
                errorCode = result;
                message = _selLangBll.GetMsg(serverInfo.SiteId == (int)SiteId.Nova88 ? "msg_PasswordValidate" : "msg_PasswordValidate2");
            }
            else if (result == -4)
            {
                errorCode = result;/**/
                message = "";
            }
            else if (result == -5)
            {
                errorCode = result;
                message = _selLangBll.GetMsg("msg_UsernameNotAvailable");
            }
            else if (result == -6)
            {
                errorCode = result;
                message = $"{_selLangBll.GetResourceValue("lbl_Phone")} {_selLangBll.GetMsg("msg_HasExist")}";
            }
            else if (result == -10)
            {
                errorCode = result;
                message = $"{_selLangBll.GetResourceValue("lbl_Email")} {_selLangBll.GetMsg("msg_HasExist")}";
            }
            else if (result == -19)
            {
                errorCode = result;
                message = _selLangBll.GetMsg("mag_AffiliateNotExist");
            }
            else if (result == -20)
            {
                errorCode = result;
                message = $"{_selLangBll.GetResourceValue("lbl_Phone")} {_selLangBll.GetMsg("msg_PhoneWrongFormat")}";
            }
            else if (custId == 0)
            {
                errorCode = result;
                message = _selLangBll.GetMsg("msg_UsernameNotAvailable");
            }
            else if (result != 0)
            {
                errorCode = result;
                message = _selLangBll.GetMsg("msg_UsernameNotAvailable");
            }
            else
            {
                _commonBll.SendWelcomeMail(custId);

                Customer customer = null;
                var fromUrl = _tools.GetFromUrl();
                var deviceType = (int) _tools.GetDeviceId(fromUrl);
                
                var userLoginResult = _custInfoBll.UserLogin(false,0,signupModel.UserName, signupModel.Userpwd, _tools.GetClientIP(), fromUrl, Request.Headers["User-Agent"],deviceType, ref customer, out string token);

                if (userLoginResult == -1)
                {
                    errorCode = 1101;
                    message = _selLangBll.GetMsg("msg_DepositProcessLogin_LoginFailed");
                }
                else if (userLoginResult == -2)
                {
                    errorCode = 1001;
                    message = _selLangBll.GetMsg("msg_DepositProcessLogin_LoginFailed");
                }
                else if (userLoginResult == -3)
                {
                    errorCode = 1102;
                    message = _selLangBll.GetMsg("msg_DepositProcessLogin_ACLocked");
                }
                else if (userLoginResult != 0)
                {
                    errorCode = 9000;
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
                    errorCode = 0;
                    if (!string.IsNullOrEmpty(signupModel.RedirectUrl))
                    {
                        if (signupModel.RedirectUrl.IndexOf(".", StringComparison.Ordinal) > -1)
                        {
                            return Redirect(signupModel.RedirectUrl);
                        }
                        return Redirect($"{Url.ActionContext.HttpContext.Request.Scheme}://{Url.ActionContext.HttpContext.Request.Host.Host}/{signupModel.RedirectUrl}");
                    }
                    else
                    {
                        return RedirectToRoute(new { action = "Index", controller = "Home" });
                    }

                }
            }

            _logger.LogInformation($"({errorCode}) {message}");

            return Redirect($"{signupModel.SignupUrl}{(signupModel.SignupUrl.IndexOf("?") > -1 ? $"&message={HttpUtility.UrlEncode(message)}&errorCode={errorCode}" : $"?message={HttpUtility.UrlEncode(message)}&errorCode={errorCode}")}");
        }

    }
}