using Dapper;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using MemberSite.Service.Model.Line;
using System.Data;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace MemberSite.Service.DAL
{
    public class CustInfoService
    {
        private readonly PromotionService _promotionService;
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;
        private readonly ILogger<CustInfoService> _logger;

        public CustInfoService(IServiceProvider provider)
        {
            _promotionService = provider.GetRequiredService<PromotionService>();
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
            _logger = provider.GetRequiredService<ILogger<CustInfoService>>();
        }

        public bool ExistsUserName(string userName, int siteId)
        {
            return _reportDbClient.ExecuteScalar<int>("Member_ExistsUserName_Get", new { UserName = userName, SiteId = siteId }) != 0;
        }

        public bool ExistsPhoneNo(int custId, int siteId, string AreaCode, string PhoneNo)
        {
            return _reportDbClient.ExecuteScalar<int>("Member_ExistsPhoneNo_Get", new { CustId = custId, AreaCode, PhoneNo, SiteId = siteId }) != 0;
        }

        public bool ExistsEmail(int custId, string email, int siteId)
        {
            return _reportDbClient.ExecuteScalar<int>("Member_ExistsEmail_Get", new { CustId = custId, Email = email, SiteId = siteId }) != 0;
        }
        public int AddCustInfo(Customer customer, string Email, string lineUserId, int deviceType ,out int custId, string signupUrl = null, string referralUrl = null, string PhoneNo = null, string AreaCode = null, string RealName = null, string promoCode = null, int Birth_d = 0, int Birth_m = 0, int Birth_y = 0)
        {
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            
            try
            {
                custId = 0;
                var p = new DynamicParameters(new
                {
                    customer.SiteId, customer.UserName, customer.Userpwd, customer.CurrencyId, customer.UserLang,
                    customer.AgentId, customer.AllAgentId, customer.UserLevel
                });
                p.Add("CustId", dbType: DbType.String, direction: ParameterDirection.Output);
                
                var result = _mainDbClient.Execute("CustInfo_Ins", trans, p );

                if (result > 0)
                {
                    var strCustId = p.Get<string>("CustId") ?? "";
                    custId = int.Parse(strCustId);

                    if (custId == 0)
                    {
                        trans.Rollback();
                        conn.Close();
                        conn.Dispose();
                        return -5;
                    }
                }
                else
                {
                    trans.Rollback();
                    conn.Close();
                    conn.Dispose();
                    return -5;
                }

                if ((!string.IsNullOrEmpty(Email) ||
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
                    result = _mainDbClient.Execute("Member_CustProfile_Ins", trans, new DynamicParameters(new
                    {
                        CustId = custId,
                        customer.SiteId,
                        Email,
                        Profile = profile,
                        PhoneNo,
                        AreaCode
                    }));

                    if (!string.IsNullOrEmpty(RealName))
                    {
                        SetCustBankInfo(custId, customer.SiteId, RealName);
                    }

                }

                if (result > 0 && (!string.IsNullOrEmpty(signupUrl) || !string.IsNullOrEmpty(referralUrl)))
                {
                    result = _mainDbClient.Execute("Member_CustRegInfo_Ins_V2",trans,
                        new DynamicParameters(new
                            { CustId = custId, customer.SiteId, SignupUrl = signupUrl, ReferralUrl = referralUrl }));
                }

                if (result > 0 && !string.IsNullOrEmpty(promoCode) && _promotionService.CheckSpecialCode(promoCode))
                {
                    if (_promotionService.CheckScratchCardCode(promoCode))
                    {
                        var parameters = new DynamicParameters(new
                            { CustId = custId, ScratchCardCode = promoCode, DeviceType = 1 });
                        parameters.Add("Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
                        _mainDbClient.Execute("Member_CustScratchCardCodeV3_Ins", trans, parameters);
                        result = parameters.Get<int>("Result") == 0 ? 1 : 0;
                    }
                    else
                    {
                        result = _mainDbClient.Execute("Member_CustPromoCode_Ins",trans,
                            new DynamicParameters(new { CustId = custId, PromoCode = promoCode.Trim() }));
                    }

                    if (result == 0)
                    {
                        result = -7;
                    }
                }

                if (result > 0 && (int.TryParse(customer.Aff, out var iAff) || !string.IsNullOrEmpty(customer.AffCode)))
                {
                    result = _mainDbClient.Execute("Member_VNAffiliate_Ins",trans,
                        new DynamicParameters(new { CustId = custId, customer.SiteId, AffId = iAff, customer.AffCode }));
                }

                if (result > 0 && !string.IsNullOrWhiteSpace(lineUserId))
                {
                    if (GetCustLineProfile(0, lineUserId) != null)
                    {
                        result = -3002;
                    }
                    else
                    {
                        result = InsertCustLineProfile(custId, lineUserId, trans) ? 1 : 0;
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
            return _reportDbClient.QueryFirst<CustTransSummary>("Member_CustTransSummary_Get", new { CustId = custId });
        }

        public Customer GetCustInfoByMail(string email, int siteId)
        {
            return _reportDbClient.QueryFirst<Customer>("Member_CustomerByMail_Get", new { Email = email, SiteId = siteId });
        }

        public Customer GetCustInfoByUserName(string userName, int siteId)
        {
            return _mainDbClient.QueryFirst<Customer>("LoginCustInfo_Get", new { UserName = userName, SiteId = siteId });
        }
        
        public bool SetLanguage(int custId, string langId)
        {
            return _mainDbClient.Execute("LangId_Set", new { CustId = custId, LangId = langId }) > 0;
        }
        
        public int GetPointLevel(int custId)
        {
            return _reportDbClient.ExecuteScalar<int>("Member_PointLevel_Get", new { CustId = custId });
        }
        
        public bool SetPassword(int custId, string md5OldPassword, string md5NewPassword, out string md5Userpwd)
        {
            md5Userpwd = null;
            
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            
            var p = new DynamicParameters(new { CustId = custId, Md5OldPassword = md5OldPassword, Md5NewPassword = md5NewPassword });
            p.Add("Md5Userpwd", dbType: DbType.String, direction: ParameterDirection.Output);

            var result = _mainDbClient.Execute("Password_Set", trans, p);
            md5Userpwd = p.Get<string>("Md5Userpwd");
            trans.Commit();
            conn.Close();
            conn.Dispose();
            
            return result > 0;
        }
        
        public bool SetProfile(int custId, int siteId, Dictionary<string, string> profile)
        {
            string Email = profile[nameof(Email)];
            string AreaCode = profile[nameof(AreaCode)];
            string PhoneNo = profile[nameof(PhoneNo)];
            var result = _mainDbClient.Execute("Member_CustProfile_Set",
                new
                {
                    CustId = custId,
                    SiteId = siteId,
                    AreaCode,
                    Email,
                    PhoneNo,
                    Profile = Newtonsoft.Json.JsonConvert.SerializeObject(profile)
                });
            return result > 0;
        }
        
        public bool SetProfileV2(int custId, int siteId, Dictionary<string, string> profile)
        {
            string Email = profile[nameof(Email)];
            string AreaCode = profile[nameof(AreaCode)];
            string PhoneNo = profile[nameof(PhoneNo)];
            var result = _mainDbClient.Execute("Member_CustProfileV2_Set",
                new
                {
                    CustId = custId,
                    SiteId = siteId,
                    AreaCode,
                    Email,
                    PhoneNo,
                    Profile = Newtonsoft.Json.JsonConvert.SerializeObject(profile)
                });
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
        
        private IEnumerable<CustAccountNameInfo> GetDuplicateAccountNameInfo(int siteId, string accountName)
        {
            /* 相同定義：
             * * 大小寫不同
             * * 去空格相同
             * 舉例：
             * Cheong wai Choon
             * CheongwaiChoon（相同）
             * cheong Wai Choon（相同）
             * Ho thai nhat
             * Hồ thái nhật（不同）
             * HoThaiNhat（相同）
             */
            
            var patterns = new[]
            {
                accountName,
                accountName.Replace(" ", string.Empty)
            };
            var similarAccList = this.GetSimilarAccountName(siteId, patterns);
            var  result = similarAccList
                .Where(x => patterns.Any(y => y.Equals(x.AccountName, StringComparison.CurrentCultureIgnoreCase)));

            foreach (var info in result)
            {
                info.TagList = string.IsNullOrEmpty(info.StrTagList)
                    ? Array.Empty<int>() : JsonConvert.DeserializeObject<IEnumerable<int>>(info.StrTagList);
            }

            return result;
        }
        
        public bool SetCustBankInfo(int custId, int siteId, string accountName)
        {
            var p = new DynamicParameters(new
            {
                CustId = custId,
                SiteId = siteId,
                AccountName = accountName
            });

            var duplicateList = 
                GetDuplicateAccountNameInfo(siteId, accountName);
            
            if (!duplicateList.Any())
            {
                return _mainDbClient.Execute("Service_CustBankInfo_SetV3", p) > 0;
            }

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            
            var success = _mainDbClient.Execute("Service_CustBankInfo_SetV3", trans, p) > 0;
            
            if (success)
            {
                var custInfo = GetCustInfo(custId, true);
                var custTags = duplicateList
                    .Where(x => !x.TagList.Contains((int) CustTagEnum.Duplicate))
                    .ToList();
                
                var tags = string.IsNullOrEmpty(custInfo.TagList)
                    ? new List<int>()
                    : JsonSerializer.Deserialize<List<int>>(custInfo.TagList);

                if (!tags.Contains((int) CustTagEnum.Duplicate))
                {
                    custTags.Add(new CustAccountNameInfo {CustId = custId, TagList = tags});
                }
            
                if (!custTags.Any())
                {
                    MySqlClient.CommitAndDisposeConnection(conn, trans);
                    return true;
                }

                foreach (var item in custTags)
                {
                    item.TagList = item.TagList.Append((int) CustTagEnum.Duplicate);
                }
                
                success = this.SetCustInfoTagList(custTags, trans);
                if (success)
                {
                    MySqlClient.CommitAndDisposeConnection(conn, trans);
                    return true;
                }
            }
            
            MySqlClient.RollbackAndDisposeConnection(conn, trans);
            return false;
        }
        
        public bool InsertCustBankInfo(int custId, int siteId, string accountName, int sysId, string accountNum, string iFSCCode, CustBankInfoLogStatus status)
        {
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            
            var custBankInfoResult = true;
            var custBankInfo = GetCustBankInfo(custId);
            if (custBankInfo == null && !string.IsNullOrEmpty(accountName))
            {
                custBankInfoResult = SetCustBankInfo(custId, siteId, accountName, trans);
            }
            var bankDetailResult = SetCustBankDetail(custId, sysId, accountNum, iFSCCode, CustBankDetailStatus.Able, trans);
            var bankLogResult = SetCustBankInfoLog(custId, siteId, accountName, sysId, accountNum, iFSCCode, status, trans);
            if (bankDetailResult & bankLogResult & custBankInfoResult)
            {
                trans.Commit();
            }
            else
            {
                trans.Rollback();
            }
            conn.Close();
            conn.Dispose();
            
            return bankDetailResult & bankLogResult & custBankInfoResult;
        }

        private bool SetCustBankInfoLog(int custId, int siteId, string accountName, int sysId, string accountNum, string iFSCCode, CustBankInfoLogStatus status, IDbTransaction trans)
        {
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
            var result = _mainDbClient.Execute("Service_CustBankInfoLog_Set_V2", trans ,parameter);
            return result > 0;
        }
        public bool SettCustUSDTInfo(int custId, int siteId, string accountName, IEnumerable<USDTDetail> uSDTInfo, IEnumerable<USDTDetailLog> uSDTInfoLog)
        {
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            
            var custUsdtInfoResult = SetCustUSDTInfo(custId, siteId, accountName, uSDTInfo, trans);
            var custUsdtInfoLogResult = SetCustUSDTInfoLog(custId, siteId, accountName, uSDTInfoLog, trans);
            if (custUsdtInfoResult & custUsdtInfoLogResult)
            {
                trans.Commit();
            }
            else
            {
                trans.Rollback();
            }
            conn.Close();
            conn.Dispose();
            
            return custUsdtInfoResult & custUsdtInfoLogResult;
        }
        
        private bool SetCustUSDTInfo(int custId, int siteId, string accountName, IEnumerable<USDTDetail> uSDTInfo, IDbTransaction trans)
        {
            var parameter = new DynamicParameters(new
            {
                CustId = custId,
                SiteId = siteId,
                AccountName = accountName,
                USDTInfo = Newtonsoft.Json.JsonConvert.SerializeObject(uSDTInfo)
            });
            var result = _mainDbClient.Execute("Service_CustUSDTInfo_Set", trans ,parameter);
            return result > 0;
        }
        
        private bool SetCustUSDTInfoLog(int custId, int siteId, string accountName, IEnumerable<USDTDetail> uSDTInfo, IDbTransaction trans)
        {
            var parameter = new DynamicParameters(new
            {
                CustId = custId,
                SiteId = siteId,
                AccountName = accountName,
                USDTInfo = Newtonsoft.Json.JsonConvert.SerializeObject(uSDTInfo)
            });
            var result = _mainDbClient.Execute("Service_CustUSDTInfoLog_Set_V2", trans ,parameter);
            return result > 0;
        }

        public CustBankInfo GetCustBankInfo(int custId)
        {
            return _mainDbClient.QueryFirst<CustBankInfo>("Member_CustBankInfo_Get", new { CustId = custId });
        }
        
        public string GetDefAccountNo(int custId, int siteId)
        {
            return _reportDbClient.QueryFirst<string>("Member_DefAccountNo_Get", new { CustId = custId, SiteId = siteId });
        }
        
        public CustInfoProfile GetCustInfoProfile(int custId)
        {
            return _reportDbClient.QueryFirst<CustInfoProfile>("Member_CustProfile_Get_V4", new { CustId = custId });
        }
        
        public int SubmittedCustPhotoId(int custId, int siteId, int currencyId)
        {
            return _reportDbClient.ExecuteScalar<int>("Member_SubmittedCustPhotoId_GetV2", new { CustId = custId, SiteId = siteId, CurrencyId = currencyId });
        }
        
        public bool SetResetPwd(int custId, string md5NewPassword)
        {
            return _mainDbClient.Execute("Member_ResetPwd_Set", new { CustId = custId, Md5NewPassword = md5NewPassword }) > 0;
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

        public bool InsertCustLineProfile(int custId, string lineUserId, IDbTransaction trans = null)
        {
            var sp = "Member_CustLineProfile_Ins";
            var param = new DynamicParameters(new { _CustId = custId, _userId = lineUserId });
            int result;

            if (trans == null)
            {
                //No execute in transaction
                result = _mainDbClient.Execute(sp, param);
            }
            else
            {
                //Execute in transaction
                result = _mainDbClient.Execute(sp, trans ,param);
            }
            return (result != 0);
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
            return _mainDbClient.Query<BankInfoDetail>("Member_CustBankDetailByCustId_Get", new { CustId = custId });
        }
        
        public IEnumerable<BankInfoDetailData> GetCustBankDetail(int sysId, string accountNo, int siteId)
        {
            return _mainDbClient.Query<BankInfoDetailData>("Member_CustBankDetail_Get_V2", new { SysId = sysId, AccountNo = accountNo, SiteId = siteId });
        }

        private bool SetCustBankDetail(int custId, int sysId, string accountNum, string iFSCCode, CustBankDetailStatus status, IDbTransaction trans)
        {
            var parameter = new DynamicParameters(new
            {
                CustId = custId,
                SysId = sysId,
                AccountNo = accountNum,
                IFSCCode = iFSCCode,
                Status = (int)status
            });
            var result = _mainDbClient.Execute("Member_CustBankDetail_Set", trans ,parameter);
            return result > 0;
        }
        
        public bool DeleteCustBankInfo(int custId, int siteId, int sysId, string accountNum, string iFSCCode, CustBankDetailStatus status)
        {
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            
            var bankDetailResult = SetCustBankDetail(custId, sysId, accountNum, iFSCCode, status, trans);
            var bankLogResult = SetCustBankInfoLog(custId, siteId, null, sysId, accountNum, iFSCCode, CustBankInfoLogStatus.Delete,trans);
            if (bankDetailResult & bankLogResult)
            {
                trans.Commit();
            }
            else
            {
                trans.Rollback();
            }
            conn.Close();
            conn.Dispose();
            
            return bankDetailResult & bankLogResult;
        }

        private bool SetCustBankInfo(int custId, int siteId, string accountName, IDbTransaction trans)
        {
            var parameter = new DynamicParameters(new
            {
                CustId = custId,
                SiteId = siteId,
                AccountName = accountName
            });
            var result = _mainDbClient.Execute("Service_CustBankInfo_SetV3", trans , parameter);
            return result > 0;
        }
        
        public IEnumerable<EWalletInfoDetail> GetCustEWalletInfo(int custId)
        {
            return _mainDbClient.Query<EWalletInfoDetail>("Member_CustEWalletDetail_Get", new { CustId = custId });
        }

        public IEnumerable<EWalletInfoDetailData> GetCustEWalletDetail(int sysId, string accountNo, int siteId)
        {
            return _mainDbClient.Query<EWalletInfoDetailData>("Member_CustEWalletDetailByAccountNo_Get", new { SysId = sysId, AccountNo = accountNo, SiteId = siteId });
        }

        public bool InsertCustEWalletDetail(int custId, int siteId, int sysId, string accountNum, CustEWalletDetailLogStatus status)
        {
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            
            var eWalletDetailResult = SetCustEWalletDetail(custId, sysId, accountNum, CustEWalletDetailStatus.Able, trans);
            var eWalletLogResult = SetEWalletinfoLog(custId, siteId, sysId, accountNum, status, trans);
            if (eWalletDetailResult & eWalletLogResult)
            {
                trans.Commit();
            }
            else
            {
                trans.Rollback();
            }
            conn.Close();
            conn.Dispose();
            
            return eWalletDetailResult & eWalletLogResult;
        }

        private bool SetCustEWalletDetail(int custId, int sysId, string accountNum, CustEWalletDetailStatus status, IDbTransaction trans)
        {
            var parameter = new DynamicParameters(new
            {
                CustId = custId,
                SysId = sysId,
                AccountNo = accountNum,
                Status = (int)status
            });
            var result = _mainDbClient.Execute("Member_CustEWalletDetail_Set", trans ,parameter);
            return result > 0;
        }
        
        private bool SetEWalletinfoLog(int custId, int siteId, int sysId, string accountNum, CustEWalletDetailLogStatus status, IDbTransaction trans)
        {
            var parameter = new DynamicParameters(new
            {
                CustId = custId,
                SiteId = siteId,
                SysId = sysId,
                AccountNum = accountNum,
                Status = (int)status
            });
            var result = _mainDbClient.Execute("Service_EWalletinfoLog_Set", trans, parameter);
            return result > 0;
        }

        public bool DeleteCustEWalletDetail(int custId, int siteId, int sysId, string accountNum, CustEWalletDetailStatus status)
        {
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            
            var eWalletDetailResult = SetCustEWalletDetail(custId, sysId, accountNum, status, trans);
            var eWalletLogResult = SetEWalletinfoLog(custId, siteId, sysId, accountNum, CustEWalletDetailLogStatus.Delete,trans);
            if (eWalletDetailResult & eWalletLogResult)
            {
                trans.Commit();
            }
            else
            {
                trans.Rollback();
            }
            conn.Close();
            conn.Dispose();
            
            return eWalletDetailResult & eWalletLogResult;
        }
        
        public IEnumerable<CustAccountNameInfo> GetSimilarAccountName(int siteId, IEnumerable<string> accountNames)
        {
            return _mainDbClient.Query<CustAccountNameInfo>("Member_SimilarAccountName_Get",
                new
                {
                    _SiteId = siteId,
                    _AccountNames = JsonSerializer.Serialize(accountNames)
                });
        }
        
        public bool SetCustInfoTagList(IEnumerable<CustTagList> tagList, IDbTransaction trans)
        {
            var parameter = new DynamicParameters(new
            {
                _Infos = JsonSerializer.Serialize(tagList)
            });
            
            return _mainDbClient.Execute("Service_CustInfoTagList_Set", trans, parameter) > 0;
        }
    }
}
