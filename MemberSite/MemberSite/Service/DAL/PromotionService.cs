using System.Data;
using System.Text.RegularExpressions;
using Dapper;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using MemberSite.Service.BLL;

namespace MemberSite.Service.DAL
{
    public class PromotionService
    {
        private readonly IConfiguration _configuration;
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;
        private readonly WalletService _walletService;
        private readonly SelLangBLL _selLangBll;

        public PromotionService(IServiceProvider serviceProvider)
        {
            _configuration = serviceProvider.GetRequiredService<IConfiguration>();
            _mainDbClient = serviceProvider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDbClient = serviceProvider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
            _walletService = serviceProvider.GetRequiredService<WalletService>();
            _selLangBll = serviceProvider.GetRequiredService<SelLangBLL>();
        }

        public IEnumerable<Promotion> GetPromotionList(int siteId)
        {
            using var conn = new MySqlConnection(_configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<Promotion>("Member_PromotionList_Get", new { SiteId = siteId }, commandType: CommandType.StoredProcedure);
        }
        public IEnumerable<Promotion> GetPromotionListByCustId(int siteId, int custId, int currencyId)
        {
            using var conn = new MySqlConnection(_configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<Promotion>("Member_PromotionListByCustId_Get", new { SiteId = siteId, CustId = custId, CurrencyId = currencyId }, commandType: CommandType.StoredProcedure);
        }

        public List<CustPromotion> GetPromotionListv3(int SiteId, int CustId, int CurrencyId, int DeviceType, bool IsLogin, string langId, string defLang)
        {
            var promotionList = _reportDbClient.Query<CustPromotion>("Member_PromotionList_Get_V6", new DynamicParameters(new {CustId, SiteId, CurrencyId, DeviceType}));
            var promotion = new List<CustPromotion>();

            foreach (var promo in promotionList)
            {
                if (IsLogin)
                {
                    var canJoinParams = GetPromotionCanJoin(SiteId, CustId, CurrencyId, DeviceType, promo);
                    SetPromotion(SiteId, CustId, CurrencyId, false, promo, canJoinParams);
                }

                SetPromotionDetail(langId, defLang, true, promo);

                promotion.Add(promo);
            }

            return promotion;
        }

        public CustPromotion GetPromotionDetail(int SiteId, int CustId, int CurrencyId, bool IsLogin, string langId, string defLang, string BonusCode, int DeviceType)
        {
            var promo = _reportDbClient.QueryFirst<CustPromotion>("Member_PromotionV3_Get", new DynamicParameters(new {CustId, SiteId, CurrencyId, BonusCode, DeviceType}));

            if (promo != null)
            {
                SetPromotionDetail(langId, defLang, false, promo);

                if (IsLogin)
                {
                    var canJoinParams = GetPromotionCanJoin(SiteId, CustId, CurrencyId, DeviceType, promo);
                    SetPromotion(SiteId, CustId, CurrencyId, true, promo, canJoinParams);
                }
            }

            return promo;
        }
        
        private static DateTime StartOfWeek(DateTime dateTime, DayOfWeek startOfWeek)
        {
            var diff = (7 + (dateTime.DayOfWeek - startOfWeek)) % 7;
            return dateTime.AddDays(-1 * diff).Date;
        }

        private void GetCashbackInfo(int custId, int siteId, int currencyId, CustPromotion promotion, bool getDetail)
        {
            var offset = 8;
            if (siteId == (int)SiteId.Goal123 || siteId == (int)SiteId.Bwing)
            {
                offset = 7;
            }
            var currentTime = DateTime.UtcNow.AddHours(offset);
            var calculateDate = StartOfWeek(currentTime, DayOfWeek.Monday)
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

            var utcMonday = StartOfWeek(currentTime, DayOfWeek.Monday).AddHours(-offset);
            var utcSunday = utcMonday.AddDays(7).AddTicks(-1);
            promotion.PeriodFrom = ((DateTimeOffset) utcMonday).ToUnixTimeSeconds();
            promotion.PeriodTo = ((DateTimeOffset) utcSunday).ToUnixTimeSeconds();

            if (!getDetail) return;
            var lastMonday = StartOfWeek(currentTime.AddDays(-7), DayOfWeek.Monday).Date;
            var lastSunday = lastMonday.AddDays(6);
            var detail = _reportDbClient.Query<CashBackDetail>("Member_CashBackInfo_Get",
                new DynamicParameters(new
                {
                    CustId = custId, promotion.BonusCode, PeriodFrom = lastMonday.ToString("yyyy-MM-dd"),
                    PeriodTo = lastSunday.ToString("yyyy-MM-dd"), PromoCategoryId = promotion.CategoryId
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
                    CalculateDate = calculateDate,
                    CurrencyId = currencyId, SiteId = siteId
                })).ToList();

            if (!details.Any()) return;
            details.First().CashBackDetail = detail;
            promotion.CashBackDetails = details.First();
        }

        public List<CustPromotion> GetSpecialPromotionListV2(int SiteId, int CustId, int CurrencyId, string langId, string defLang, int deviceType)
        {
            var promotionList = _reportDbClient.Query<CustPromotion>("Member_SPCodePromotionList_Get_V4", new DynamicParameters(new { CustId, DeviceType = deviceType }));

            var _promotion = new List<CustPromotion>();

            foreach (var promo in promotionList)
            {
                var canJoinParams = GetPromotionCanJoin(SiteId, CustId, CurrencyId, deviceType, promo);
                SetPromotion(SiteId, CustId, CurrencyId, false, promo, canJoinParams);
                SetPromotionDetail(langId, defLang, true, promo);

                _promotion.Add(promo);
            }

            return _promotion;
        }
        
        private DynamicParameters GetPromotionCanJoin(int siteId, int custId, int currencyId, int deviceType,
            CustPromotion promo)
        {
            var canJoinParams = new DynamicParameters(new { CustId = custId, CurrencyId = currencyId, SiteId = siteId, promo.BonusCode,
                DeviceType = deviceType });
            canJoinParams.Add("CanJoin", dbType: DbType.Boolean, direction: ParameterDirection.Output);
            canJoinParams.Add("Amount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            canJoinParams.Add("RefId", dbType: DbType.String, direction: ParameterDirection.Output);
            canJoinParams.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            canJoinParams.Add("RefList", dbType: DbType.String, direction: ParameterDirection.Output);
            _mainDbClient.Execute("Member_PromotionCanJoin_Get_V7", canJoinParams);

            var canJoinResult = canJoinParams.Get<byte?>("CanJoin") ?? 0;
            promo.canJoin = canJoinResult == 1;
            promo.Result = canJoinParams.Get<int>("Result");
            return canJoinParams;
        }
        
        private void SetPromotion(int siteId, int custId, int currencyId, bool getCashbackDetail, CustPromotion promo, DynamicParameters canJoinParams)
        {
            if (promo.PromotionType is (int)PromotionType.FstDeposit or (int)PromotionType.ReDeposit
                && promo.Result == 2)
            {
                promo.DepositId = canJoinParams.Get<string>("RefId");
                promo.DepositAmount = canJoinParams.Get<decimal>("Amount");
                promo.RefList = JsonConvert.DeserializeObject<IEnumerable<Ref>>(canJoinParams.Get<string>("RefList"));

                promo.MaxAmount = promo.PromotionType == (int)PromotionType.FstDeposit
                    ? _walletService.GetBalance(custId)
                    : promo.DepositAmount;
                
                if (promo.PromotionType == (int)PromotionType.FstDeposit
                    && promo.MaxAmount > promo.DepositAmount)
                {
                    promo.MaxAmount = promo.DepositAmount;
                }

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
                promo.isJoin = _reportDbClient.Execute("Member_RebateInfo_GetByCust",
                    new DynamicParameters(new { promo.BonusCode, RebateType = promo.PromotionType - 5, CustId = custId })) > 0;

            }
            else
            {
                var custBonusInfo = _reportDbClient.QueryFirst<BonusWallet>("Member_BonusInfo_GetByCust",
                    new DynamicParameters(new { promo.BonusCode, PromoCategoryId = promo.CategoryId, promo.TpId, CustId = custId }));
                if (custBonusInfo != null)
                {
                    promo.CustExpiryTime = custBonusInfo.ExpiryTime;
                    promo.CustRollOver = custBonusInfo.Rollover;
                    promo.CustTurnOver = custBonusInfo.Turnover;
                    promo.Status = custBonusInfo.Status;
                    promo.isJoin = true;
                }
                
                if (promo.PromotionType == (int)PromotionType.Cashback)
                {
                    GetCashbackInfo(custId, siteId, currencyId, promo, getCashbackDetail);
                }
            }
        }
        
        private void SetPromotionDetail(string langId, string defLang, bool clearDetails, CustPromotion promo)
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

                if (clearDetails)
                {
                    promo.Details = null;
                }

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

                    if (!clearDetails)
                    {
                        settings.TryGetValue("detail", out var detail);
                        promo.Details = detail;
                    }
                }
            }
        }

        
        public bool SetSpecialCode(int custId, string specialCode, int? deviceType)
        {
            using var conn = new MySqlConnection(_configuration.GetConnectionString("ConnectionString"));
            var result = conn.Execute("Member_CustSpecialCodeV3_Ins", new { CustId = custId, SpecialCode = specialCode, DeviceType = deviceType }, commandType: CommandType.StoredProcedure);
            return result > 0;
        }
        
