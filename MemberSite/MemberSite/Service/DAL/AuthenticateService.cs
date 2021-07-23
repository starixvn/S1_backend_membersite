using MySql.Data.MySqlClient;
using System;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using MemberSite.Service.Library;
using System.Data;
using MemberSite.Service.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MemberSite.Service.DAL
{
    public class AuthenticateService
    {
        private readonly IConfiguration configuration;
        private readonly Tools tools;
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;

        public AuthenticateService(IServiceProvider provider)
        {
            configuration = provider.GetRequiredService<IConfiguration>();
            tools = provider.GetRequiredService<Tools>();
            var logger = provider.GetRequiredService<ILogger<MySqlClient>>();
            _mainDbClient = new MySqlClient(configuration.GetConnectionString("ConnectionString"), logger);
            _reportDbClient = new MySqlClient(configuration.GetConnectionString("ConnectionStringReport"), logger);
        }
        public void AddUserLoginLog(int custId, string userName, int siteId, string loginIp, string fromUrl, string userAgent)
        {
            Task.Run(() =>
            {
                using (var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
                {
                    conn.Execute("LoginLog_Ins", new { CustId = custId, UserName = userName, SiteId = siteId, LoginIp = loginIp, FromUrl = fromUrl, UserAgent = userAgent }, commandType: CommandType.StoredProcedure);
                }
            });
        }
        public void AddLoginRecord(int custId, int siteId, string ip, out string token)
        {
            token = tools.GetToken();
            using (var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
            {
                conn.Open();
                using (var trans = conn.BeginTransaction())
                {
                    conn.Execute("LoginRecord_Set", new { CustId = custId, SiteId = siteId, IP = ip, Token = token }, trans, commandType: CommandType.StoredProcedure);
                    trans.Commit();
                }
            }
        }
        public bool UpdateOnline(int custId, bool isOnline, string token, string ip)
        {
            using (var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
            {
                return conn.Execute("LoginCheckIn_Set", new { IsOnline = isOnline, CustId = custId, IP = ip, Token = token }, commandType: CommandType.StoredProcedure) > 0;
            }
        }
        
        public OnUser GetOnUser(int custId)
        {
            return _mainDbClient.QueryFirst<OnUser>("Member_OnUser_Get", new DynamicParameters(new {CustId = custId}));
        }
    }
}
