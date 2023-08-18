using System.Security.Claims;
using System.Web;

using MemberSite.Controllers.Api;
using MemberSite.Service.BLL;
using MemberSite.Service.Model;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace MemberSite.Controllers
{
    public class ExtraController : Controller
    {
        private readonly SelLangBLL _selLangBll;
        private readonly ILogger<ExtraController> _logger;
        private readonly SiteBll _siteBll;

        public ExtraController(IServiceProvider provider)
        {
            _selLangBll = provider.GetRequiredService<SelLangBLL>();
            _logger = provider.GetRequiredService<ILogger<ExtraController>>();
            _siteBll = provider.GetRequiredService<SiteBll>();
        }

        [HttpPost]
        [EnableCors]
        public async Task<IActionResult> SignupAsync([FromForm] SiteController.SignupModel signupModel)
        {
            var apiResult = new ApiResult(_selLangBll);
            var isAuthenticated = User.Identity is { IsAuthenticated: true };
            if (isAuthenticated)
            {
                return RedirectToRoute(new { action = "Index", controller = "Home" });
            }
            var customer = _siteBll.Signup(signupModel, apiResult, isAuthenticated, Request.Host.Host,
                Request.Headers["User-Agent"], false);

            if (apiResult.ErrorCode != 0 || customer == null)
            {
                _logger.LogInformation($"errorCode={apiResult.ErrorCode}, message={apiResult.Message}");
                return Redirect($"{signupModel.SignupUrl}{(signupModel.SignupUrl.IndexOf("?") > -1 ? $"&message={HttpUtility.UrlEncode(apiResult.Message)}&errorCode={apiResult.ErrorCode}" : $"?message={HttpUtility.UrlEncode(apiResult.Message)}&errorCode={apiResult.ErrorCode}")}");
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, customer.UserName),
                new(ClaimTypes.NameIdentifier, customer.CustId.ToString()),
                new("Token", customer.Token),
                new("LangId", customer.UserLang)
            };
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));
            if (!string.IsNullOrEmpty(signupModel.RedirectUrl))
            {
                return Redirect(signupModel.RedirectUrl.IndexOf(".", StringComparison.Ordinal) > -1
                    ? signupModel.RedirectUrl
                    : $"{Url.ActionContext.HttpContext.Request.Scheme}://{Url.ActionContext.HttpContext.Request.Host.Host}/{signupModel.RedirectUrl}");
            }

            return RedirectToRoute(new { action = "Index", controller = "Home" });
        }

    }
}
