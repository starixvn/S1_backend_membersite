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
        public int WithdrawalReq(int custId, string userName, int siteId, decimal amount, string memo, WithdrawalInfo withdrawalInfo, out decimal afterAmount, out string transId)
        {
            transId = "";

            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using MySqlTransaction trans = conn.BeginTransaction();
            var p = new DynamicParameters(new { CustId = custId, UserName = userName, SiteId = siteId, Amount = amount, Memo = memo, WithdrawalInfo = Newtonsoft.Json.JsonConvert.SerializeObject(withdrawalInfo), Prefix = $"W{common.GetEnv()}" });
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
    }
}
