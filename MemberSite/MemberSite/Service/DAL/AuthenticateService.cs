using Dapper;

using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.DAL
{
    public class AuthenticateService
    {
        private readonly Tools tools;
        private readonly MySqlClient _mainDbClient;

        public AuthenticateService(IServiceProvider provider)
        {
            tools = provider.GetRequiredService<Tools>();
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }

        public void AddUserLoginLog(int custId, string userName, int siteId, string loginIp, string fromUrl, string userAgent)
        {
            Task.Run(() =>
            {
                _mainDbClient.Execute("LoginLog_Ins", new { CustId = custId, UserName = userName, SiteId = siteId, LoginIp = loginIp, FromUrl = fromUrl, UserAgent = userAgent });
            });
        }

        public void AddLoginRecord(int custId, int siteId, string ip, int deviceType, out string token)
        {
            token = tools.GetToken();
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            _mainDbClient.Execute("LoginRecord_Set_V2", trans, new { CustId = custId, SiteId = siteId, IP = ip, Token = token, DeviceType = deviceType });
            trans.Commit();
        }

        public bool UpdateOnline(int custId, bool isOnline, string token, string ip, int? deviceType)
        {
            return _mainDbClient.Execute("LoginCheckIn_Set_V2", new DynamicParameters(new { IsOnline = isOnline, CustId = custId, IP = ip, Token = token, DeviceType = deviceType })) > 0;
        }

        public OnUser GetOnUser(int custId)
        {
            return _mainDbClient.QueryFirst<OnUser>("Member_OnUser_Get", new DynamicParameters(new { CustId = custId }));
        }
    }
}
