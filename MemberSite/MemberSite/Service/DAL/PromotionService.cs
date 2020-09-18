using Dapper;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MemberSite.Service.DAL
{
    public class PromotionService
    {
        private IConfiguration configuration;
        private readonly Common common;
        private readonly RolloverService rollOverService;
        private readonly WalletService walletService;
        private readonly BonusWalletService bonusWalletService;
        private readonly WithdrawalLimitService withdrawalLimitService;
        private readonly Tools tools;
        private readonly int[] hasBonusWallet;

        public PromotionService(IConfiguration configuration, Common common, WalletService walletService, RolloverService rollOverService, BonusWalletService bonusWalletService, WithdrawalLimitService withdrawalLimitService, Tools tools)
        {
            this.configuration = configuration;
            this.common = common;
            this.rollOverService = rollOverService;
            this.walletService = walletService;
            this.bonusWalletService = bonusWalletService;
            this.withdrawalLimitService = withdrawalLimitService;
            this.tools = tools;
            hasBonusWallet = configuration.GetSection("HasBonusWallet").Get<int[]>();
        }
        public IEnumerable<Promotion> GetPromotionList(int siteId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Query<Promotion>("Member_PromotionList_Get", new { SiteId = siteId }, commandType: CommandType.StoredProcedure);
        }
        public IEnumerable<Promotion> GetPromotionListByCustId(int siteId, int custId, int currencyId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Query<Promotion>("Member_PromotionListByCustId_Get", new { SiteId = siteId, CustId = custId, CurrencyId = currencyId }, commandType: CommandType.StoredProcedure);
        }
        public Promotion GetPromotion(int siteId, string bonusCode, int currencyId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.QueryFirstOrDefault<Promotion>("Member_Promotion_Get", new { SiteId = siteId, BonusCode = bonusCode, CurrencyId = currencyId }, commandType: CommandType.StoredProcedure);
        }
        public int SetClaimPromotion(int custId, string userName, int siteId, string bonusCode, int currencyId, out decimal bonusAmount, out decimal lockedAmount, out decimal rollOver, out long expiryTime, out Promotion promotion)
        {
            bonusAmount = 0;
            lockedAmount = 0;
            rollOver = 0;
            expiryTime = 0;
            bool bHasBonusWallet = true;/*Never roll back*/

            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();

            promotion = conn.QueryFirstOrDefault<Promotion>("Member_Promotion_Get", new { SiteId = siteId, BonusCode = bonusCode, CurrencyId = currencyId }, transaction: trans, commandType: CommandType.StoredProcedure);
            if (promotion != null)
            {
                if (promotion.PromotionType == PromotionType.Freebet)
                {
                    var p1 = new DynamicParameters(new { BonusCode = bonusCode, SiteId = siteId, IP = tools.GetClientIP() });
                    p1.Add("IsBlocked", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    using var connBlockFreeBet = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
                    connBlockFreeBet.Execute("Service_BlockFreeBet_Get", p1, commandType: CommandType.StoredProcedure);
                    if (p1.Get<int>("IsBlocked") == 1)
                    {
                        return -3;
                    }
                }

                int result = 0;
                var p = new DynamicParameters(new { CustId = custId, CurrencyId = currencyId, SiteId = siteId, BonusCode = bonusCode });
                p.Add("CanJoin", dbType: DbType.Boolean, direction: ParameterDirection.Output);
                p.Add("Amount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
                conn.Execute("Member_PromotionCanJoinV2_Get", p, commandType: CommandType.StoredProcedure);

                var canJoin = p.Get<byte?>("CanJoin") ?? 0;
                var amount = p.Get<decimal?>("Amount") ?? 0;
                if (canJoin == 1)
                {
                    decimal dBonus = 0;
                    decimal dLockedAmount = amount;
                    decimal dRollOver = 0;
                    string sBonusInfo = "";

                    if (promotion.PromotionType == PromotionType.FstDeposit || promotion.PromotionType == PromotionType.ReDeposit)
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

                        bonusAmount = dBonus;
                        lockedAmount = dLockedAmount;
                        rollOver = dRollOver;
                        expiryTime = DateTimeOffset.Now.AddDays(promotion.ExpiryDays).ToUnixTimeSeconds();
                    }
                    else if (promotion.PromotionType == PromotionType.Freebet)
                    {
                        dBonus = promotion.Bonus;
                        dRollOver = dBonus * promotion.RollOver;
                        sBonusInfo = Newtonsoft.Json.JsonConvert.SerializeObject(promotion, new Newtonsoft.Json.JsonSerializerSettings { NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore });

                        bonusAmount = dBonus;
                        lockedAmount = 0;
                        rollOver = dRollOver;
                        expiryTime = DateTimeOffset.Now.AddDays(promotion.ExpiryDays).ToUnixTimeSeconds();
                        conn.Execute("Service_BlockFreeBet_Ins", new { BonusCode = bonusCode, SiteId = siteId, IP = tools.GetClientIP() }, transaction: trans, commandType: CommandType.StoredProcedure);
                    }
                    else if (promotion.PromotionType == PromotionType.WalletBonus)
                    {
                        if (promotion.RequiredAmount == 0)
                        {
                            throw new Exception("Required Amount can't be 0");
                        }

                        amount = walletService.GetBalance(custId);

                        if (amount < promotion.RequiredAmount)
                        {
                            lockedAmount = promotion.RequiredAmount;
                            return -2;
                        }

                        if (promotion.BonusType == BonusType.Amount)
                        {
                            dBonus = promotion.Bonus;
                            dLockedAmount = promotion.RequiredAmount;
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

                        bonusAmount = dBonus;
                        lockedAmount = dLockedAmount;
                        dRollOver = ((dLockedAmount + dBonus) * promotion.RollOver);
                        sBonusInfo = Newtonsoft.Json.JsonConvert.SerializeObject(promotion, new Newtonsoft.Json.JsonSerializerSettings { NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore });

                        rollOver = dRollOver;
                        expiryTime = DateTimeOffset.Now.AddDays(promotion.ExpiryDays).ToUnixTimeSeconds();
                    }
                    else
                    {
                        return 0;
                    }

                    p = new DynamicParameters(new { PromotionType = (int)promotion.PromotionType, BonusCode = bonusCode, CustId = custId, UserName = userName, CurrencyId = currencyId, SiteId = siteId, BonusInfo = sBonusInfo, LockedAmount = dLockedAmount, BonusAmount = dBonus, ExpiryDays = promotion.ExpiryDays, Memo = (string)null, Prefix = $"B{common.GetEnv()}" });
                    p.Add("TransId", dbType: DbType.String, direction: ParameterDirection.Output);
                    result = conn.Execute("Member_BonusInfo_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);
                    var transId = p.Get<string>("TransId");

                    if (bHasBonusWallet)
                    {
                        if (result > 0 && dLockedAmount > 0)
                        {
                            byte is1stDepoist = 0;
                            decimal afterAmount = 0;
                            int type = 5/*Bonus*/;
                            result = walletService.SetBalance(custId, siteId, userName, dLockedAmount * -1, type, $"Claim Promotion BonusCode: {bonusCode}, Locked Amount: {dLockedAmount.ToString("#,##0.##")}", transId, out afterAmount, out is1stDepoist, conn, trans);

                            if (result > 0)
                            {
                                withdrawalLimitService.SetWithdrawalLimit(custId, userName, siteId, dLockedAmount * -1, null, transId, out decimal afterWithdrawalLimit, conn, trans);
                            }
                            else
                            {
                                return -2;
                            }
                        }

                        if (result > 0)
                        {
                            int bonusId = 0;

                            Match m = Regex.Match(transId, @"\d+");
                            int.TryParse(m.Value, out bonusId);

                            result = bonusWalletService.RewardBonus(bonusId, promotion.CategoryId, promotion.TpId, custId, siteId, userName, dBonus, dLockedAmount, dRollOver, expiryTime, (int)promotion.PromotionType, $"Claim Promotion BonusCode: {bonusCode}", transId, conn, trans) ? 1 : 0;
                        }
                    }
                    else
                    {
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
                            result = rollOverService.SetRollOver(custId, siteId, userName, bonusCode, dRollOver, $"Bonus RollOver: {dRollOver}", transId, out afterRollOver, conn, trans);
                        }
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
            conn.Execute("Member_PromotionCanJoinV2_Get", p, commandType: CommandType.StoredProcedure);
            var canJoin = p.Get<byte?>("CanJoin") ?? 0;
            return canJoin == 1;
        }

        public List<CustPromotion> GetPromotionListv2(int SiteId, int CustId, int CurrencyId, bool IsLogin, string langId, string defLang)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();

            var promotionList = conn.Query<CustPromotion>("Member_PromotionListV2_Get", new { SiteId, CurrencyId }, trans, commandType: CommandType.StoredProcedure);

            List<CustPromotion> _promotion = new List<CustPromotion>();

            foreach (var promo in promotionList)
            {
                if (IsLogin)
                {
                    var canJoinParams = new DynamicParameters(new { CustId, CurrencyId, SiteId, BonusCode = promo.BonusCode });
                    canJoinParams.Add("CanJoin", dbType: DbType.Boolean, direction: ParameterDirection.Output);
                    canJoinParams.Add("Amount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
                    conn.Execute("Member_PromotionCanJoinV2_Get", canJoinParams, trans, commandType: CommandType.StoredProcedure);

                    var canJoinResult = canJoinParams.Get<byte?>("CanJoin") ?? 0;
                    promo.canJoin = canJoinResult == 1;


                    BonusWallet custBonusInfo = conn.QueryFirstOrDefault<BonusWallet>("Member_BonusInfo_GetByCust", new { promo.BonusCode, PromoCategoryId = promo.CategoryId, TpId = promo.TpId, CustId }, trans, commandType: CommandType.StoredProcedure);
                    if (custBonusInfo != null)
                    {
                        promo.CustExpiryTime = custBonusInfo.ExpiryTime;
                        promo.CustRollOver = custBonusInfo.Rollover;
                        promo.CustTurnOver = custBonusInfo.Turnover;
                        promo.Status = custBonusInfo.Status;
                        promo.isJoin = true;
                    }
                }

                if (!string.IsNullOrEmpty(promo.Details))
                {
                    var details = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(promo.Details);
                    Dictionary<string, string> settings = null;
                    if (details.TryGetValue(langId, out settings) || details.TryGetValue(defLang, out settings))
                    {

                    }
                    else
                    {
                        foreach (var item in details)
                        {
                            settings = item.Value;
                            break;
                        }
                    }

                    promo.Details = null;
                    if (settings != null)
                    {
                        settings.TryGetValue("title", out var title);
                        settings.TryGetValue("bannerUrl", out var bannerUrl);
                        settings.TryGetValue("description", out var description);

                        promo.Title = title;
                        promo.Banner = bannerUrl;
                        promo.Description = description;
                    }
                }

                _promotion.Add(promo);
            }

            trans.Commit();

            return _promotion;
        }

        public CustPromotion GetPromotionDetail(int SiteId, int CustId, int CurrencyId, bool IsLogin, string langId, string defLang, string BonusCode)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();

            var promo = conn.QueryFirstOrDefault<CustPromotion>("Member_PromotionV2_Get", new { SiteId, CurrencyId, BonusCode }, trans, commandType: CommandType.StoredProcedure);

            if (promo != null)
            {
                if (!string.IsNullOrEmpty(promo.Details))
                {
                    var details = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(promo.Details);
                    Dictionary<string, string> settings = null;
                    if (details.TryGetValue(langId, out settings) || details.TryGetValue(defLang, out settings))
                    {

                    }
                    else
                    {
                        foreach (var item in details)
                        {
                            settings = item.Value;
                            break;
                        }
                    }

                    if (settings != null)
                    {
                        settings.TryGetValue("title", out var title);
                        settings.TryGetValue("bannerUrl", out var bannerUrl);
                        settings.TryGetValue("description", out var description);
                        settings.TryGetValue("detail", out var detail);

                        promo.Title = title;
                        promo.Banner = bannerUrl;
                        promo.Description = description;
                        promo.Details = detail;
                    }
                }

                if (IsLogin)
                {
                    var canJoinParams = new DynamicParameters(new { CustId, CurrencyId, SiteId, BonusCode = promo.BonusCode });
                    canJoinParams.Add("CanJoin", dbType: DbType.Boolean, direction: ParameterDirection.Output);
                    canJoinParams.Add("Amount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
                    conn.Execute("Member_PromotionCanJoinV2_Get", canJoinParams, trans, commandType: CommandType.StoredProcedure);

                    var canJoinResult = canJoinParams.Get<byte?>("CanJoin") ?? 0;
                    promo.canJoin = canJoinResult == 1;

                    BonusWallet custBonusInfo = conn.QueryFirstOrDefault<BonusWallet>("Member_BonusInfo_GetByCust", new { promo.BonusCode, PromoCategoryId = promo.CategoryId, TpId = promo.TpId, CustId }, trans, commandType: CommandType.StoredProcedure);
                    if (custBonusInfo != null)
                    {
                        promo.CustExpiryTime = custBonusInfo.ExpiryTime;
                        promo.CustRollOver = custBonusInfo.Rollover;
                        promo.CustTurnOver = custBonusInfo.Turnover;
                        promo.Status = custBonusInfo.Status;
                        promo.isJoin = true;
                    }
                }
            }

            trans.Commit();

            return promo;
        }
    }
}
