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

namespace MemberSite.Service.DAL
{
    public class PromotionService
    {
        private IConfiguration configuration;
        private readonly Common common;
        private readonly RolloverService rollOverService;
        private readonly WalletService walletService;

        public PromotionService(IConfiguration configuration, Common common, WalletService walletService, RolloverService rollOverService)
        {
            this.configuration = configuration;
            this.common = common;
            this.rollOverService = rollOverService;
            this.walletService = walletService;
        }
        public IEnumerable<Promotion> GetPromotionList(int siteId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Query<Promotion>("Member_PromotionList_Get", new { SiteId = siteId }, commandType: CommandType.StoredProcedure);
        }
        public IEnumerable<Promotion> GetPromotionListByCustId(int siteId, int custId,int currencyId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Query<Promotion>("Member_PromotionListByCustId_Get", new { SiteId = siteId, CustId = custId, CurrencyId = currencyId }, commandType: CommandType.StoredProcedure);
        }
        public Promotion GetPromotion(int siteId, string bonusCode, int currencyId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.QueryFirstOrDefault<Promotion>("Member_Promotion_Get", new { SiteId = siteId, BonusCode = bonusCode, CurrencyId = currencyId }, commandType: CommandType.StoredProcedure);
        }
        public int SetClaimPromotion(int custId, string userName, int siteId, string bonusCode, int currencyId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            var promotion = conn.QueryFirstOrDefault<Promotion>("Member_Promotion_Get", new { SiteId = siteId, BonusCode = bonusCode, CurrencyId = currencyId }, transaction: trans, commandType: CommandType.StoredProcedure);
            if (promotion != null)
            {
                int result = 0;
                var p = new DynamicParameters(new { CustId = custId, CurrencyId = currencyId, SiteId = siteId, BonusCode = bonusCode });
                p.Add("CanJoin", dbType: DbType.Boolean, direction: ParameterDirection.Output);
                p.Add("Amount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
                conn.Execute("Member_PromotionCanJoin_Get", p, transaction: trans, commandType: CommandType.StoredProcedure);
                var canJoin = p.Get<byte?>("CanJoin") ?? 0;
                var amount = p.Get<decimal?>("Amount") ?? 0;
                if (canJoin == 1)
                {
                    decimal dBonus = 0;
                    decimal dLockedAmount = amount;
                    decimal dRollOver = 0;
                    string sBonusInfo = "";

                    if (promotion.PromotionType == PromotionType.FstDeposit)
                    {
                        if (promotion.BonusType == BonusType.Amount)
                        {
                            dBonus = promotion.Bonus;
                        }
                        else
                        {
                            dBonus = Math.Ceiling(promotion.Bonus * amount / 100);
                            if (dBonus > promotion.MaxBonus)
                            {
                                dBonus = promotion.MaxBonus;
                                dLockedAmount = promotion.MaxBonus * 100 / promotion.Bonus;
                            }
                        }

                        dRollOver = ((dLockedAmount + dBonus) * promotion.RollOver);
                        sBonusInfo = Newtonsoft.Json.JsonConvert.SerializeObject(promotion, new Newtonsoft.Json.JsonSerializerSettings { NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore });
                    }
                    else
                    {
                        return 0;
                    }

                    p = new DynamicParameters(new { PromotionType = (int)promotion.PromotionType, BonusCode = bonusCode, CustId = custId, UserName = userName, CurrencyId = currencyId, SiteId = siteId, BonusInfo = sBonusInfo, LockedAmount = dLockedAmount, BonusAmount = dBonus, ExpiryDays = promotion.ExpiryDays, Memo = (string)null, Prefix = $"B{common.GetEnv()}" });
                    p.Add("TransId", dbType: DbType.String, direction: ParameterDirection.Output);
                    result = conn.Execute("Member_BonusInfo_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);
                    var transId = p.Get<string>("TransId");
                    if (result > 0)
                    {
                        byte is1stDepoist = 0;
                        decimal afterAmount = 0;
                        int type = 5/*Bonus*/;
                        result = walletService.SetBalance(custId, siteId, userName, dBonus, type, $"Claim Promotion BonusCode: {bonusCode}", transId, out afterAmount, out is1stDepoist, conn, trans);
                    }

                    if (result > 0)
                    {
                        decimal afterRollOver = 0;
                        result = rollOverService.SetRollOver(custId, siteId, userName, RollOverType.Bonus, bonusCode, dRollOver, $"Bonus RollOver: {dRollOver}", transId, out afterRollOver, conn, trans);
                    }

                    if (result > 0)
                    {
                        result = conn.Execute("Member_PromotionBeenUsed_Set", new { BonusCode = bonusCode, CurrencyId = currencyId, SiteId = siteId }, transaction: trans, commandType: CommandType.StoredProcedure);
                    }

                    if (result > 0)
                    {
                        trans.Commit();
                    }

                    return result;
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                return 0;
            }
        }

        public bool GetCanJoin(int custId, int siteId, string bonusCode, int currencyId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            var p = new DynamicParameters(new { CustId = custId, CurrencyId = currencyId, SiteId = siteId, BonusCode = bonusCode });
            p.Add("CanJoin", dbType: DbType.Boolean, direction: ParameterDirection.Output);
            p.Add("Amount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            conn.Execute("Member_PromotionCanJoin_Get", p, commandType: CommandType.StoredProcedure);
            var canJoin = p.Get<byte?>("CanJoin") ?? 0;
            return canJoin == 1;
        }
    }
}
