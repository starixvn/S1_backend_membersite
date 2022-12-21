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
        private readonly ILogger<CustInfoService> _logger;
        private readonly string _connectionString;

        public CustInfoService(IServiceProvider provider)
        {
            configuration = provider.GetRequiredService<IConfiguration>();
            promotionService = provider.GetRequiredService<PromotionService>();
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
            _connectionString = configuration.GetConnectionString("ConnectionString");
            _logger = provider.GetRequiredService<ILogger<CustInfoService>>();
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
            var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            try
            {
                custId = 0;
                var p = new DynamicParameters(new
                {
                    customer.SiteId, customer.UserName, customer.Userpwd, customer.CurrencyId, customer.UserLang,
                    customer.AgentId, customer.AllAgentId, customer.UserLevel
                });
                p.Add("CustId", dbType: DbType.String, direction: ParameterDirection.Output);

                conn.Open();
                using var trans = conn.BeginTransaction();
                var result = _mainDbClient.Execute("CustInfo_Ins", p, conn, trans);

                if (result > 0)
                {
                    string strCustId;
                    strCustId = p.Get<string>("CustId") ?? "";
                    custId = int.Parse(strCustId);

                    if (custId == 0)
                    {
                        trans.Rollback();
                        conn.Close();
                        return -5;
                    }
                }
                else
                {
                    trans.Rollback();
                    conn.Close();
                    return -5;
                }

                if (result > 0 && (!string.IsNullOrEmpty(Email) ||
                                   (!string.IsNullOrEmpty(PhoneNo) && !string.IsNullOrEmpty(AreaCode))))
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

                    var profiler = new Dictionary<string, string>();

                    if (!string.IsNullOrEmpty(RealName)) profiler.Add("AccountName", RealName);
                    if (Birth_d != 0 && Birth_m != 0 && Birth_y != 0)
                    {
                        profiler.Add("Birth_d", Birth_d.ToString());
                        profiler.Add("Birth_m", Birth_m.ToString());
                        profiler.Add("Birth_y", Birth_y.ToString());
                    }

                    var profile = profiler.Count <= 0 ? null : Newtonsoft.Json.JsonConvert.SerializeObject(profiler);
                    result = _mainDbClient.Execute("Member_CustProfile_Ins", new DynamicParameters(new
                    {
                        CustId = custId,
                        customer.SiteId,
                        Email,
                        Profile = profile,
                        PhoneNo,
                        AreaCode
                    }), conn, trans);

                    if (!string.IsNullOrEmpty(RealName))
                    {
                        SetCustBankInfo(custId, customer.SiteId, RealName);
                    }

                }

                if (result > 0 && (!string.IsNullOrEmpty(signupUrl) || !string.IsNullOrEmpty(referralUrl)))
                {
                    result = _mainDbClient.Execute("Member_CustRegInfo_Ins_V2",
                        new DynamicParameters(new
                            { CustId = custId, customer.SiteId, SignupUrl = signupUrl, ReferralUrl = referralUrl }),
                        conn, trans);
                }

                if (result > 0 && !string.IsNullOrEmpty(promoCode) && promotionService.CheckSpecialCode(promoCode))
                {
                    if (promotionService.CheckScratchCardCode(promoCode))
                    {
                        var parameters = new DynamicParameters(new
                            { CustId = custId, ScratchCardCode = promoCode, DeviceType = 1 });
                        parameters.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
                        _mainDbClient.Execute("Member_CustScratchCardCodeV3_Ins", parameters, conn, trans);
                        result = parameters.Get<int>("Result") == 0 ? 1 : 0;
                    }
                    else
                    {
                        result = _mainDbClient.Execute("Member_CustPromoCode_Ins",
                            new DynamicParameters(new { CustId = custId, PromoCode = promoCode.Trim() }), conn, trans);
                    }

                    if (result == 0)
                    {
                        result = -7;
                    }
                }

                if (result > 0 && (int.TryParse(customer.Aff, out var iAff) || !string.IsNullOrEmpty(customer.AffCode)))
                {
                    result = _mainDbClient.Execute("Member_VNAffiliate_Ins",
                        new DynamicParameters(new { CustId = custId, customer.SiteId, AffId = iAff, customer.AffCode }),
                        conn, trans);
                }

                if (result > 0 && !string.IsNullOrWhiteSpace(lineUserId))
                {
                    if (GetCustLineProfile(0, lineUserId) != null)
                    {
                        result = -3002;
                    }
                    else
                    {
                        result = InsertCustLineProfile(custId, lineUserId, conn, trans) ? 1 : 0;
                    }
                }

                if (result > 0)
                {
                    trans.Commit();
                }
                else
                {
                    trans.Rollback();
                }
                conn.Close();
                conn.Dispose();

                result = result switch
                {
                    > 0 => 0,
                    0 => -5,
                    _ => result
                };

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{ex.Message}");
                conn.Close();
                conn.Dispose();
                throw;
            }
        }
        public Customer GetCustInfo(int custId, bool forceUseMainDb = false)
        {
            var spName = "Customer_Get";
            var parameter = new { CustId = custId };

            return forceUseMainDb
                ? _mainDbClient.QueryFirst<Customer>(spName, parameter)
                : _reportDbClient.QueryFirst<Customer>(spName, parameter);
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
            return _reportDbClient.ExecuteScalar<int>("Member_PointLevel_Get", new { CustId = custId });
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
            result = conn.Execute("Member_CustProfile_Set",
                new
                {
                    CustId = custId,
                    SiteId = siteId,
                    AreaCode,
                    Email,
                    PhoneNo,
                    Profile = Newtonsoft.Json.JsonConvert.SerializeObject(profile)
                }, commandType: CommandType.StoredProcedure);
            return result > 0;
        }
        public bool SetProfileV2(int custId, int siteId, Dictionary<string, string> profile)
        {
            var result = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            string Email = profile[nameof(Email)];
            string AreaCode = profile[nameof(AreaCode)];
            string PhoneNo = profile[nameof(PhoneNo)];
            result = conn.Execute("Member_CustProfileV2_Set",
                new
                {
                    CustId = custId,
                    SiteId = siteId,
                    AreaCode,
                    Email,
                    PhoneNo,
                    Profile = Newtonsoft.Json.JsonConvert.SerializeObject(profile)
                }, commandType: CommandType.StoredProcedure);
            return result > 0;
        }
        public bool SetAddress(int custId, int siteId, Dictionary<string, string> address)
        {
            var p = new DynamicParameters(new
            {
                CustId = custId,
                SiteId = siteId,
                Address = Newtonsoft.Json.JsonConvert.SerializeObject(address)
            });
            return _mainDbClient.Execute("Member_CustProfile_Address_Set", p) > 0;
        }
        public bool SetCustBankInfo(int custId, int siteId, string accountName)
        {
            var result = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            result = conn.Execute("Service_CustBankInfo_SetV3", new { CustId = custId, SiteId = siteId, AccountName = accountName }, commandType: CommandType.StoredProcedure);
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

        public bool InsertCustBankInfo(int custId, int siteId, string accountName, int sysId, string accountNum, string iFSCCode, CustBankInfoLogStatus status)
        {
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();
            using var trans = conn.BeginTransaction();
            var custBankInfoResult = true;
            var custBankInfo = GetCustBankInfo(custId);
            if (custBankInfo == null && !string.IsNullOrEmpty(accountName))
            {
                custBankInfoResult = SetCustBankInfo(custId, siteId, accountName, conn, trans);
            }
            var bankDetailResult = SetCustBankDetail(custId, sysId, accountNum, iFSCCode, CustBankDetailStatus.Able, conn, trans);
            var banklogResult = SetCustBankInfoLog(custId, siteId, accountName, sysId, accountNum, iFSCCode, status, conn, trans);
            if (bankDetailResult & banklogResult & custBankInfoResult)
            {
                trans.Commit();
            }
            return bankDetailResult & banklogResult & custBankInfoResult;
        }

        private bool SetCustBankInfoLog(int custId, int siteId, string accountName, int sysId, string accountNum, string iFSCCode, CustBankInfoLogStatus status, MySqlConnection conn, MySqlTransaction trans)
        {
            var result = 0;
            var parameter = new DynamicParameters(new
            {
                CustId = custId,
                SiteId = siteId,
                AccountName = accountName,
                SysId = sysId,
                AccountNum = accountNum,
                IFSCCode = string.IsNullOrWhiteSpace(iFSCCode) ? "" : iFSCCode,
                Status = (int)status
            });
            result = _mainDbClient.Execute("Service_CustBankInfoLog_Set_V2", parameter, conn, trans);
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

        public CustBankInfo GetCustBankInfo(int custId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.QueryFirstOrDefault<CustBankInfo>("Member_CustBankInfo_Get", new { CustId = custId }, commandType: CommandType.StoredProcedure);
        }
        public string GetDefAccountNo(int custId, int siteId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.QueryFirstOrDefault<string>("Member_DefAccountNo_Get", new { CustId = custId, SiteId = siteId }, commandType: CommandType.StoredProcedure);
        }
        public CustInfoProfile GetCustInfoProfile(int custId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.QueryFirstOrDefault<CustInfoProfile>("Member_CustProfile_Get_V4", new { CustId = custId }, commandType: CommandType.StoredProcedure);
        }
        public int SubmittedCustPhotoId(int custId, int siteId, int currencyId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.ExecuteScalar<int>("Member_SubmittedCustPhotoId_GetV2", new { CustId = custId, SiteId = siteId, CurrencyId = currencyId }, commandType: CommandType.StoredProcedure);
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
                result = conn.Execute(sp, param, transaction: trans, commandType: CommandType.StoredProcedure);
            }
            return (result == 0) ? false : true;
        }

        public CustLineProfileModel GetCustLineProfile(int custId, string lineUserId)
        {
            return _reportDbClient.QueryFirst<CustLineProfileModel>("Member_CustLineProfile_Get", new DynamicParameters(new
            {
                _CustId = custId,
                _userId = lineUserId
            }));
        }

        public IEnumerable<BankInfoDetail> GetCustBankDetail(int custId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Query<BankInfoDetail>("Member_CustBankDetailByCustId_Get", new { CustId = custId }, commandType: CommandType.StoredProcedure);
        }
        public IEnumerable<BankInfoDetailData> GetCustBankDetail(int sysId, string accountNo, int siteId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Query<BankInfoDetailData>("Member_CustBankDetail_Get_V2", new { SysId = sysId, AccountNo = accountNo, SiteId = siteId }, commandType: CommandType.StoredProcedure);
        }
        public bool SetCustBankDetail(int custId, int sysId, string accountNum, string iFSCCode, CustBankDetailStatus status, MySqlConnection conn, MySqlTransaction trans)
        {
            var result = 0;
            var parameter = new DynamicParameters(new
            {
                CustId = custId,
                SysId = sysId,
                AccountNo = accountNum,
                IFSCCode = iFSCCode,
                Status = (int)status
            });
            result = _mainDbClient.Execute("Member_CustBankDetail_Set", parameter, conn, trans);
            return result > 0;
        }
        public bool DeleteCustBankInfo(int custId, int siteId, int sysId, string accountNum, string iFSCCode, CustBankDetailStatus status)
        {
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();
            using var trans = conn.BeginTransaction();
            var bankDetailResult = SetCustBankDetail(custId, sysId, accountNum, iFSCCode, status, conn, trans);
            var banklogResult = SetCustBankInfoLog(custId, siteId, null, sysId, accountNum, iFSCCode, CustBankInfoLogStatus.Delete, conn, trans);
            if (bankDetailResult & banklogResult)
            {
                trans.Commit();
            }
            return bankDetailResult & banklogResult;
        }

        public bool SetCustBankInfo(int custId, int siteId, string accountName, MySqlConnection conn, MySqlTransaction trans)
        {
            var result = 0;
            var parameter = new DynamicParameters(new
            {
                CustId = custId,
                SiteId = siteId,
                AccountName = accountName
            });
            result = _mainDbClient.Execute("Service_CustBankInfo_SetV3", parameter, conn, trans);
            return result > 0;
        }
        public IEnumerable<EWalletInfoDetail> GetCustEWalletInfo(int custId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Query<EWalletInfoDetail>("Member_CustEWalletDetail_Get", new { CustId = custId }, commandType: CommandType.StoredProcedure);
        }

        public IEnumerable<EWalletInfoDetailData> GetCustEWalletDetail(int sysId, string accountNo, int siteId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Query<EWalletInfoDetailData>("Member_CustEWalletDetailByAccountNo_Get", new { SysId = sysId, AccountNo = accountNo, SiteId = siteId }, commandType: CommandType.StoredProcedure);
        }

        public bool InsertCustEWalletDetail(int custId, int siteId, int sysId, string accountNum, CustEWalletDetailLogStatus status)
        {
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();
            using var trans = conn.BeginTransaction();
            var eWalletDetailResult = SetCustEWalletDetail(custId, sysId, accountNum, CustEWalletDetailStatus.Able, conn, trans);
            var eWalletlogResult = SetEWalletinfoLog(custId, siteId, sysId, accountNum, status, conn, trans);
            if (eWalletDetailResult & eWalletlogResult)
            {
                trans.Commit();
            }
            return eWalletDetailResult & eWalletlogResult;
        }
        public bool SetCustEWalletDetail(int custId, int sysId, string accountNum, CustEWalletDetailStatus status, MySqlConnection conn, MySqlTransaction trans)
        {
            var result = 0;
            var parameter = new DynamicParameters(new
            {
                CustId = custId,
                SysId = sysId,
                AccountNo = accountNum,
                Status = (int)status
            });
            result = _mainDbClient.Execute("Member_CustEWalletDetail_Set", parameter, conn, trans);
            return result > 0;
        }
        private bool SetEWalletinfoLog(int custId, int siteId, int sysId, string accountNum, CustEWalletDetailLogStatus status, MySqlConnection conn, MySqlTransaction trans)
        {
            var result = 0;
            var parameter = new DynamicParameters(new
            {
                CustId = custId,
                SiteId = siteId,
                SysId = sysId,
                AccountNum = accountNum,
                Status = (int)status
            });
            result = _mainDbClient.Execute("Service_EWalletinfoLog_Set", parameter, conn, trans);
            return result > 0;
        }

        public bool DeleteCustEWalletDetail(int custId, int siteId, int sysId, string accountNum, CustEWalletDetailStatus status)
        {
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();
            using var trans = conn.BeginTransaction();
            var eWalletDetailResult = SetCustEWalletDetail(custId, sysId, accountNum, status, conn, trans);
            var eWalletlogResult = SetEWalletinfoLog(custId, siteId, sysId, accountNum, CustEWalletDetailLogStatus.Delete, conn, trans);
            if (eWalletDetailResult & eWalletlogResult)
            {
                trans.Commit();
            }
            return eWalletDetailResult & eWalletlogResult;
        }
    }
}
