using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MemberSite.Service.BLL
{
    public class AuthorizeBLL
    {
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly Tools tools;
        private readonly AuthenticateService authenticateService;

        public AuthorizeBLL(IHttpContextAccessor httpContextAccessor, Tools tools, AuthenticateService authenticateService)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.tools = tools;
            this.authenticateService = authenticateService;
        }

        public bool LoginCheckin()
        {
            var authData = GetAuthData();

            string ip = tools.GetClientIP();
            bool result = authenticateService.UpdateOnline(authData.CustId, true, authData.Token, ip);

            return result;
        }

        public AuthData GetAuthData()
        {
            AuthData authData = null;
            if (httpContextAccessor.HttpContext.User.Identity.IsAuthenticated)
            {
                authData = new AuthData();
                int custId = 0;
                int.TryParse(httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(m => m.Type == ClaimTypes.NameIdentifier).Value, out custId);
                authData.CustId = custId;
                authData.UserName = httpContextAccessor.HttpContext.User.Identity.Name;
                authData.Token = httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(m => m.Type == "Token").Value;
                authData.LangId = httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(m => m.Type == "LangId").Value;
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
    }
}
