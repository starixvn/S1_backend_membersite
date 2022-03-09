using Dapper;
using MemberSite.Service.Model;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System.Data;

namespace MemberSite.Service.DAL
{
    public class RolloverService
    {
        private readonly IConfiguration configuration;
        public RolloverService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public CustRollOver GetCustRollOver(int custId, string bonusCode)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            //Common_RollOver_GetV2
            var result = conn.QueryFirstOrDefault<CustRollOver>("Common_RollOver_Get", new { CustId = custId, BonusCode = bonusCode }, commandType: CommandType.StoredProcedure);
            return result;
        }
    }
}
