using Dapper;
using MemberSite.Service.Library;
using MemberSite.Service.Model.enums;
using MemberSite.Service.Model.Line;
using MySql.Data.MySqlClient;
using System.Data;

namespace MemberSite.Service.DAL
{
    public class AuthService
    {
        private readonly IConfiguration configuration;
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;
        public AuthService(IServiceProvider provider)
        {
            configuration = provider.GetRequiredService<IConfiguration>();
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }

        public string Authenticate(string Key)
        {
            string permissionData = null;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            var p = new DynamicParameters(new { Token = Key });
            p.Add("PermissionData", dbType: DbType.String, direction: ParameterDirection.Output);
            var result = 0;
            conn.Open();
            using var trans = conn.BeginTransaction();
            result = conn.Execute("Common_AuthToken_Sel", p, trans, commandType: CommandType.StoredProcedure);
            trans.Commit();
            permissionData = p.Get<string>("PermissionData");
            return permissionData;
        }

        public async Task<LineIssueTokenModel> GetLineToken(string token)
        {
            return await _reportDbClient.QueryFirstAsync<LineIssueTokenModel>("Member_LineIssueToken_Get", new DynamicParameters(new
            {
                _Token = token
            }));
        }

        public bool IssueLineToken(int type, string paramString, string token,
            MySqlConnection conn = null, MySqlTransaction trans = null)
        {
            var sp = "Member_LineIssueToken_Ins";
            var result = 0;
            var param = new DynamicParameters(new { _FlowType = type, _Info = paramString, _Token = token });

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

            return (result == 0) ? false : true;
        }
    }
}
