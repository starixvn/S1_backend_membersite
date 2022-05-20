using Dapper;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using MemberSite.Service.Model.Line;
using MySql.Data.MySqlClient;
using System.Data;

namespace MemberSite.Service.DAL
{
    public class CustInfoService
    {
        private readonly IConfiguration configuration;
        private readonly PromotionService promotionService;
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;
        private readonly string _connectionString;

        public CustInfoService(IServiceProvider provider)
        {
            configuration = provider.GetRequiredService<IConfiguration>();
            promotionService = provider.GetRequiredService<PromotionService>();
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
            _connectionString = configuration.GetConnectionString("ConnectionString");
        }

        public bool ExistsUserName(string userName, int siteId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.ExecuteScalar<int>("Member_ExistsUserName_Get", new { UserName = userName, SiteId = siteId }, commandType: CommandType.StoredProcedure) != 0;
        }

        public bool ExistsPhoneNo(int custId, int siteId, string AreaCode, string PhoneNo)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.ExecuteScalar<int>("Member_ExistsPhoneNo_Get", new { CustId = custId, AreaCode, PhoneNo, SiteId = siteId }, commandType: CommandType.StoredProcedure) != 0;
        }

        public bool ExistsEmail(int custId, string email, int siteId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.ExecuteScalar<int>("Member_ExistsEmail_Get", new { CustId = custId, Email = email, SiteId = siteId }, commandType: CommandType.StoredProcedure) != 0;
        }
        public int AddCustInfo(Customer customer, string Email, string lineUserId, out int custId, string signupUrl = null, string referralUrl = null, string PhoneNo = null, string AreaCode = null, string RealName = null, string promoCode = null, int Birth_d = 0, int Birth_m = 0, int Birth_y = 0)
        {
            custId = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            var p = new DynamicParameters(new { customer.SiteId, customer.UserName, customer.Userpwd, customer.CurrencyId, customer.UserLang, customer.AgentId, customer.AllAgentId, customer.UserLevel });
            p.Add("CustId", dbType: DbType.Int32, direction: ParameterDirection.Output);

            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = conn.Execute("CustInfo_Ins", p, trans, commandType: CommandType.StoredProcedure);

            if (result > 0)
            {
                custId = p.Get<int?>("CustId") ?? 0;
            }

            if (result > 0 && (!string.IsNullOrEmpty(Email) || (!string.IsNullOrEmpty(PhoneNo) && !string.IsNullOrEmpty(AreaCode))))
            {
                if (string.IsNullOrEmpty(Email))
                {
                    Email = null;
                }
                else if (string.IsNullOrEmpty(PhoneNo) || string.IsNullOrEmpty(AreaCode))
                {
                    PhoneNo = null;
                    AreaCode = null;
                }
                var profileinfo = new Dictionary<string, string>();
                if (!string.IsNullOrEmpty(RealName))
                    profileinfo.Add("AccountName", RealName);
                if (Birth_d != 0 && Birth_m != 0 && Birth_y != 0)
                {
                    profileinfo.Add("Birth_d", Birth_d.ToString());
                    profileinfo.Add("Birth_m", Birth_m.ToString());
                    profileinfo.Add("Birth_y", Birth_y.ToString());

                }


                var profile1 = string.IsNullOrEmpty(RealName) ? null : Newtonsoft.Json.JsonConvert.SerializeObject(new Dictionary<string, string>() { { "AccountName", RealName } });
                var profile = profileinfo.Count <= 0 ? null : Newtonsoft.Json.JsonConvert.SerializeObject(profileinfo);
                result = conn.Execute("Member_CustProfile_Ins", new
                {
                    CustId = custId,
                    customer.SiteId,
                    Email,
                    Profile = (string)profile,
                    PhoneNo,
                    AreaCode
                }, trans, commandType: CommandType.StoredProcedure);


                if (!string.IsNullOrEmpty(RealName))
                    SetCustBankInfo(custId, customer.SiteId, RealName, null);
            }

            if (result > 0 && (!string.IsNullOrEmpty(signupUrl) || !string.IsNullOrEmpty(referralUrl)))
            {
                //custId = p.Get<int?>("CustId") ?? 0;
                result = conn.Execute("Member_CustRegInfo_Ins_V2", new { CustId = custId, customer.SiteId, SignupUrl = signupUrl, ReferralUrl = referralUrl }, trans, commandType: CommandType.StoredProcedure);
            }

            if (result > 0 && !string.IsNullOrEmpty(promoCode) && promotionService.CheckSpecialCode(promoCode))
            {

                if (promotionService.CheckScratchCardCode(promoCode))
                {
                    var Params = new DynamicParameters(new { CustId = custId, ScratchCardCode = promoCode, DeviceType = 1 });
                    Params.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    conn.Execute("Member_CustScratchCardCodeV3_Ins", Params, trans, commandType: CommandType.StoredProcedure);
                    result = Params.Get<int>("Result") == 0 ? 1 : 0;
                }
                else
                    result = conn.Execute("Member_CustPromoCode_Ins", new { CustId = custId, PromoCode = promoCode.Trim() }, trans, commandType: CommandType.StoredProcedure);

                if (result == 0)
                {
                    result = -7;
                }
            }

            if (result > 0 && (int.TryParse(customer.Aff, out var iAff) || !string.IsNullOrEmpty(customer.AffCode)))
            {
                result = conn.Execute("Member_VNAffiliate_Ins", new { CustId = custId, customer.SiteId, AffId = iAff, customer.AffCode }, trans, commandType: CommandType.StoredProcedure);
            }

            if (result > 0 && !string.IsNullOrWhiteSpace(lineUserId))
            {
                result = InsertCustLineProfile(custId, lineUserId, conn, trans) ? 1 : 0;
            }

            if (result > 0)
            {
                trans.Commit();
            }

            if (result > 0)
            {
                result = 0;
            }
            else if (result == 0)
            {
                result = -5;
            }

            return result;
        }
        public Customer GetCustInfo(int custId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.QueryFirstOrDefault<Customer>("Customer_Get", new { CustId = custId }, commandType: CommandType.StoredProcedure);
        }
        public CustTransSummary GetCustTransSummary(int custId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.QueryFirstOrDefault<CustTransSummary>("Member_CustTransSummary_Get", new { CustId = custId }, commandType: CommandType.StoredProcedure);
        }

