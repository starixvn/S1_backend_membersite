using Dapper;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MemberSite.Service.BLL;
using Microsoft.Extensions.Logging;

namespace MemberSite.Service.DAL
{
    public class PromotionService
    {
        private IConfiguration configuration;
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;
        private readonly Common common;
        private readonly RolloverService rollOverService;
        private readonly WalletService walletService;
        private readonly BonusWalletService bonusWalletService;
        private readonly WithdrawalLimitService withdrawalLimitService;
        private readonly Tools tools;
        private readonly int[] hasBonusWallet;
        private readonly SelLangBLL _selLangBll;

        public PromotionService(IConfiguration configuration, Common common, WalletService walletService,
            RolloverService rollOverService, BonusWalletService bonusWalletService,
            WithdrawalLimitService withdrawalLimitService, Tools tools, ILogger<MySqlClient> logger,
            SelLangBLL selLangBll)
        {
            this.configuration = configuration;
            _mainDbClient = new MySqlClient(configuration.GetConnectionString("ConnectionString"), logger);
            _reportDbClient = new MySqlClient(configuration.GetConnectionString("ConnectionStringReport"), logger);
            this.common = common;
            this.rollOverService = rollOverService;
            this.walletService = walletService;
            this.bonusWalletService = bonusWalletService;
            this.withdrawalLimitService = withdrawalLimitService;
            this.tools = tools;
            hasBonusWallet = configuration.GetSection("HasBonusWallet").Get<int[]>();
            _selLangBll = selLangBll;
        }

        public IEnumerable<Promotion> GetPromotionList(int siteId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<Promotion>("Member_PromotionList_Get", new { SiteId = siteId }, commandType: CommandType.StoredProcedure);
        }
        public IEnumerable<Promotion> GetPromotionListByCustId(int siteId, int custId, int currencyId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<Promotion>("Member_PromotionListByCustId_Get", new { SiteId = siteId, CustId = custId, CurrencyId = currencyId }, commandType: CommandType.StoredProcedure);
        }
        public Promotion GetPromotion(int siteId, string bonusCode, int currencyId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.QueryFirstOrDefault<Promotion>("Member_Promotion_Get", new { SiteId = siteId, BonusCode = bonusCode, CurrencyId = currencyId }, commandType: CommandType.StoredProcedure);
        }

