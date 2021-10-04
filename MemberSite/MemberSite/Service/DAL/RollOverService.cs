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
        public Rollover ReachedRollover(int custId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction(IsolationLevel.ReadUncommitted);
            var result = ReachedRollover(custId, conn, trans);
            trans.Rollback();
            return result;
        }
        public Rollover ReachedRollover(int custId, MySqlConnection conn, MySqlTransaction trans)
        {
            var result = conn.QueryFirstOrDefault<Rollover>("Common_ReachedRollover_Get", new { CustId = custId }, transaction: trans, commandType: CommandType.StoredProcedure);
            return result;
        }
    }
}