        public int SetScratchCardCode(int custId, string scratchCardCode, int? deviceType)
        {
            using var conn = new MySqlConnection(_configuration.GetConnectionString("ConnectionString"));
            var Params = new DynamicParameters(new { CustId = custId, ScratchCardCode = scratchCardCode, DeviceType = deviceType });
            Params.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            conn.Execute("Member_CustScratchCardCodeV3_Ins", Params, commandType: CommandType.StoredProcedure);
            var Result = Params.Get<int>("Result");
            return Result;
        }

        public List<CustPromotion> GetDepositPromotionList(int CustId, int DeviceType, string langId, string defLang)
        {
            using var conn = new MySqlConnection(_configuration.GetConnectionString("ConnectionStringReport"));
            conn.Open();

            var promotionList = conn.Query<CustPromotion>("Member_DepositPromotion_Get_V4", new { CustId, DeviceType }, commandType: CommandType.StoredProcedure);
            var _promotion = new List<CustPromotion>();

            foreach (var promo in promotionList)
            { 
                SetPromotionDetail(langId, defLang, true, promo);
                _promotion.Add(promo);
            }


            return _promotion;
        }
        public bool SetDepositPromotionQueue(string transId, string bonusCode, int custId, int siteId, string userName, int currencyId, int deviceType, decimal lockedAmount, string custIp)
        {
            using var conn = new MySqlConnection(_configuration.GetConnectionString("ConnectionString"));
            return conn.Execute("Member_DepositBonusQueue_Set_V2",
                new
                {
                    TransId = transId, BonusCode = bonusCode, CustId = custId, SiteId = siteId, UserName = userName,
                    CurrencyId = currencyId, DeviceType = deviceType, LockedAmount = lockedAmount, IP = custIp
                }, commandType: CommandType.StoredProcedure) > 0;
        }
        public bool CheckSpecialCode(string specialCode)
        {
            if (string.IsNullOrEmpty(specialCode) || specialCode.Length > 20)
                return false;
            var regex = new Regex("^[- !@#$%^&*a-zA-Z0-9]{1,20}$", RegexOptions.Singleline);
            var mPwd = regex.Match(specialCode);

            return mPwd.Success;
        }