        public int SetClaimPromotion(int custId, string userName, int siteId, string bonusCode, int currencyId,
            out decimal bonusAmount, ref decimal lockedAmount, out decimal rollOver, out long expiryTime,
            out Promotion promotion)
        {
            bonusAmount = 0;
            rollOver = 0;
            expiryTime = 0;

            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();

            promotion = conn.QueryFirstOrDefault<Promotion>("Member_Promotion_Get",
                new {SiteId = siteId, BonusCode = bonusCode, CurrencyId = currencyId}, trans,
                commandType: CommandType.StoredProcedure);

            if (promotion == null)
            {
                return 0;
            }

            if (promotion.PromotionType == PromotionType.FreeBet)
            {
                var p1 = new DynamicParameters(new {BonusCode = bonusCode, SiteId = siteId, IP = tools.GetClientIP()});
                p1.Add("IsBlocked", dbType: DbType.Int32, direction: ParameterDirection.Output);
                using var connBlockFreeBet = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
                connBlockFreeBet.Execute("Service_BlockFreeBet_Get", p1, commandType: CommandType.StoredProcedure);
                if (p1.Get<int>("IsBlocked") == 1)
                {
                    return -3;
                }
            }

            var p = new DynamicParameters(new
                {CustId = custId, CurrencyId = currencyId, SiteId = siteId, BonusCode = bonusCode});
            p.Add("CanJoin", dbType: DbType.Boolean, direction: ParameterDirection.Output);
            p.Add("Amount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("RefId", dbType: DbType.String, direction: ParameterDirection.Output);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("RefList", dbType: DbType.String, direction: ParameterDirection.Output);
            conn.Execute("Member_PromotionCanJoinV4_Get", p, commandType: CommandType.StoredProcedure);

            var jResult = p.Get<int>("Result");
            var canJoin = p.Get<byte?>("CanJoin") ?? 0;
            var amount = p.Get<decimal?>("Amount") ?? 0;
            if (canJoin == 1)
            {
                decimal dBonus = 0;
                if (promotion.PromotionType == PromotionType.ReDeposit ||
                    (promotion.IsSpecial && promotion.PromotionType == PromotionType.FstDeposit))
                {
                    amount = promotion.BonusType == BonusType.Amount ? promotion.RequiredAmount : lockedAmount;

                    if (amount <= 0)
                    {
                        return 0;
                    }
                }

                var dLockedAmount = amount;
                decimal dRollOver = 0;
                var sBonusInfo = "";

                switch (promotion.PromotionType)
                {
                    case PromotionType.FstDeposit:
                    case PromotionType.ReDeposit:
                    {
                        if (amount < promotion.RequiredAmount)
                        {
                            return -4;
                        }

                        if (promotion.BonusType == BonusType.Amount)
                        {
                            if (promotion.RequiredAmount == 0)
                            {
                                throw new Exception("Required Amount can't be 0");
                            }

                            dLockedAmount = promotion.RequiredAmount;
                            dBonus = promotion.Bonus;
                        }
                        else
                        {
                            dBonus = Math.Ceiling(promotion.Bonus * amount / 100);
                            if (promotion.MaxBonus != 0 && dBonus > promotion.MaxBonus)
                            {
                                dBonus = promotion.MaxBonus;
                                dLockedAmount = promotion.MaxBonus * 100 / promotion.Bonus;
                            }
                        }

                        dRollOver = ((dLockedAmount + dBonus) * promotion.RollOver);
                        sBonusInfo = JsonConvert.SerializeObject(promotion,
                            new JsonSerializerSettings {NullValueHandling = NullValueHandling.Ignore});

                        bonusAmount = dBonus;
                        lockedAmount = dLockedAmount;
                        rollOver = dRollOver;
                        expiryTime = DateTimeOffset.Now.AddDays(promotion.ExpiryDays).ToUnixTimeSeconds();
                        break;
                    }
                    
                    case PromotionType.Cashback:
                        var custPromotion = new CustPromotion
                        {
                            BonusCode = promotion.BonusCode,
                            CategoryId = promotion.CategoryId
                        };
                        GetCashbackInfo(custId, siteId, currencyId, custPromotion, true);
                        if (!(custPromotion.canJoin && custPromotion.CashBackDetails?.TotalBonus > promotion.MinPayout))
                        {
                            return 0;
                        }

                        dBonus = custPromotion.CashBackDetails?.TotalBonus ?? 0;
                        if (promotion.MaxBonus != 0 && dBonus > promotion.MaxBonus)
                        {
                            dBonus = promotion.MaxBonus;
                        }

                        dRollOver = ((dLockedAmount + dBonus) * promotion.RollOver);
                        sBonusInfo = JsonConvert.SerializeObject(promotion,
                            new JsonSerializerSettings {NullValueHandling = NullValueHandling.Ignore});
                        bonusAmount = dBonus;
                        lockedAmount = 0;
                        rollOver = dRollOver;
                        expiryTime = DateTimeOffset.Now.AddDays(promotion.ExpiryDays).ToUnixTimeSeconds();
                        break;

                    case PromotionType.FreeBet:
                        dBonus = promotion.Bonus;
                        dRollOver = dBonus * promotion.RollOver;
                        sBonusInfo = JsonConvert.SerializeObject(promotion,
                            new JsonSerializerSettings {NullValueHandling = NullValueHandling.Ignore});

                        bonusAmount = dBonus;
                        lockedAmount = 0;
                        rollOver = dRollOver;
                        expiryTime = DateTimeOffset.Now.AddDays(promotion.ExpiryDays).ToUnixTimeSeconds();
                        conn.Execute("Service_BlockFreeBet_Ins",
                            new {BonusCode = bonusCode, SiteId = siteId, IP = tools.GetClientIP()}, trans,
                            commandType: CommandType.StoredProcedure);
                        break;

                    case PromotionType.WalletBonus when promotion.RequiredAmount == 0:
                        throw new Exception("Required Amount can't be 0");

                    case PromotionType.WalletBonus:
                    {
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
                            if (promotion.MaxBonus != 0 && dBonus > promotion.MaxBonus)
                            {
                                dBonus = promotion.MaxBonus;
                                dLockedAmount = promotion.MaxBonus * 100 / promotion.Bonus;
                            }
                        }

                        bonusAmount = dBonus;
                        lockedAmount = dLockedAmount;
                        dRollOver = ((dLockedAmount + dBonus) * promotion.RollOver);
                        sBonusInfo = JsonConvert.SerializeObject(promotion,
                            new JsonSerializerSettings {NullValueHandling = NullValueHandling.Ignore});

                        rollOver = dRollOver;
                        expiryTime = DateTimeOffset.Now.AddDays(promotion.ExpiryDays).ToUnixTimeSeconds();
                        break;
                    }

                    case PromotionType.Rebate:
                        break;

                    default:
                        return 0;
                }

                var result = 0;
                switch (promotion.PromotionType)
                {
                    case PromotionType.FstDeposit:
                    case PromotionType.ReDeposit:
                    case PromotionType.FreeBet:
                    case PromotionType.WalletBonus:
                    case PromotionType.Cashback:
                    {
                        p = new DynamicParameters(new
                        {
                            PromotionType = (int) promotion.PromotionType, BonusCode = bonusCode, CustId = custId,
                            UserName = userName, CurrencyId = currencyId, SiteId = siteId, BonusInfo = sBonusInfo,
                            LockedAmount = dLockedAmount, BonusAmount = dBonus,
                            promotion.ExpiryDays, Memo = (string) null, Prefix = $"B{common.GetEnv()}"
                        });
                        p.Add("TransId", dbType: DbType.String, direction: ParameterDirection.Output);
                        result = conn.Execute("Member_BonusInfo_Ins", p, trans, commandType: CommandType.StoredProcedure);
                        var transId = p.Get<string>("TransId");
                    
                        if (result > 0 && dLockedAmount > 0)
                        {
                            var type = 5 /*Bonus*/;
                            result = walletService.SetBalance(custId, siteId, userName, dLockedAmount * -1, type,
                                $"Claim Promotion BonusCode: {bonusCode}, Locked Amount: {dLockedAmount:#,##0.##}", transId,
                                out _, out _, conn, trans);

                            if (result > 0)
                            {
                                withdrawalLimitService.SetWithdrawalLimit(custId, userName, siteId, dLockedAmount * -1,
                                    null, transId, out _, conn, trans);
                            }
                            else
                            {
                                return -2;
                            }
                        }

                        if (result > 0)
                        {
                            var m = Regex.Match(transId, @"\d+");
                            int.TryParse(m.Value, out var bonusId);

                            result = bonusWalletService.RewardBonus(bonusId, promotion.CategoryId, promotion.TpId, custId,
                                siteId, userName, dBonus, dLockedAmount, dRollOver, expiryTime, 5,
                                $"Claim Promotion BonusCode: {bonusCode}", transId, conn, trans)
                                ? 1
                                : 0;
                        }

                        if (result > 0 && promotion.PromotionType == PromotionType.Cashback)
                        {
                            var currentTime = DateTime.UtcNow.AddHours(8);
                            var calculateDate = currentTime
                                .AddDays(-(int) DateTime.Today.DayOfWeek + (int) DayOfWeek.Monday)
                                .ToString("yyyy-MM-dd");

                            result = bonusWalletService.RewardCashBack(
                                new CustCashBackInput(custId, promotion.BonusCode, promotion.CategoryId, calculateDate),
                                trans)
                                ? 1
                                : 0;
                        }

                        break;
                    }
                    
                    case PromotionType.Rebate:
                        result = bonusWalletService.RewardRebate(promotion.BonusCode, (int) promotion.PromotionType - 5,
                            custId, userName, siteId, currencyId, conn, trans)
                            ? 1
                            : 0;
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (result > 0)
                {
                    result = conn.Execute("Member_PromotionBeenUsed_Set",
                        new {SiteId = siteId, BonusCode = bonusCode, CurrencyId = currencyId}, trans,
                        commandType: CommandType.StoredProcedure);
                }

                if (result > 0)
                {
                    trans.Commit();
                }

                return result;
            }

            if (jResult == 3)
            {
                return -5;
            }

            return 0;

        }

        public bool GetCanJoin(int custId, int siteId, string bonusCode, int currencyId, out int result, out string depositId, out decimal dpositAmount)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            var p = new DynamicParameters(new { CustId = custId, CurrencyId = currencyId, SiteId = siteId, BonusCode = bonusCode });
            p.Add("CanJoin", dbType: DbType.Boolean, direction: ParameterDirection.Output);
            p.Add("Amount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("RefId", dbType: DbType.String, direction: ParameterDirection.Output);
            p.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("RefList", dbType: DbType.String, direction: ParameterDirection.Output);
            conn.Execute("Member_PromotionCanJoinV4_Get", p, commandType: CommandType.StoredProcedure);
            var canJoin = p.Get<byte?>("CanJoin") ?? 0;
            result = p.Get<int>("Result");
            depositId = p.Get<string>("RefId");
            dpositAmount = p.Get<decimal?>("Amount") ?? 0m;

            return canJoin == 1;
        }

        public List<CustPromotion> GetPromotionListv2(int SiteId, int CustId, int CurrencyId, bool IsLogin, string langId, string defLang)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();

            var promotionList = conn.Query<CustPromotion>("Member_PromotionListV2_Get", new { CustId, SiteId, CurrencyId }, trans, commandType: CommandType.StoredProcedure);

            var promotion = new List<CustPromotion>();

            foreach (var promo in promotionList)
            {
                if (IsLogin)
                {
                    var canJoinParams = new DynamicParameters(new { CustId, CurrencyId, SiteId, BonusCode = promo.BonusCode });
                    canJoinParams.Add("CanJoin", dbType: DbType.Boolean, direction: ParameterDirection.Output);
                    canJoinParams.Add("Amount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
                    canJoinParams.Add("RefId", dbType: DbType.String, direction: ParameterDirection.Output);
                    canJoinParams.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    canJoinParams.Add("RefList", dbType: DbType.String, direction: ParameterDirection.Output);
                    conn.Execute("Member_PromotionCanJoinV4_Get", canJoinParams, commandType: CommandType.StoredProcedure);

                    var canJoinResult = canJoinParams.Get<byte?>("CanJoin") ?? 0;
                    promo.canJoin = canJoinResult == 1;
                    promo.Result = canJoinParams.Get<int>("Result");
                    
                    if ((promo.PromotionType == (int)PromotionType.FstDeposit || promo.PromotionType == (int)PromotionType.ReDeposit) && promo.Result == 2)
                    {
                        promo.DepositId = canJoinParams.Get<string>("RefId");
                        promo.DepositAmount = canJoinParams.Get<decimal>("Amount");
                        promo.RefList = JsonConvert.DeserializeObject<IEnumerable<Ref>>(canJoinParams.Get<string>("RefList"));
                        
                        promo.MaxAmount = promo.PromotionType == (int) PromotionType.FstDeposit
                            ? walletService.GetBalance(CustId)
                            : promo.DepositAmount;
                        if (promo.RequiredAmount == 0)
                        {
                            promo.RequiredAmount = 1;
                        }

                        if (promo.BonusType == (int)BonusType.Amount)
                        {
                            promo.MaxAmount = promo.RequiredAmount;
                        }
                        else
                        {
                            if (promo.MaxBonus > 0)
                            {
                                var dBonus = Math.Ceiling(promo.Bonus * promo.DepositAmount / 100);
                                if (dBonus > promo.MaxBonus)
                                {
                                    promo.MaxAmount = promo.MaxBonus * 100 / promo.Bonus;
                                }
                            }
                        }
                    }

                    if (promo.PromotionType == (int)PromotionType.Rebate)
                    {
                        promo.isJoin = conn.Execute("Member_RebateInfo_GetByCust", new { promo.BonusCode, RebateType = promo.PromotionType - 5, CustId }, trans, commandType: CommandType.StoredProcedure) > 0;
                    }
                    else
                    {
                        var custBonusInfo = conn.QueryFirstOrDefault<BonusWallet>("Member_BonusInfo_GetByCust", new { promo.BonusCode, PromoCategoryId = promo.CategoryId, TpId = promo.TpId, CustId }, trans, commandType: CommandType.StoredProcedure);
                        if (custBonusInfo != null)
                        {
                            promo.CustExpiryTime = custBonusInfo.ExpiryTime;
                            promo.CustRollOver = custBonusInfo.Rollover;
                            promo.CustTurnOver = custBonusInfo.Turnover;
                            promo.Status = custBonusInfo.Status;
                            promo.isJoin = true;
                        }

                        if (promo.PromotionType == (int) PromotionType.Cashback)
                        {
                            GetCashbackInfo(CustId, SiteId, CurrencyId, promo, false);
                        }
                    }
                }

                if (!string.IsNullOrEmpty(promo.Details))
                {
                    var details = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(promo.Details);
                    Dictionary<string, string> settings = null;
                    if (details.ContainsKey(langId))
                    {
                        settings = details[langId];
                    }
                    else if (details.ContainsKey(defLang))
                    {
                        settings = details[defLang];
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
                        settings.TryGetValue("promotionLink", out var promotionLink);

                        promo.Title = title;
                        promo.Banner = bannerUrl;
                        promo.Description = description;
                        promo.PromotionLink = promotionLink;
                    }
                }

                promotion.Add(promo);
            }

            trans.Commit();

            return promotion;
        }

        public CustPromotion GetPromotionDetail(int SiteId, int CustId, int CurrencyId, bool IsLogin, string langId, string defLang, string BonusCode)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();

            var promo = conn.QueryFirstOrDefault<CustPromotion>("Member_PromotionV2_Get", new { CustId, SiteId, CurrencyId, BonusCode }, trans, commandType: CommandType.StoredProcedure);

            if (promo != null)
            {
                if (!string.IsNullOrEmpty(promo.Details))
                {
                    var details = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(promo.Details);
                    Dictionary<string, string> settings = null;
                    if (details.ContainsKey(langId))
                    {
                        settings = details[langId];
                    }
                    else if (details.ContainsKey(defLang))
                    {
                        settings = details[defLang];
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
                    canJoinParams.Add("RefId", dbType: DbType.String, direction: ParameterDirection.Output);
                    canJoinParams.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    canJoinParams.Add("RefList", dbType: DbType.String, direction: ParameterDirection.Output);
                    conn.Execute("Member_PromotionCanJoinV4_Get", canJoinParams, trans, commandType: CommandType.StoredProcedure);

                    var canJoinResult = canJoinParams.Get<byte?>("CanJoin") ?? 0;
                    promo.canJoin = canJoinResult == 1;
                    promo.Result = canJoinParams.Get<int>("Result");

                    if ((promo.PromotionType == (int)PromotionType.FstDeposit || promo.PromotionType == (int)PromotionType.ReDeposit) && promo.Result == 2)
                    {
                        promo.DepositId = canJoinParams.Get<string>("RefId");
                        promo.DepositAmount = canJoinParams.Get<decimal>("Amount");
                        promo.RefList = JsonConvert.DeserializeObject<IEnumerable<Ref>>(canJoinParams.Get<string>("RefList"));
                        
                        promo.MaxAmount = promo.PromotionType == (int) PromotionType.FstDeposit
                            ? walletService.GetBalance(CustId)
                            : promo.DepositAmount;
                        if (promo.RequiredAmount == 0)
                        {
                            promo.RequiredAmount = 1;
                        }

                        if (promo.BonusType == (int)BonusType.Amount)
                        {
                            promo.MaxAmount = promo.RequiredAmount;
                        }
                        else
                        {
                            if (promo.MaxBonus > 0)
                            {
                                var dBonus = Math.Ceiling(promo.Bonus * promo.DepositAmount / 100);
                                if (dBonus > promo.MaxBonus)
                                {
                                    promo.MaxAmount = promo.MaxBonus * 100 / promo.Bonus;
                                }
                            }
                        }
                    }

                    if (promo.PromotionType == (int)PromotionType.Rebate)
                    {
                        promo.isJoin = conn.Execute("Member_RebateInfo_GetByCust", new { promo.BonusCode, RebateType = promo.PromotionType - 5, CustId }, trans, commandType: CommandType.StoredProcedure) > 0;
                    }
                    else
                    {
                        var custBonusInfo = conn.QueryFirstOrDefault<BonusWallet>("Member_BonusInfo_GetByCust", new { promo.BonusCode, PromoCategoryId = promo.CategoryId, TpId = promo.TpId, CustId }, trans, commandType: CommandType.StoredProcedure);
                        if (custBonusInfo != null)
                        {
                            promo.CustExpiryTime = custBonusInfo.ExpiryTime;
                            promo.CustRollOver = custBonusInfo.Rollover;
                            promo.CustTurnOver = custBonusInfo.Turnover;
                            promo.Status = custBonusInfo.Status;
                            promo.isJoin = true;
                        }

                        if (promo.PromotionType == (int) PromotionType.Cashback)
                        {
                            GetCashbackInfo(CustId, SiteId, CurrencyId, promo, true);
                        }
                    }
                }
            }

            trans.Commit();

            return promo;
        }

        private void GetCashbackInfo(int custId, int siteId, int currencyId, CustPromotion promotion, bool getDetail)
        {
            var calculateDate = DateTime.UtcNow.AddHours(8)
                .AddDays(-(int) DateTime.Today.DayOfWeek + (int) DayOfWeek.Monday)
                .ToString("yyyy-MM-dd");

            var result =
                _reportDbClient.Query<CustCashBackCheckOutput>("Member_CustCashBackCheck_Get",
                    new DynamicParameters(new
                    {
                        CustId = custId, promotion.BonusCode, PromoCategoryId = promotion.CategoryId,
                        CalculateDate = calculateDate
                    })).ToList();

            promotion.isJoin = result.FirstOrDefault()?.Claimed ?? false;
            promotion.canJoin = result.Any();

            var currentDate = DateTime.UtcNow.AddHours(8).Date;
            var utcMonday = currentDate.AddDays(-(int) currentDate.DayOfWeek + (int) DayOfWeek.Monday).AddHours(-8);
            var utcSunday = utcMonday.AddDays(7).AddTicks(-1);
            promotion.PeriodFrom = ((DateTimeOffset) utcMonday).ToUnixTimeSeconds();
            promotion.PeriodTo = ((DateTimeOffset) utcSunday).ToUnixTimeSeconds();

            if (!getDetail) return;
            var lastMonday = currentDate.AddDays(-(int) currentDate.DayOfWeek - 6).Date;
            var lastSunday = lastMonday.AddDays(6).ToString("yyyy-MM-dd");
            var detail = _reportDbClient.Query<CashBackDetail>("Member_CashBackInfo_Get",
                new DynamicParameters(new
                {
                    CustId = custId, promotion.BonusCode, PeriodFrom = lastMonday.ToString("yyyy-MM-dd"),
                    PeriodTo = lastSunday, PromoCategoryId = promotion.CategoryId
                })).ToList();

            if (detail.Any() && detail.First().TpId == (int)Tp.Saba)
            {
                foreach (var d in detail)
                {
                    d.TpName = d.CategoryId switch
                    {
                        (int) TpCategoryId.Sports => _selLangBll.GetResourceValue("lbl_Cashback_Sports"),
                        (int) TpCategoryId.Casino => _selLangBll.GetResourceValue("lbl_Cashback_RNG"),
                        (int) TpCategoryId.Lottery => _selLangBll.GetResourceValue("lbl_Cashback_Lottery"),
                        (int) TpCategoryId.VirtualSports => _selLangBll.GetResourceValue("lbl_Cashback_Virtual_Sports"),
                        _ => d.TpName
                    };
                }
            }
            
            var details = _reportDbClient.Query<CashBackDetails>("Member_CustCashBack_Get",
                new DynamicParameters(new
                {
                    CustId = custId, promotion.BonusCode, PromoCategoryId = promotion.CategoryId,
                    CalculateDate = currentDate.AddDays(-(int) currentDate.DayOfWeek + (int) DayOfWeek.Monday)
                        .ToString("yyyy-MM-dd"),
                    CurrencyId = currencyId, SiteId = siteId
                })).ToList();

            if (!details.Any()) return;
            details.First().CashBackDetail = detail;
            promotion.CashBackDetails = details.First();
        }

        public List<CustPromotion> GetSpecialPromotionList(int SiteId, int CustId, int CurrencyId, string langId, string defLang)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            conn.Open();
            using var trans = conn.BeginTransaction();

            var promotionList = conn.Query<CustPromotion>("Member_SPCodePromotionList_Get", new { CustId }, trans, commandType: CommandType.StoredProcedure);

            List<CustPromotion> _promotion = new List<CustPromotion>();

            foreach (var promo in promotionList)
            {
                var canJoinParams = new DynamicParameters(new { CustId, CurrencyId, SiteId, BonusCode = promo.BonusCode });
                canJoinParams.Add("CanJoin", dbType: DbType.Boolean, direction: ParameterDirection.Output);
                canJoinParams.Add("Amount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
                canJoinParams.Add("RefId", dbType: DbType.String, direction: ParameterDirection.Output);
                canJoinParams.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
                canJoinParams.Add("RefList", dbType: DbType.String, direction: ParameterDirection.Output);
                conn.Execute("Member_PromotionCanJoinV4_Get", canJoinParams, commandType: CommandType.StoredProcedure);

                var canJoinResult = canJoinParams.Get<byte?>("CanJoin") ?? 0;
                promo.canJoin = canJoinResult == 1 || canJoinResult == 3;
                promo.Result = canJoinParams.Get<int>("Result");

                if (promo.PromotionType == (int)PromotionType.FstDeposit && promo.Result == 2)
                {
                    promo.DepositId = canJoinParams.Get<string>("RefId");
                    promo.DepositAmount = canJoinParams.Get<decimal>("Amount");
                    promo.MaxAmount = walletService.GetBalance(CustId);
                    if (promo.RequiredAmount == 0)
                    {
                        promo.RequiredAmount = 1;
                    }

                    if (promo.BonusType == (int)BonusType.Amount)
                    {
                        promo.MaxAmount = promo.RequiredAmount;
                    }
                    else
                    {
                        if (promo.MaxBonus > 0)
                        {
                            var dBonus = Math.Ceiling(promo.Bonus * promo.DepositAmount / 100);
                            if (dBonus > promo.MaxBonus)
                            {
                                promo.MaxAmount = promo.MaxBonus * 100 / promo.Bonus;
                            }
                        }
                    }
                }

                if (promo.PromotionType == (int)PromotionType.Rebate || promo.PromotionType == (int)PromotionType.Cashback)
                {
                    promo.isJoin = conn.Execute("Member_RebateInfo_GetByCust", new { promo.BonusCode, RebateType = promo.PromotionType - 5, CustId }, trans, commandType: CommandType.StoredProcedure) > 0;
                }
                else
                {
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
                    var details = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(promo.Details);
                    Dictionary<string, string> settings = null;
                    if (details.ContainsKey(langId))
                    {
                        settings = details[langId];
                    }
                    else if (details.ContainsKey(defLang))
                    {
                        settings = details[defLang];
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
        public bool SetSpecialCode(int custId, string specialCode)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            int result = conn.Execute("Member_CustSpecialCode_Ins", new { CustId = custId, SpecialCode = specialCode }, commandType: CommandType.StoredProcedure);
            return result > 0;
        }
        public void SetSpecialCodeJoined(int custId, string specialCode, string bonusCode)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Execute("Member_CustSpecialCodeJoin_Set", new { CustId = custId, SpecialCode = specialCode, BonusCode = bonusCode }, commandType: CommandType.StoredProcedure);
        }
    }
}
