using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using MemberSite.Service.Library;
using System.Data;

namespace MemberSite.Service.DAL
{
    public class AuthenticateService
    {
        private readonly IConfiguration configuration;
        private readonly Tools tools;

        public AuthenticateService(IConfiguration configuration, Tools tools)
        {
            this.configuration = configuration;
            this.tools = tools;
        }
        public void AddUserLoginLog(int custId, string userName, int siteId, string loginIp, string fromUrl, string userAgent)
        {
            Task.Run(() =>
            {
                using (MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
                {
                    conn.Execute("LoginLog_Ins", new { CustId = custId, UserName = userName, SiteId = siteId, LoginIp = loginIp, FromUrl = fromUrl, UserAgent = userAgent }, commandType: CommandType.StoredProcedure);
                }
            });
        }
        public void AddLoginRecord(int custId, int siteId, string ip, out string token)
        {
            token = tools.GetToken();
            using (MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
            {
                conn.Open();
                using (MySqlTransaction trans = conn.BeginTransaction())
                {
                    conn.Execute("LoginRecord_Set", new { CustId = custId, SiteId = siteId, IP = ip, Token = token }, trans, commandType: CommandType.StoredProcedure);
                    trans.Commit();
                }
            }
        }
        public bool UpdateOnline(int custId, bool isOnline, string token, string ip)
        {
            using (MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
            {
                return conn.Execute("LoginCheckIn_Set", new { IsOnline = isOnline, CustId = custId, IP = ip, Token = token }, commandType: CommandType.StoredProcedure) > 0;
            }
        }
    }
}
