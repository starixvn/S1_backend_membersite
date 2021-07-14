using Dapper;
using MemberSite.Service.Model;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.Data;

namespace MemberSite.Service.DAL
{
    public class CustInfoService
    {
        private readonly IConfiguration configuration;
        private readonly PromotionService promotionService;

        public CustInfoService(IConfiguration configuration, PromotionService promotionService)
        {
            this.configuration = configuration;
            this.promotionService = promotionService;
        }
        public bool ExistsUserName(string userName, int siteId)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.ExecuteScalar<int>("Member_ExistsUserName_Get", new { UserName = userName, SiteId = siteId }, commandType: CommandType.StoredProcedure) != 0;
        }

        public bool ExistsPhoneNo(int custId, int siteId, string AreaCode, string PhoneNo)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.ExecuteScalar<int>("Member_ExistsPhoneNo_Get", new { CustId = custId, AreaCode, PhoneNo, SiteId = siteId }, commandType: CommandType.StoredProcedure) != 0;
        }

        public bool ExistsEmail(int custId, string email, int siteId)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.ExecuteScalar<int>("Member_ExistsEmail_Get", new { CustId = custId, Email = email, SiteId = siteId }, commandType: CommandType.StoredProcedure) != 0;
        }
        public int AddCustInfo(Customer customer, string Email, out int custId, string signupUrl = null, string PhoneNo = null, string AreaCode = null, string RealName = null, string promoCode = null, int Birth_d = 0, int Birth_m = 0, int Birth_y = 0)
        {
            custId = 0;
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            var p = new DynamicParameters(new { customer.SiteId, customer.UserName, customer.Userpwd, customer.CurrencyId, customer.UserLang, customer.AgentId, customer.AllAgentId, customer.UserLevel });
            p.Add("CustId", dbType: DbType.Int32, direction: ParameterDirection.Output);

            conn.Open();
            using MySqlTransaction trans = conn.BeginTransaction();
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
                if(Birth_d != 0 && Birth_m != 0 && Birth_y != 0)
                {
                    profileinfo.Add("Birth_d", Birth_d.ToString());
                    profileinfo.Add("Birth_m", Birth_m.ToString());
                    profileinfo.Add("Birth_y", Birth_y.ToString());

                }


                string profile1 = string.IsNullOrEmpty(RealName) ? null : Newtonsoft.Json.JsonConvert.SerializeObject(new Dictionary<string, string>() { { "AccountName", RealName } });
                string profile = profileinfo.Count <= 0 ? null : Newtonsoft.Json.JsonConvert.SerializeObject(profileinfo);
                result = conn.Execute("Member_CustProfile_Ins", new { CustId = custId, customer.SiteId, Email = Email, Profile = (string)profile, PhoneNo = PhoneNo, AreaCode = AreaCode }, trans, commandType: CommandType.StoredProcedure);


                if (!string.IsNullOrEmpty(RealName))
                    SetCustBankInfo(custId, customer.SiteId, RealName, null);
            }

            if (result > 0 && !string.IsNullOrEmpty(signupUrl))
            {
                //custId = p.Get<int?>("CustId") ?? 0;
                result = conn.Execute("Member_CustRegInfo_Ins", new { CustId = custId, SiteId = customer.SiteId, SignupUrl = signupUrl }, trans, commandType: CommandType.StoredProcedure);
            }

            if (result > 0 && !string.IsNullOrEmpty(promoCode)&& promotionService.CheckSpecialCode(promoCode))
            {

                if (promotionService.CheckScratchCardCode(promoCode))
                {
                    var Params = new DynamicParameters(new { CustId = custId, ScratchCardCode = promoCode, DeviceType = 1 });
                    Params.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    conn.Execute("Member_CustScratchCardCodeV3_Ins", Params, trans, commandType: CommandType.StoredProcedure);
                    result = Params.Get<int>("Result") == 0?1:0;
                }
                else
                    result = conn.Execute("Member_CustPromoCode_Ins", new { CustId = custId, PromoCode = promoCode.Trim() }, trans, commandType: CommandType.StoredProcedure);

                if (result == 0)
                {
                    result = -7;
                }
            }

            if (result > 0 && (int.TryParse(customer.Aff, out int iAff) || !string.IsNullOrEmpty(customer.AffCode)))
            {
                result = conn.Execute("Member_VNAffiliate_Ins", new { CustId = custId, customer.SiteId, AffId = iAff, AffCode = customer.AffCode }, trans, commandType: CommandType.StoredProcedure);
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
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.QueryFirstOrDefault<Customer>("Customer_Get", new { CustId = custId }, commandType: CommandType.StoredProcedure);
        }
        public CustTransSummary GetCustTransSummary(int custId)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.QueryFirstOrDefault<CustTransSummary>("Member_CustTransSummary_Get", new { CustId = custId }, commandType: CommandType.StoredProcedure);
        }

        public Customer GetCustInfoByMail(string email, int siteId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.QueryFirstOrDefault<Customer>("Member_CustomerByMail_Get", new { Email = email, SiteId = siteId }, commandType: CommandType.StoredProcedure);
        }

        public Customer GetCustInfoByUserName(string userName, int siteId)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.QueryFirstOrDefault<Customer>("LoginCustInfo_Get", new { UserName = userName, SiteId = siteId }, commandType: CommandType.StoredProcedure);
        }
        public bool SetLanguage(int custId, string langId)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Execute("LangId_Set", new { CustId = custId, LangId = langId }, commandType: CommandType.StoredProcedure) > 0;
        }
        public int GetPointLevel(int custId)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.ExecuteScalar<int>("Member_PointLevel_Get", new { CustId = custId }, commandType: CommandType.StoredProcedure);
        }
        public bool SetPassword(int custId, string md5OldPassword, string md5NewPassword, out string md5Userpwd)
        {
            int result = 0;
            md5Userpwd = null;
            using (MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
            {
                conn.Open();
                using (MySqlTransaction trans = conn.BeginTransaction())
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
            int result = 0;
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            string Email = profile[nameof(Email)];
            string AreaCode = profile[nameof(AreaCode)];
            string PhoneNo = profile[nameof(PhoneNo)];
            result = conn.Execute("Member_CustProfile_Set", new { CustId = custId, SiteId = siteId, AreaCode, Email = Email, PhoneNo, Profile = Newtonsoft.Json.JsonConvert.SerializeObject(profile) }, commandType: CommandType.StoredProcedure);
            return result > 0;
        }
        public bool SetProfileV2(int custId, int siteId, Dictionary<string, string> profile)
        {
            int result = 0;
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            string Email = profile[nameof(Email)];
            string AreaCode = profile[nameof(AreaCode)];
            string PhoneNo = profile[nameof(PhoneNo)];
            result = conn.Execute("Member_CustProfileV2_Set", new { CustId = custId, SiteId = siteId, AreaCode, Email = Email, PhoneNo, Profile = Newtonsoft.Json.JsonConvert.SerializeObject(profile) }, commandType: CommandType.StoredProcedure);
            return result > 0;
        }
        public bool SetCustBankInfo(int custId, int siteId, string accountName, IEnumerable<BankInfoDetail> bankInfo)
        {
            int result = 0;
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            string tmp = Newtonsoft.Json.JsonConvert.SerializeObject(bankInfo);
            result = conn.Execute("CustBankInfo_Set", new { CustId = custId, SiteId = siteId, AccountName = accountName, BankInfo = Newtonsoft.Json.JsonConvert.SerializeObject(bankInfo) }, commandType: CommandType.StoredProcedure);
            return result > 0;
        }
        public bool SetCustUSDTInfo(int custId, int siteId, string accountName, IEnumerable<USDTDetail> uSDTInfo)
        {
            int result = 0;
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            result = conn.Execute("Member_CustUSDTInfo_Set", new { CustId = custId, SiteId = siteId, AccountName = accountName, USDTInfo = Newtonsoft.Json.JsonConvert.SerializeObject(uSDTInfo) }, commandType: CommandType.StoredProcedure);
            return result > 0;
        }
        public CustBankInfo GetCustBankInfo(int custId, int siteId)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.QueryFirstOrDefault<CustBankInfo>("CustBankInfo_Get", new { CustId = custId, SiteId = siteId }, commandType: CommandType.StoredProcedure);
        }
        public string GetDefAccountNo(int custId, int siteId)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.QueryFirstOrDefault<string>("Member_DefAccountNo_Get", new { CustId = custId, SiteId = siteId }, commandType: CommandType.StoredProcedure);
        }
        public CustInfoProfile GetCustInfoProfile(int custId, int siteId)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.QueryFirstOrDefault<CustInfoProfile>("Member_CustProfileV2_Get", new { CustId = custId, SiteId = siteId }, commandType: CommandType.StoredProcedure);
        }
        public int SubmittedCustPhotoId(int custId, int siteId, int currencyId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.ExecuteScalar<int>("Member_SubmittedCustPhotoId_Get", new { CustId = custId, SiteId = siteId, CurrencyId = currencyId }, commandType: CommandType.StoredProcedure);
        }

        public int SetCustPhotoId(int custId, int siteId, string picFileName, byte[] photoIdPic)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Execute("Member_CustPhotoId_Int", new { CustId = custId, SiteId = siteId, PicFileName = picFileName, PhotoIdPic = photoIdPic }, commandType: CommandType.StoredProcedure);
        }
        public bool SetResetPwd(int custId, string md5NewPassword)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Execute("Member_ResetPwd_Set", new { CustId = custId, Md5NewPassword = md5NewPassword }, commandType: CommandType.StoredProcedure) > 0;
        }

        public int GetCustLevel(int CustId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.QueryFirstOrDefault<int>("VipLevel_Get", new { CustId }, commandType: CommandType.StoredProcedure);
        }
        public int SetPhoneVerified(int custId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            var result = conn.Execute("Common_PhoneVerified_Set", new { CustId = custId }, commandType: CommandType.StoredProcedure);
            return result;
        }
        public int SetFireBaseTroken(int custId, int siteId, string fireBaseToken)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Execute("Member_FireBaseToken_Set", new { CustId = custId, SiteId = siteId, FireBaseToken = fireBaseToken }, commandType: CommandType.StoredProcedure);
        }
    }
}
