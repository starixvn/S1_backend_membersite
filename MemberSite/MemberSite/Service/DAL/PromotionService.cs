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