        public bool CheckScratchCardCode(string specialCode)
        {
            if (string.IsNullOrEmpty(specialCode) || specialCode.Length != 12)
                return false;

            var regex = new Regex("^[a-zA-Z]{5}-[a-zA-Z0-9]{6}$", RegexOptions.Singleline);
            var mPwd = regex.Match(specialCode);

            return mPwd.Success;
        }

        public PromotionBySpecialCode GetPromotionBySpecialCode(int siteid,string specialCode)
        {
            return _reportDbClient.QueryFirst<PromotionBySpecialCode>("Member_PromotionBySpecialCode_Get", new DynamicParameters(new { SpecialCode = specialCode, SiteId = siteid }));
        }
        public PromotionBySpecialScratchCard GetPromotionBySpecialScratchCard(int siteid, string specialCode)
        {
            return _reportDbClient.QueryFirst<PromotionBySpecialScratchCard>("Member_PromotionBySpecialScratchCard_Get", new DynamicParameters(new { ScratchCardCode = specialCode, SiteId = siteid }));
        }
        public BonusinfoByBonusCode GetBonusinfoByBonusCode(int CustId, string BonusCode)
        {
            return _reportDbClient.QueryFirst<BonusinfoByBonusCode>("Member_BonusinfoByBonusCode_Get", new DynamicParameters(new { CustId = CustId, BonusCode = BonusCode }));
        }
    }
}
