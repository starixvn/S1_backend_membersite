using System.Collections.Generic;
using System.Data;
using Dapper;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;

namespace MemberSite.Service.DAL
{
    public class BonusWalletService
    {
        
        private readonly IConfiguration configuration;
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;
        private int typePosition = 1000;

        public BonusWalletService(IConfiguration configuration, ILogger<MySqlClient> logger)
        {
            this.configuration = configuration;
            _mainDbClient = new MySqlClient(configuration.GetConnectionString("ConnectionString"), logger);
            _reportDbClient = new MySqlClient(configuration.GetConnectionString("ConnectionStringReport"), logger);
        }
        public bool SetBonusTransferOutAll(int custId, int siteId, string userName, int tpId, string Memo, string RefTransId, out decimal Bonus, out decimal LockedAmount, out decimal BonusAmount, out long ExpiryTime, MySqlConnection conn, MySqlTransaction trans)
        {
            int type = typePosition + tpId;
            Bonus = 0;
            LockedAmount = 0;
            BonusAmount = 0;
            ExpiryTime = 0;

            var p = new DynamicParameters(new { CustId = custId, SiteId = siteId, UserName = userName, Type = type, Memo, RefTransId });
            p.Add("Bonus", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("LockedAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("BonusAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("ExpiryTime", dbType: DbType.Int64, direction: ParameterDirection.Output);
            conn.Execute("Common_BonusTransferOutAll_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);
            Bonus = p.Get<decimal>("Bonus");
            LockedAmount = p.Get<decimal>("LockedAmount");
            BonusAmount = p.Get<decimal>("BonusAmount");
            ExpiryTime = p.Get<long>("ExpiryTime");
            return Bonus > 0m;
        }
        public bool SetBonusByBonusId(int bonusId, int custId, int siteId, string userName, decimal Bonus, int type, string Memo, string RefTransId, out decimal afterBonus, MySqlConnection conn, MySqlTransaction trans)
        {
            var p = new DynamicParameters(new { CustId = custId, SiteId = siteId, UserName = userName, Bonus = Bonus, Type = type, Memo = Memo, RefTransId = RefTransId });
            p.Add("AfterBonus", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            conn.Execute("Common_BonusByBonusId_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);
            afterBonus = p.Get<decimal>("AfterBonus");
            return afterBonus >= 0;
        }
        public bool RewardBonus(int bonusId, int promoCategoryId, int tpId, int custId, int siteId, string userName, decimal bonusAmount, decimal lockedAmount, decimal rollover, long expiryTime, int type, string Memo, string RefTransId, MySqlConnection conn, MySqlTransaction trans)
        {
            var p = new DynamicParameters(new { BonusId = bonusId, PromoCategoryId = promoCategoryId, TpId = tpId, CustId = custId, SiteId = siteId, UserName = userName, BonusAmount = bonusAmount, LockedAmount = lockedAmount, Rollover = rollover, ExpiryTime = expiryTime, Type = type, Memo = Memo, RefTransId = RefTransId });
            return conn.Execute("Common_Bonus_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure) > 0;
        }
        public bool RewardRebate(string bonusCode, int rebateType, int custId, string userName, int siteId, int currencyId, MySqlConnection conn, MySqlTransaction trans)
        {
            var p = new DynamicParameters(new { BonusCode = bonusCode, RebateType = rebateType, CustId = custId, UserName = userName, SiteId = siteId, CurrencyId = currencyId });
            return conn.Execute("Common_Rebate_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure) > 0;
        }

        public bool RewardCashBack(CustCashBackInput custCashBackInput, MySqlTransaction trans)
        {
            var parameter = new DynamicParameters(new
            {
                custCashBackInput.CustId,
                custCashBackInput.BonusCode,
                custCashBackInput.PromoCategoryId,
                custCashBackInput.CalculateDate
            });
            return _mainDbClient.Execute("Member_CustCashBack_Set", parameter, trans);
        }

        public IEnumerable<BonusWallet> GetCustBonusWallet(int CustId)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Query<BonusWallet>("Member_CustBonus_Get", new { CustId }, commandType: CommandType.StoredProcedure);
        }

        public BonusWallet CheckBonusWallet(int CustId, int TpId)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.QueryFirstOrDefault<BonusWallet>("Member_CustBonus_Check", new { CustId, TpId }, commandType: CommandType.StoredProcedure);
        }
        public BonusWallet CheckBonusWallet(int CustId, string bonusCode)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.QueryFirstOrDefault<BonusWallet>("Member_CustBonusByCode_Get", new { CustId, BonusCode = bonusCode }, commandType: CommandType.StoredProcedure);
        }
        public BonusWallet CustBonusInfo(int CustId, string BonusCode)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.QueryFirstOrDefault<BonusWallet>("Member_CustBonusInfo_Get", new { CustId, BonusCode }, commandType: CommandType.StoredProcedure);
        }

        public BonusWallet CustBonusInfo(int CustId, string BonusCode, int TpId, int PromoCategoryId)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.QueryFirstOrDefault<BonusWallet>("Member_CustBonus_GetById", new { CustId, BonusCode, TpId, PromoCategoryId }, commandType: CommandType.StoredProcedure);
        }
    }
}
