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
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using MySqlTransaction trans = conn.BeginTransaction();
            var p = new DynamicParameters(new { depositDetailInfo.DepositType, depositDetailInfo.CustId, depositDetailInfo.UserName, depositDetailInfo.SiteId, depositDetailInfo.DepositInfo, depositDetailInfo.Amount, depositDetailInfo.Memo, Prefix = $"D{common.GetEnv()}" });
            p.Add("vPostScript", dbType: DbType.String, direction: ParameterDirection.Output);
            p.Add("vTransId", dbType: DbType.String, direction: ParameterDirection.Output);

            conn.Execute("DepositInfo_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);
            trans.Commit();

            postScript = p.Get<string>("vPostScript");
            transId = p.Get<string>("vTransId");


            return 0;
        }

        public IEnumerable<DepositInfoHistory> GetDepositInfoRecent(int custId, int siteId, int topNum)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Query<DepositInfoHistory>("DepositInfoRecent_Get", new { CustId = custId, SiteId = siteId, TopNum = topNum }, commandType: CommandType.StoredProcedure);
        }

        public IEnumerable<DepositInfoHistory> GetDepositInfoHistory(int custId, long startDate, long endDate, int siteId, int pageSize, ref int pageNumber, out int totalPages,out int rowCount)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            var p = new DynamicParameters(new { CustId = custId, StartDate = startDate, EndDate = endDate, SiteId = siteId, PageSize = pageSize });
            p.Add("PageNumber", pageNumber, dbType: DbType.Int32, direction: ParameterDirection.InputOutput);
            p.Add("TotalPages", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("RowCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("StartIndex", dbType: DbType.Int32, direction: ParameterDirection.Output);

            var depositInfoHistoryResult = conn.Query<DepositInfoHistory>("DepositInfoSearch_Get", p, commandType: CommandType.StoredProcedure);
            totalPages = p.Get<int?>("TotalPages") ?? 0;
            rowCount = p.Get<int?>("RowCount") ?? 0;
            return depositInfoHistoryResult;
        }
    }
}
