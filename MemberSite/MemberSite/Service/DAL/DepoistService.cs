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
            var p = new DynamicParameters(new { depositDetailInfo.DepositType, depositDetailInfo.CustId, depositDetailInfo.UserName, depositDetailInfo.CurrencyId, depositDetailInfo.SiteId, depositDetailInfo.DepositInfo, depositDetailInfo.Amount, depositDetailInfo.Memo, Prefix = $"D{common.GetEnv()}" });
            p.Add("vPostScript", dbType: DbType.String, direction: ParameterDirection.Output);
            p.Add("vTransId", dbType: DbType.String, direction: ParameterDirection.Output);

            var result = conn.Execute("DepositInfo_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);
            trans.Commit();

            postScript = p.Get<string>("vPostScript");
            transId = p.Get<string>("vTransId");

            return result;
        }

        public IEnumerable<DepositInfoHistory> GetDepositInfoRecent(int custId, int siteId, int topNum)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<DepositInfoHistory>("DepositInfoRecent_Get", new { CustId = custId, SiteId = siteId, TopNum = topNum }, commandType: CommandType.StoredProcedure);
        }

        public IEnumerable<DepositInfoHistory> GetDepositInfoHistory(int custId, long startDate, long endDate, int siteId, int pageSize, ref int pageNumber, out int totalPages,out int rowCount)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
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
        public int SetDepositReceipt(int custId, int depositId, string refNo, long depositTime)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Execute("DepositReceipt_Set", new { CustId = custId, DepositId = depositId, RefNo = refNo, DepositTime = depositTime }, commandType: CommandType.StoredProcedure);
        }

        public int SetDepositReceiptPic(int custId, int depositId, string picFileName, byte[] depositPic)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Execute("DepositReceiptPic_Set", new { CustId = custId, DepositId = depositId, PicFileName = picFileName, DepositPic = depositPic }, commandType: CommandType.StoredProcedure);
        }
        public int SetDepositCancel(int custId, int depositId, int siteid)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using MySqlTransaction trans = conn.BeginTransaction();

            var result = conn.Execute("DepositInfoCancel_Set", new { CustId = custId, DepositId = depositId, Siteid = siteid }, transaction: trans, commandType: CommandType.StoredProcedure);
            trans.Commit();

            return result;
        }
        public DepositReceipt GetDepositReceipt(int depositId, int siteId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.QueryFirstOrDefault<DepositReceipt>("DepositReceipt_Get", new { DepositId = depositId, SiteId = siteId }, commandType: CommandType.StoredProcedure);
        }
        public IEnumerable<DepositLimitInfo> GetDepositLimitInfoList()
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<DepositLimitInfo>("Member_DepositLimitInfo_Get", commandType: CommandType.StoredProcedure);
        }
    }
}
