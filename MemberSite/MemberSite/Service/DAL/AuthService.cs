using System.Data;

using Dapper;

using MemberSite.Service.Library;
using MemberSite.Service.Model.enums;
using MemberSite.Service.Model.Line;

using MySql.Data.MySqlClient;
using Newtonsoft.Json;

namespace MemberSite.Service.DAL
{
    public class AuthService
    {
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;
        public AuthService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }

        public string Authenticate(string Key)
        {
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            var p = new DynamicParameters(new { Token = Key });
            p.Add("PermissionData", dbType: DbType.String, direction: ParameterDirection.Output);

            _mainDbClient.Execute("Common_AuthToken_Sel", trans, p);

            trans.Commit();
            SentrySdk.CaptureMessage($"VerifyEmailAsync Authenticate Common_AuthToken_Sel: {JsonConvert.SerializeObject(p)} - Key:{JsonConvert.SerializeObject(Key)} ");
            var permissionData = p.Get<string>("PermissionData");
            return permissionData;
        }

        public async Task<LineIssueTokenModel> GetLineToken(string token)
        {
            return await _reportDbClient.QueryFirstAsync<LineIssueTokenModel>("Member_LineIssueToken_Get", new DynamicParameters(new
            {
                _Token = token
            }));
        }

        public bool IssueLineToken(int type, string token,
            MySqlConnection conn = null, MySqlTransaction trans = null)
        {
            var sp = "Member_LineIssueToken_Ins";
            int result;
            var param = new DynamicParameters(new { _FlowType = type, _Token = token });

            if (conn == null)
            {
                //No execute in transaction
                result = _mainDbClient.QueryFirstAsync<int>(sp, param).Result;
            }
            else
            {
                //Execute in transaction
                result = conn.QueryFirstAsync<int>(sp, param, trans).Result;
            }

            return (result != 0);
        }
    }
}
