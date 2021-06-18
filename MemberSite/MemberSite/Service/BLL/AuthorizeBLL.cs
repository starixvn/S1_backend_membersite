using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MemberSite.Service.BLL
{
    public class AuthorizeBLL
    {
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly Tools tools;
        private readonly AuthenticateService authenticateService;
        private readonly bool _isJwt;

        public AuthorizeBLL(IServiceProvider provider)
        {
            httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
            tools = provider.GetRequiredService<Tools>();
            authenticateService = provider.GetRequiredService<AuthenticateService>();
            var configuration = provider.GetRequiredService<IConfiguration>();
            _isJwt = configuration.GetValue<bool>("JwtSettings:IsJwt");
        }

        public bool LoginCheckin()
        {
            var authData = GetAuthData();

            string ip = tools.GetClientIP();
            bool result = authenticateService.UpdateOnline(authData.CustId, true, authData.Token, ip);

            return result;
        }

        public AuthData GetAuthData([CallerMemberName] string callerName = "")
        {
            AuthData authData = null;
            if (httpContextAccessor.HttpContext.User.Identity.IsAuthenticated)
            {
                authData = new AuthData();
                int.TryParse(httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(m => m.Type == ClaimTypes.NameIdentifier).Value, out var custId);
                authData.CustId = custId;
                authData.UserName = httpContextAccessor.HttpContext.User.Identity.Name;
                authData.Token = httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(m => m.Type == "Token").Value;
                authData.LangId = httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(m => m.Type == "LangId").Value;

                if (_isJwt || callerName == "WithdrawalReqAsync")
                {
                    var onUser = GetOnUser(custId);
                    if (onUser == null)
                    {
                        authData.CustId = 0;
                    }
                }
            }

            return authData;
        }
        public bool Logout()
        {
            var authData = GetAuthData();

            string ip = tools.GetClientIP();
            bool result = authenticateService.UpdateOnline(authData.CustId, false, authData.Token, ip);

            return result;
        }

        private OnUser GetOnUser(int custId)
        {
            return authenticateService.GetOnUser(custId);
        }
    }
}
