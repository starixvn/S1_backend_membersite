using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data;
using MemberSite.Service.Model;

namespace MemberSite.Service.DAL
{
    public class WalletService
    {
        private readonly IConfiguration configuration;

        public WalletService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public int SetBalance(int custId, int siteId, string userName, decimal Amount, int type, string Memo, string RefTransId, out decimal afterAmount, out byte is1stDepoist, MySqlConnection conn, MySqlTransaction trans)
        {
            var p = new DynamicParameters(new { CustId = custId, SiteId = siteId, UserName = userName, Amount = Amount, Type = type, Memo = Memo, RefTransId = RefTransId });
            p.Add("Is1stDepoist", dbType: DbType.Boolean, direction: ParameterDirection.Output);
            p.Add("AfterAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            conn.Execute("Wallet_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);
            is1stDepoist = p.Get<byte?>("Is1stDepoist") ?? 0;
            afterAmount = p.Get<decimal?>("AfterAmount") ?? 0;
            return afterAmount >= 0 ? 1 : 0;
        }
        public decimal GetBalance(int custId)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.ExecuteScalar<decimal>("Balance_Get", new { CustId = custId }, commandType: CommandType.StoredProcedure);
        }
        public decimal GetBonus(int custId, int promoCategoryId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.QueryFirstOrDefault<decimal>("Service_Bonus_Get", new { CustId = custId, PromoCategoryId = promoCategoryId }, commandType: CommandType.StoredProcedure);
        }
        public WalletInfo GetWalletInfo(int custId, int siteId)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            var walletInfo = conn.QueryFirstOrDefault<WalletInfo>("CustInfoLog1_Get", new { CustId = custId, SiteId = siteId }, commandType: CommandType.StoredProcedure);
            walletInfo.Balance = GetBalance(custId);

            return walletInfo;
        }
        public int GetWalletTransferStatus(int custId, out int autoTransfer)
        {
            autoTransfer = 0;
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            conn.Open();
            var p = new DynamicParameters(new { CustId = custId});
            p.Add("AutoTransfer", dbType: DbType.Int32, direction: ParameterDirection.Output);
            var result = conn.Execute("Member_CustWalletTransferStatus_Get", p, commandType: CommandType.StoredProcedure);
            autoTransfer = p.Get<int?>("AutoTransfer") ?? 0;
            return result >= 0 ? 1 : 0;

        }
        public int SetWalletTransferStatus(int custId, int autoTransfer)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            return conn.Execute("Member_CustWalletTransferStatus_Set", new { CustId = custId , AutoTransfer = autoTransfer }, commandType: CommandType.StoredProcedure) > 0 ? 1 : 0;
        }
    }
}
