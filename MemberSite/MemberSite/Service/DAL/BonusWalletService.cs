using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MemberSite.Service.DAL
{
    public class BonusWalletService
    {
        private readonly IConfiguration configuration;
        private int typePosition = 1000;
        public BonusWalletService(IConfiguration configuration)
        {
            this.configuration = configuration;
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
        public bool SetBonus(int custId, int siteId, string userName, decimal Bonus, int type, string Memo, string RefTransId, out decimal afterBonus, MySqlConnection conn, MySqlTransaction trans)
        {
            var p = new DynamicParameters(new { CustId = custId, SiteId = siteId, UserName = userName, Bonus = Bonus, Type = type, Memo = Memo, RefTransId = RefTransId });
            p.Add("AfterBonus", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            conn.Execute("Common_BonusWallet_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);
            afterBonus = p.Get<decimal>("AfterBonus");
            return afterBonus >= 0;
        }
        public bool RewardBonus(int custId, int siteId, string userName, decimal bonusAmount, decimal lockedAmount, decimal rollover, long expiryTime, int type, string Memo, string RefTransId, MySqlConnection conn, MySqlTransaction trans)
        {
            var p = new DynamicParameters(new { CustId = custId, SiteId = siteId, UserName = userName, BonusAmount = bonusAmount, LockedAmount = lockedAmount, Rollover = rollover, ExpiryTime = expiryTime, Type = type, Memo = Memo, RefTransId = RefTransId });
            return conn.Execute("Common_BonusWallet_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure) > 0;
        }
    }
}
