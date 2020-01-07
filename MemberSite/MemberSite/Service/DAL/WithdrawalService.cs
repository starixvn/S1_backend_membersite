using Dapper;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MemberSite.Service.DAL
{
    public class WithdrawalService
    {
        private readonly IConfiguration configuration;
        private readonly Common common;
        private readonly WalletService walletService;
        private int typePosition = 1000;

        public WithdrawalService(IConfiguration configuration, Common common, WalletService walletService)
        {
            this.configuration = configuration;
            this.common = common;
            this.walletService = walletService;
        }
        public int WithdrawalReq(int custId, string userName, int currencyId, int siteId, decimal amount, string memo, WithdrawalInfo withdrawalInfo, out decimal afterAmount, out string transId)
        {
            transId = "";

            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using MySqlTransaction trans = conn.BeginTransaction();
            var p = new DynamicParameters(new { CustId = custId, UserName = userName, CurrencyId = currencyId, SiteId = siteId, Amount = amount, Memo = memo, WithdrawalInfo = Newtonsoft.Json.JsonConvert.SerializeObject(withdrawalInfo), Prefix = $"W{common.GetEnv()}" });
            p.Add("vTransId", dbType: DbType.String, direction: ParameterDirection.Output);

            conn.Execute("WithdrawalInfo_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);
            transId = p.Get<string>("vTransId");

            afterAmount = 0;
            byte is1stDepoist = 0;
            int result = -99;
            result = walletService.SetBalance(custId, siteId, userName, amount * -1, typePosition + 1, null, transId, out afterAmount, out is1stDepoist, conn, trans);
            if (result == 0)
            {
                trans.Commit();
                return 0;
            }
            else
            {
                return result;
            }
        }
        public IEnumerable<WithdrawalHistory> GetWithdrawalInfoRecent(int custId, int siteId, int topNum)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Query<WithdrawalHistory>("WithdrawalInfoRecent_Get", new { CustId = custId, SiteId = siteId, TopNum = topNum }, commandType: CommandType.StoredProcedure);
        }
        public IEnumerable<WithdrawalHistory> GetWithdrawalInfoHistory(int custId, long startDate, long endDate, int siteId, int pageSize, ref int pageNumber, out int totalPages, out int rowCount)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            var p = new DynamicParameters(new { CustId = custId, StartDate = startDate, EndDate = endDate, SiteId = siteId, PageSize = pageSize });
            p.Add("PageNumber", pageNumber, dbType: DbType.Int32, direction: ParameterDirection.InputOutput);
            p.Add("TotalPages", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("RowCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("StartIndex", dbType: DbType.Int32, direction: ParameterDirection.Output);

            var depositInfoHistoryResult = conn.Query<WithdrawalHistory>("WithdrawalInfoSearch_Get", p, commandType: CommandType.StoredProcedure);
            totalPages = p.Get<int?>("TotalPages") ?? 0;
            rowCount = p.Get<int?>("RowCount") ?? 0;
            return depositInfoHistoryResult;
        }

        public int SetWithdrawalCancel(int custId, int withdrawalId, int siteid)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using MySqlTransaction trans = conn.BeginTransaction();

            var result = conn.Execute("WithdrawalInfoCancel_Set", new { CustId = custId, WithdrawalId = withdrawalId, Siteid = siteid }, transaction: trans, commandType: CommandType.StoredProcedure);
            trans.Commit();

            return result;
        }
    }
}