        public Customer GetCustInfoByMail(string email, int siteId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.QueryFirstOrDefault<Customer>("Member_CustomerByMail_Get", new { Email = email, SiteId = siteId }, commandType: CommandType.StoredProcedure);
        }

        public Customer GetCustInfoByUserName(string userName, int siteId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.QueryFirstOrDefault<Customer>("LoginCustInfo_Get", new { UserName = userName, SiteId = siteId }, commandType: CommandType.StoredProcedure);
        }
        public bool SetLanguage(int custId, string langId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Execute("LangId_Set", new { CustId = custId, LangId = langId }, commandType: CommandType.StoredProcedure) > 0;
        }
        public int GetPointLevel(int custId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.ExecuteScalar<int>("Member_PointLevel_Get", new { CustId = custId }, commandType: CommandType.StoredProcedure);
        }
        public bool SetPassword(int custId, string md5OldPassword, string md5NewPassword, out string md5Userpwd)
        {
            var result = 0;
            md5Userpwd = null;
            using (var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
            {
                conn.Open();
                using (var trans = conn.BeginTransaction())
                {
                    var p = new DynamicParameters(new { CustId = custId, Md5OldPassword = md5OldPassword, Md5NewPassword = md5NewPassword });
                    p.Add("Md5Userpwd", dbType: DbType.String, direction: ParameterDirection.Output);

                    result = conn.Execute("Password_Set", p, trans, commandType: CommandType.StoredProcedure);
                    md5Userpwd = p.Get<string>("Md5Userpwd");
                    trans.Commit();
                    return result > 0;
                }
            }
        }
        public bool SetProfile(int custId, int siteId, Dictionary<string, string> profile)
        {
            var result = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            string Email = profile[nameof(Email)];
            string AreaCode = profile[nameof(AreaCode)];
            string PhoneNo = profile[nameof(PhoneNo)];
            result = conn.Execute("Member_CustProfile_Set", new { CustId = custId, SiteId = siteId, AreaCode, Email, PhoneNo, Profile = Newtonsoft.Json.JsonConvert.SerializeObject(profile) }, commandType: CommandType.StoredProcedure);
            return result > 0;
        }
        public bool SetProfileV2(int custId, int siteId, Dictionary<string, string> profile)
        {
            var result = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            string Email = profile[nameof(Email)];
            string AreaCode = profile[nameof(AreaCode)];
            string PhoneNo = profile[nameof(PhoneNo)];
            result = conn.Execute("Member_CustProfileV2_Set", new { CustId = custId, SiteId = siteId, AreaCode, Email, PhoneNo, Profile = Newtonsoft.Json.JsonConvert.SerializeObject(profile) }, commandType: CommandType.StoredProcedure);
            return result > 0;
        }
        public bool SetCustBankInfo(int custId, int siteId, string accountName, IEnumerable<BankInfoDetail> bankInfo)
        {
            var result = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            var tmp = Newtonsoft.Json.JsonConvert.SerializeObject(bankInfo);
            result = conn.Execute("Service_CustBankInfo_SetV2", new { CustId = custId, SiteId = siteId, AccountName = accountName, BankInfo = Newtonsoft.Json.JsonConvert.SerializeObject(bankInfo) }, commandType: CommandType.StoredProcedure);
            return result > 0;
        }

        public bool SetCustUSDTInfo(int custId, int siteId, string accountName, IEnumerable<USDTDetail> uSDTInfo)
        {
            var result = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            var tmp = Newtonsoft.Json.JsonConvert.SerializeObject(uSDTInfo);
            result = conn.Execute("Service_CustUSDTInfo_Set", new { CustId = custId, SiteId = siteId, AccountName = accountName, USDTInfo = Newtonsoft.Json.JsonConvert.SerializeObject(uSDTInfo) }, commandType: CommandType.StoredProcedure);
            return result > 0;
        }

        public bool InsertCustBankInfo(int custId, int siteId, string accountName, IEnumerable<BankInfoDetail> bankInfo, IEnumerable<BankInfoDetail> banklog)
        {
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();
            using var trans = conn.BeginTransaction();
            var bankinfoResult = SetCustBankInfo(custId, siteId, accountName, bankInfo, conn, trans);
            var banklogResult = SetCustBankInfoLog(custId, siteId, accountName, banklog, conn, trans);
            if (bankinfoResult & banklogResult)
            {
                trans.Commit();
            }
            return bankinfoResult & banklogResult;
        }

        private bool SetCustBankInfo(int custId, int siteId, string accountName, IEnumerable<BankInfoDetail> bankInfo, MySqlConnection conn, MySqlTransaction trans)
        {
            var result = 0;
            var tmp = Newtonsoft.Json.JsonConvert.SerializeObject(bankInfo);
            var parameter = new DynamicParameters(new
            {
                CustId = custId,
                SiteId = siteId,
                AccountName = accountName,
                BankInfo = Newtonsoft.Json.JsonConvert.SerializeObject(bankInfo)
            });
            result = _mainDbClient.Execute("Service_CustBankInfo_SetV2", parameter, conn, trans);
            return result > 0;
        }
        private bool SetCustBankInfoLog(int custId, int siteId, string accountName, IEnumerable<BankInfoDetail> bankInfo, MySqlConnection conn, MySqlTransaction trans)
        {
            var result = 0;
            var tmp = Newtonsoft.Json.JsonConvert.SerializeObject(bankInfo);
            var parameter = new DynamicParameters(new
            {
                CustId = custId,
                SiteId = siteId,
                AccountName = accountName,
                BankInfo = Newtonsoft.Json.JsonConvert.SerializeObject(bankInfo)
            });
            result = _mainDbClient.Execute("Service_CustBankInfoLog_Set", parameter, conn, trans);
            return result > 0;
        }
        public bool InsertCustUSDTInfo(int custId, int siteId, string accountName, IEnumerable<USDTDetail> uSDTInfo, IEnumerable<USDTDetail> uSDTInfoLog)
        {
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();
            using var trans = conn.BeginTransaction();
            var custUsdtInfoResult = SetCustUSDTInfo(custId, siteId, accountName, uSDTInfo, conn, trans);
            var custUsdtInfologResult = SetCustUSDTInfoLog(custId, siteId, accountName, uSDTInfoLog, conn, trans);
            if (custUsdtInfoResult & custUsdtInfologResult)
            {
                trans.Commit();
            }
            return custUsdtInfoResult & custUsdtInfologResult;
        }
        private bool SetCustUSDTInfo(int custId, int siteId, string accountName, IEnumerable<USDTDetail> uSDTInfo, MySqlConnection conn, MySqlTransaction trans)
        {
            var result = 0;
            var parameter = new DynamicParameters(new
            {
                CustId = custId,
                SiteId = siteId,
                AccountName = accountName,
                USDTInfo = Newtonsoft.Json.JsonConvert.SerializeObject(uSDTInfo)
            });
            result = _mainDbClient.Execute("Service_CustUSDTInfo_Set", parameter, conn, trans);
            return result > 0;
        }
        private bool SetCustUSDTInfoLog(int custId, int siteId, string accountName, IEnumerable<USDTDetail> uSDTInfo, MySqlConnection conn, MySqlTransaction trans)
        {
            var result = 0;
            var parameter = new DynamicParameters(new
            {
                CustId = custId,
                SiteId = siteId,
                AccountName = accountName,
                USDTInfo = Newtonsoft.Json.JsonConvert.SerializeObject(uSDTInfo)
            });
            result = _mainDbClient.Execute("Service_CustUSDTInfoLog_Set", parameter, conn, trans);
            return result > 0;
        }

        public CustBankInfo GetCustBankInfo(int custId, int siteId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.QueryFirstOrDefault<CustBankInfo>("CustBankInfo_Get", new { CustId = custId, SiteId = siteId }, commandType: CommandType.StoredProcedure);
        }
        public string GetDefAccountNo(int custId, int siteId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.QueryFirstOrDefault<string>("Member_DefAccountNo_Get", new { CustId = custId, SiteId = siteId }, commandType: CommandType.StoredProcedure);
        }
        public CustInfoProfile GetCustInfoProfile(int custId, int siteId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.QueryFirstOrDefault<CustInfoProfile>("Member_CustProfileV2_Get", new { CustId = custId, SiteId = siteId }, commandType: CommandType.StoredProcedure);
        }
        public int SubmittedCustPhotoId(int custId, int siteId, int currencyId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.ExecuteScalar<int>("Member_SubmittedCustPhotoId_GetV2", new { CustId = custId, SiteId = siteId, CurrencyId = currencyId }, commandType: CommandType.StoredProcedure);
        }

        public int SetCustPhotoId(int custId, int siteId, string picFileName, byte[] photoIdPic)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Execute("Member_CustPhotoId_Set", new { CustId = custId, SiteId = siteId, PicFileName = picFileName, PhotoIdPic = photoIdPic }, commandType: CommandType.StoredProcedure);
        }
        public bool SetResetPwd(int custId, string md5NewPassword)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Execute("Member_ResetPwd_Set", new { CustId = custId, Md5NewPassword = md5NewPassword }, commandType: CommandType.StoredProcedure) > 0;
        }

        public int SetPhoneVerified(int custId)
        {
            var result = _mainDbClient.Execute("Common_PhoneVerified_Set", new DynamicParameters(new { CustId = custId }));
            return result;
        }

        public async Task<bool> GetPhoneVerified(int custId)
        {
            var result = await _mainDbClient.QueryFirstAsync<bool>("Member_PhoneVerified_Get", new DynamicParameters(new { _CustId = custId }));
            return result;
        }

        public int SetEmailVerified(int custId)
        {
            var result = _mainDbClient.Execute("Common_EmailVerified_Set", new DynamicParameters(new { _CustId = custId }));
            return result;
        }

        public async Task<bool> GetEmailVerified(int custId)
        {
            var result = await _mainDbClient.QueryFirstAsync<bool>("Member_EmailVerified_Get", new DynamicParameters(new { _CustId = custId }));
            return result;
        }

        public int SetFireBaseTroken(int custId, int siteId, string fireBaseToken)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Execute("Member_FireBaseToken_Set", new { CustId = custId, SiteId = siteId, FireBaseToken = fireBaseToken }, commandType: CommandType.StoredProcedure);
        }
        public int SetCustActStatus(int custId, string Operator)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = conn.Execute("Member_CustActStatus_Set", new { CustId = custId, Operator = Operator }, transaction: trans, commandType: CommandType.StoredProcedure);
            trans.Commit();
            return result;
        }

        public bool InsertCustLineProfile(int custId, string lineUserId,
            MySqlConnection conn = null, MySqlTransaction trans = null)
        {
            var sp = "Member_CustLineProfile_Ins";
            var param = new DynamicParameters(new { _CustId = custId, _userId = lineUserId });
            var result = 0;

            if (conn == null)
            {
                //No execute in transaction
                result = _mainDbClient.Execute(sp, param);
            }
            else
            {
                //Execute in transaction
                result = conn.Execute(sp, param, commandType: CommandType.StoredProcedure);
            }
            return (result == 0) ? false : true;
        }

        public async Task<CustLineProfileModel> GetCustLineProfile(int custId, string lineUserId)
        {
            return await _reportDbClient.QueryFirstAsync<CustLineProfileModel>("Member_CustLineProfile_Get", new DynamicParameters(new
            {
                _CustId = custId,
                _userId = lineUserId
            }));
        }
    }
}
