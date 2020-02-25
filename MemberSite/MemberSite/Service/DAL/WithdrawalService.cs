using Dapper;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace MemberSite.Service.DAL
{
    public class WithdrawalService
    {
        private readonly IConfiguration configuration;
        private readonly Common common;
        private readonly WalletService walletService;
        private readonly RolloverService rolloverService;
        private int typePosition = 1000;

        public WithdrawalService(IConfiguration configuration, Common common, WalletService walletService, RolloverService rolloverService)
        {
            this.configuration = configuration;
            this.common = common;
            this.walletService = walletService;
            this.rolloverService = rolloverService;
        }
        public int WithdrawalReq(int custId, string userName, int currencyId, int siteId, decimal amount, string memo, WithdrawalInfo withdrawalInfo, out decimal afterAmount, out string transId)
        {
            transId = "";
            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            var p = new DynamicParameters(new { CustId = custId, UserName = userName, CurrencyId = currencyId, SiteId = siteId, Amount = amount, Memo = memo, WithdrawalInfo = Newtonsoft.Json.JsonConvert.SerializeObject(withdrawalInfo), Prefix = $"W{common.GetEnv()}" });
            p.Add("vTransId", dbType: DbType.String, direction: ParameterDirection.Output);

            conn.Execute("WithdrawalInfo_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);
            transId = p.Get<string>("vTransId");
            var rollover = rolloverService.ReachedRollover(custId, conn, trans);

            if (!rollover.ReachedRollover)
            {
                return -2;
            }
            //else if (GetWithdrawalAvailableBalance(custId, conn, trans) < amount)
            //{
            //    return -3;
            //}

            byte is1stDepoist = 0;
            int result = -99;
            result = walletService.SetBalance(custId, siteId, userName, amount * -1, typePosition + 1, null, transId, out afterAmount, out is1stDepoist, conn, trans);
            if (result > 0)
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

        public int SetWithdrawalCancel(int custId, string transId, int siteId, IEnumerable<int> forStatus, WithdrawalStatus status)
        {
            int withdrawalId = 0;
            Match m = Regex.Match(transId, @"\d+");
            int.TryParse(m.Value, out withdrawalId);
            int result = 0;
            int type = 1003;  /*Cancel  Withdrawal 1003*/
            string UserName = null;
            try
            {
                using (MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
                {
                    conn.Open();
                    using (var trans = conn.BeginTransaction())
                    {
                        string sForStatus = Newtonsoft.Json.JsonConvert.SerializeObject(forStatus);
                        var p = new DynamicParameters(new { WithdrawalId = withdrawalId, SiteId = siteId, Comment = "Customer Cancelled", ForStatus = sForStatus, Status = (int)status, WithdrawalInfo = (string)null, WithdrawalType = (int?)null, RefId = (string)null });
                        p.Add("CustId", dbType: DbType.Int32, direction: ParameterDirection.Output);
                        p.Add("UserName", dbType: DbType.String, direction: ParameterDirection.Output);
                        p.Add("Amount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
                        result = conn.Execute("WithdrawalInfo_Set", p, trans, commandType: CommandType.StoredProcedure);
                        custId = p.Get<int?>("CustId") ?? 0;
                        UserName = p.Get<string>("UserName");
                        decimal amount = p.Get<decimal>("Amount");
                        if (result > 0)
                        {
                            SetWithdrawalLog(withdrawalId, siteId, "Customer Cancelled", (int)status, UserName);

                            if (status == WithdrawalStatus.Cancelled)
                            {
                                byte is1stDepoist = 0;
                                decimal afterAmount = 0;

                                result = walletService.SetBalance(custId, siteId, UserName, amount, type, $"Withdrawal Canceled: {amount}", transId, out afterAmount, out is1stDepoist, conn, trans);
                            }
                            
                            if (result > 0)
                            {
                                trans.Commit();
                                //result = p.Get<int?>("CustId") ?? result;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SetWithdrawalLog(withdrawalId, siteId, ex.Message, (int)WithdrawalStatus.Error, UserName);
            }

            return result;
        }

        private int SetWithdrawalLog(int withdrawalId, int siteId, string comment, int status, string Operator)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Execute("WithdrawalLog_Ins", new { WithdrawalId = withdrawalId, SiteId = siteId, Comment = comment, Action = status, Operator }, commandType: CommandType.StoredProcedure);
        }
        public decimal GetWithdrawalAvailableBalance(int custId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction(IsolationLevel.ReadUncommitted);
            var result = GetWithdrawalAvailableBalance(custId, conn, trans);
            trans.Rollback();
            return result;
        }
        public decimal GetWithdrawalAvailableBalance(int custId, MySqlConnection conn, MySqlTransaction trans)
        {
            return conn.ExecuteScalar<decimal>("Common_WithdrawalAvailableBalance_Get", new { CustId = custId }, transaction: trans, commandType: CommandType.StoredProcedure);
        }
    }
}
