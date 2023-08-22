using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace MemberSite.Service.BLL
{
    public class AuthorizeBLL
    {
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly Tools tools;
        private readonly AuthenticateService authenticateService;
        private readonly JwtConfig _jwtConfig;
        private readonly ILogger<AuthorizeBLL> _logger;

        public AuthorizeBLL(IServiceProvider provider)
        {
            httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
            tools = provider.GetRequiredService<Tools>();
            authenticateService = provider.GetRequiredService<AuthenticateService>();
            _jwtConfig = provider.GetRequiredService<IOptions<JwtConfig>>()?.Value;
            _logger = provider.GetRequiredService<ILogger<AuthorizeBLL>>();
        }

        public bool LoginCheckin(int? deviceType)
        {
            var authData = GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                return false;
            }
            var ip = tools.GetClientIP();
            var result = authenticateService.UpdateOnline(authData.CustId, true, authData.Token, ip, deviceType);

            return result;
        }

        public AuthData GetAuthData(bool checkOnUser = false)
        {
            AuthData authData = null;
            try
            {
                if (httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true)
                {
                    if (!int.TryParse(httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(m => m.Type == ClaimTypes.NameIdentifier)?.Value, out var custId))
                    {
                        return new AuthData();
                    }

                    authData = new AuthData
                    {
                        CustId = custId,
                        UserName = httpContextAccessor.HttpContext.User.Identity.Name,
                        Token = httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(m => m.Type == "Token")?.Value,
                        LangId = httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(m => m.Type == "LangId")?.Value
                    };

                    if (_jwtConfig.IsJwt || checkOnUser)
                    {
                        var onUser = GetOnUser(custId);
                        if (onUser == null)
                        {
                            authData.CustId = 0;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "GetAuthData");
            }

            return authData;
        }
        public bool Logout()
        {
            var authData = GetAuthData();

            var ip = tools.GetClientIP();
            var result = authenticateService.UpdateOnline(authData.CustId, false, authData.Token, ip, null);

            return result;
        }

        private OnUser GetOnUser(int custId)
        {
            return authenticateService.GetOnUser(custId);
        }

        public bool TryValidateJwtToken(string jwtToken, out AuthData appAuthData)
        {
            appAuthData = null;
            if (string.IsNullOrEmpty(jwtToken))
            {
                return false;
            }

            try
            {
                var handler = new JwtSecurityTokenHandler();
                handler.ValidateToken(jwtToken, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfig.SignKey)),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                }, out var validatedToken);

                var jwtSecurityToken = (JwtSecurityToken)validatedToken;
                appAuthData = new AuthData()
                {
                    UserName = jwtSecurityToken.Claims.First(claim => claim.Type == ClaimTypes.Name).Value,
                    CustId = int.Parse(jwtSecurityToken.Claims.First(claim => claim.Type == ClaimTypes.NameIdentifier).Value),
                    Token = jwtSecurityToken.Claims.First(claim => claim.Type == "Token").Value,
                    LangId = jwtSecurityToken.Claims.First(claim => claim.Type == "LangId").Value,
                };
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
