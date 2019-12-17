using MemberSite.Service.Model;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MemberSite.Service.Model.enums;
using MemberSite.Service.Library;
using System.Data;

namespace MemberSite.Service.DAL
{
    public class DepoistService
    {
        private readonly IConfiguration configuration;
        private readonly Common common;

        public DepoistService(IConfiguration configuration, Common common)
        {
            this.configuration = configuration;
            this.common = common;
        }
        public int DepositReq(DepositDetailInfo depositDetailInfo, out string transId, out string postScript)
        {
            transId = null;
            postScript = null;
            using (MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
            {
                conn.Open();
                using (MySqlTransaction trans = conn.BeginTransaction())
                {
                    var p = new DynamicParameters(new { depositDetailInfo.DepositType, depositDetailInfo.CustId, depositDetailInfo.UserName, depositDetailInfo.SiteId, depositDetailInfo.DepositInfo, depositDetailInfo.Amount, depositDetailInfo.Memo, Prefix = $"D{common.GetEnv()}" });
                    p.Add("vPostScript", dbType: DbType.String, direction: ParameterDirection.Output);
                    p.Add("vTransId", dbType: DbType.String, direction: ParameterDirection.Output);

                    conn.Execute("DepositInfo_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);
                    trans.Commit();

                    postScript = p.Get<string>("vPostScript");
                    transId = p.Get<string>("vTransId");
                    return 0;
                }
            }
        }

        public IEnumerable<DepositInfoHistory> GetDepositInfoRecent(int custId, int siteId, int topNum)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Query<DepositInfoHistory>("DepositInfoRecent_Get", new { CustId = custId, SiteId = siteId, TopNum = topNum }, commandType: CommandType.StoredProcedure);
        }

        public IEnumerable<WithdrawalHistory> GetWithdrawalInfoRecent(int custId, int siteId, int topNum)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Query<WithdrawalHistory>("WithdrawalInfoRecent_Get", new { CustId = custId, SiteId = siteId, TopNum = topNum }, commandType: CommandType.StoredProcedure);
        }
    }
}
