using MemberSite.Service.DAL;
using MemberSite.Service.DAL.Interface;
using MemberSite.Service.Domain;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using MemberSite.Service.Model.Mapping;

using Microsoft.Extensions.Caching.Memory;

using Newtonsoft.Json;

namespace MemberSite.Service.BLL
{
    public class CustInfoBll
    {
        private readonly Tools _tools;
        private readonly VerificationRewardBLL _verificationRewardBll;
        private readonly IRabbitMQProducer _producer;
        private readonly CustInfoService _custInfoService;
        private readonly AuthenticateService _authenticateService;
        private readonly IAffiliateService _affiliateService;
        private readonly ServerInfoBll _serverInfoBll;
        private readonly AgentInfoService _agentInfoService;
        private readonly BankInfoService _bankInfoService;
        private readonly CommonBLL _commonBll;
        private readonly CacheHelper _cacheHelper;
        private readonly ICacheHelper _redisHelper;
        private readonly Dictionary<int, bool> _isConvert = new()
        {
            { 51, false },
            { 15, false }
        };
        private readonly IEnumerable<AreaCodeMap> _areaCodeMap;
        private readonly ShieldBLL _shieldBll;
        private readonly VIPService _vIPService;
        private readonly DepositService _depositService;

        public CustInfoBll(IServiceProvider provider)
        {
            _tools = provider.GetRequiredService<Tools>();
            _verificationRewardBll = provider.GetRequiredService<VerificationRewardBLL>();
            _custInfoService = provider.GetRequiredService<CustInfoService>();
            _authenticateService = provider.GetRequiredService<AuthenticateService>();
            _serverInfoBll = provider.GetRequiredService<ServerInfoBll>();
            _agentInfoService = provider.GetRequiredService<AgentInfoService>();
            _bankInfoService = provider.GetRequiredService<BankInfoService>();
            _commonBll = provider.GetRequiredService<CommonBLL>();
            _cacheHelper = provider.GetRequiredService<CacheHelper>();
            _areaCodeMap = provider.GetRequiredService<IEnumerable<AreaCodeMap>>();
            _redisHelper = provider.GetRequiredService<ICacheHelper>();
            _shieldBll = provider.GetRequiredService<ShieldBLL>();
            _producer = provider.GetRequiredService<IRabbitMQProducer>();
            _affiliateService = provider.GetRequiredService<IAffiliateService>();
            _vIPService = provider.GetRequiredService<VIPService>();
            _depositService = provider.GetRequiredService<DepositService>();
        }
        public string GetCountryNameByAreaCode(string areaCode)
        {
            var countryName = _areaCodeMap.FirstOrDefault(x => x.AreaCode == areaCode);
            return countryName?.Code;
        }

        public string GetAreaCodeByCountryName(string countryName)
        {
            var areaCode = _areaCodeMap.FirstOrDefault(x => x.Code == countryName);
            return areaCode?.AreaCode;
        }

        public bool IsConvertRequired(int currencyId)
        {
            return _isConvert.ContainsKey(currencyId);
        }

        public int AddCustInfo(string UserName, string Userpwd, string Email, int CurrencyId, string UserLang,
            string refId, string affId, string aff, AffCodeObject affCode, string lineUserId, int deviceType, out int custId, string signupUrl = null,
            string referralUrl = null, string PhoneNo = null, string AreaCode = null, string RealName = null, string promoCode = null,
            int Birth_d = 0, int Birth_m = 0, int Birth_y = 0)
        {
            var serverInfo = _serverInfoBll.GetServerInfo();
            if (string.IsNullOrEmpty(affId) && !serverInfo.ICurrencyList.Contains(CurrencyId))
            {
                custId = 0;
                return -1;
            }

            if (!_tools.CheckUserName(UserName))
            {
                custId = 0;
                return -2;
            }

            if (!_tools.CheckUserInputPassword(Userpwd))
            {
                custId = 0;
                return -3;
            }

            if (!serverInfo.ILangList.Contains(UserLang))
            {
                custId = 0;
                return -4;
            }
            if (affCode != null && !affCode.IsExists(serverInfo.SiteId, _affiliateService.ExistsAffiliateCode))
            {
                custId = 0;
                return -10;
            }

            Customer agent = null;
            //如果從CreditSite註冊，且有輸入邀請碼(Referral)，則該用戶屬於邀請碼下線
            if (!string.IsNullOrEmpty(refId))
            {
                agent = _agentInfoService.GetAffiliateAgent($"{refId}_{CurrencyId}", serverInfo.SiteId);

                if (agent?.CurrencyId != CurrencyId)
                {
                    custId = 0;
                    return -19; /*agent Currency != CurrencyId*/
                }
            }
            else if (!string.IsNullOrEmpty(affId))
            {
                agent = _agentInfoService.GetAffiliateAgent(affId, serverInfo.SiteId);

                if (agent == null)
                {
                    custId = 0;
                    return -19;
                }
            }

            agent ??= _agentInfoService.GetSuperAgent(serverInfo.SiteId, CurrencyId);

            var regCustInfo = new Customer
            {
                UserName = UserName,
                Userpwd = _tools.MD5Encrypt(Userpwd),
                AgentId = agent.CustId,
                AllAgentId = GetAllAgentByUserId(agent),
                ActStatus = (int)CustActStatus.NonDepoist,
                CurrencyId = agent.CurrencyId,
                IsTest = agent.IsTest,
                SiteId = serverInfo.SiteId,
                UserLevel = 1,
                UserLang = UserLang,
                Aff = aff,
                AffCode = affCode?.Value
            };

            if (string.IsNullOrEmpty(signupUrl))
            {
                signupUrl = _tools.GetFromUrl();
            }

            var result = _custInfoService.AddCustInfo(regCustInfo, Email, lineUserId, deviceType, out custId, signupUrl, referralUrl, PhoneNo, AreaCode,
                RealName, promoCode, Birth_d, Birth_m, Birth_y);

            if (result == 0 && regCustInfo.SiteId != (int)SiteId.Nova88)
            {
                regCustInfo.CustId = custId;
                _ = Task.Run(async () =>
                {
                    var checkResult = _shieldBll.CheckCustShieldRisk(
                        regCustInfo,
                        ShieldRiskTriggeredPoint.Registration,
                        ShieldRiskTaggedIssue.HighRisk);
                });
            }
            return result != 0 ? result : 0;
        }

        private string GetAllAgentByUserId(Customer agent)
        {
            if (agent.AgentId == 0 || agent.AllAgentId == null)
            {
                return agent.AllAgentId = $"[{agent.CustId}]";
            }
            else
            {
                var allAgentId = JsonConvert.DeserializeObject<List<int>>(agent.AllAgentId);
                allAgentId.Add(agent.CustId);
                return JsonConvert.SerializeObject(allAgentId);
            }
        }

        public Customer GetCustInfoByUserName(string userName)
        {
            var serverInfo = _serverInfoBll.GetServerInfo();
            var key = $"CustInfoByUserName_{serverInfo.SiteId}_{userName}";
            return _cacheHelper.TryGetOrCreate(key,
                new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(1)),
                i => _custInfoService.GetCustInfoByUserName(userName, serverInfo.SiteId));
        }
        public int UserLogin(bool isLineLogin, int custId, string userName, string pwd, string ip, string fromUrl, string userAgent, int deviceType, ref Customer customer, out string token)
        {
            var serverInfo = _serverInfoBll.GetServerInfo();
            token = null;
            if (isLineLogin)
            {
                customer = _custInfoService.GetCustInfo(custId);
                if (customer == null) return -1;
            }
            else
            {
                customer = _custInfoService.GetCustInfoByUserName(userName, serverInfo.SiteId);
                if (customer == null) return -1;

                if (customer.Userpwd != _tools.MD5Encrypt(pwd))
                {
                    return -2;
                }
            }

            if (!VerifyCustomerStatus(customer.Status))
            {
                return -3;
            }

            if (customer.SiteId != (int)SiteId.Nova88)
            {
                var info = customer;
                _ = Task.Run(async () =>
                {
                    // Check Shield Info is high risk
                    var checkResult = _shieldBll.CheckCustShieldRisk(
                        info,
                        ShieldRiskTriggeredPoint.Login,
                        ShieldRiskTaggedIssue.HighRisk);

                    // send custId to tp.Shield
                    var request = new SendShieldAttributesRequest()
                    {
                        SiteId = info.SiteId,
                        UserId = info.CustId.ToString()
                    };
                    var result = await _shieldBll.SendShieldAttributes(request);
                });
            }

            _authenticateService.AddLoginRecord(customer.CustId, customer.SiteId, ip, deviceType, out token);
            _authenticateService.AddUserLoginLog(customer.CustId, customer.UserName, customer.SiteId, ip, fromUrl, userAgent);
            return 0;
        }

        public int UserLoginForResetPwd(string userName, string ip, string fromUrl, string userAgent, int deviceType, ref Customer customer, out string token)
        {
            var serverInfo = _serverInfoBll.GetServerInfo();
            customer = _custInfoService.GetCustInfoByUserName(userName, serverInfo.SiteId);
            token = null;
            if (customer == null)
            {
                return -1;
            }

            if (!VerifyCustomerStatus(customer.Status))
            {
                return -3;
            }

            _authenticateService.AddLoginRecord(customer.CustId, customer.SiteId, ip, deviceType, out token);
            _authenticateService.AddUserLoginLog(customer.CustId, customer.UserName, customer.SiteId, ip, fromUrl, userAgent);
            return 0;
        }

        public int GetCustomer(string userName, ref Customer customer)
        {
            var serverInfo = _serverInfoBll.GetServerInfo();
            customer = _custInfoService.GetCustInfoByUserName(userName, serverInfo.SiteId);
            if (customer == null)
            {
                return -1;
            }

            return 0;
        }

        public int SetLanguage(int custId, string langId)
        {
            var serverInfo = _serverInfoBll.GetServerInfo();
            if (!serverInfo.ILangList.Contains(langId))
            {
                return -1;
            }

            _custInfoService.SetLanguage(custId, langId);

            return 0;
        }
        public Customer GetCustInfoWithCache(int custId, bool forceUseMainDb = false)
        {
            return _redisHelper.TryGetOrCreate($"CustInfo:{custId}", TimeSpan.FromMinutes(1),
                () => _custInfoService.GetCustInfo(custId, forceUseMainDb),
                RedisDataBase.Shared);
        }

        public Customer GetCustInfo(int custId)
        {
            return _custInfoService.GetCustInfo(custId);
        }

        public CustTransSummary GetCustTransSummary(int custId)
        {
            return _custInfoService.GetCustTransSummary(custId);
        }

        public Customer GetCustInfoByMail(string email)
        {
            var serverInfo = _serverInfoBll.GetServerInfo();
            var key = $"CustInfoByMai_{serverInfo.SiteId}_{email}";
            return _cacheHelper.TryGetOrCreate(key,
                new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(1)),
                i => _custInfoService.GetCustInfoByMail(email, serverInfo.SiteId));
        }

        public int GetPointLevelWithCache(int custId)
        {
            return _redisHelper.TryGetOrCreate($"PointLevel:{custId}", TimeSpan.FromMinutes(1),
                () => _custInfoService.GetPointLevel(custId),
                RedisDataBase.Shared);
        }

        public int SetPassword(int custId, string oldPassword, string newPassword)
        {
            if (!_tools.CheckUserInputPassword(newPassword))
            {
                return -2;
            }

            var md5OldPassword = _tools.MD5Encrypt(oldPassword);
            var md5NewPassword = _tools.MD5Encrypt(newPassword);


            var isSet = _custInfoService.SetPassword(custId, md5OldPassword, md5NewPassword, out var md5Userpwd);
            if (!isSet)
            {
                if (md5OldPassword != md5Userpwd)
                {
                    return -3;
                }

                return -4;
            }
            else
            {
                return 0;
            }
        }

        public bool SetResetPwd(int custId, string newPassword)
        {
            var md5NewPassword = _tools.MD5Encrypt(newPassword);
            return _custInfoService.SetResetPwd(custId, md5NewPassword);
        }
        public int SetProfile(int custId, Dictionary<string, string> custProfile)
        {
            var custInfo = GetCustInfoWithCache(custId);
            return _custInfoService.SetProfile(custId, custInfo.SiteId, custProfile) ? 0 : -1;
        }
        public int SetProfileV2(int custId, Dictionary<string, string> custProfile)
        {
            var custInfo = GetCustInfoWithCache(custId);
            return _custInfoService.SetProfileV2(custId, custInfo.SiteId, custProfile) ? 0 : -1;
        }

        public int SetProfileAddress(int custId, Dictionary<string, string> custProfile)
        {
            var custInfo = GetCustInfoWithCache(custId);
            return _custInfoService.SetAddress(custId, custInfo.SiteId, ConvertAddress(custProfile)) ? 0 : -1;
        }

        public int SetCustBankInfo(int custId, string accountName, bool forceUseMainDb = false)
        {
            var custInfo = GetCustInfoWithCache(custId, forceUseMainDb);

            return _custInfoService.SetCustBankInfo(custId, custInfo.SiteId, accountName) ? 0 : -1;
        }
        public int InsertCustBankInfo(int custId, string accountName, int sysId, string accountNum, string iFSCCode, CustBankInfoLogStatus status)
        {
            var custInfo = GetCustInfoWithCache(custId);
            return _custInfoService.InsertCustBankInfo(custId, custInfo.SiteId, accountName, sysId, accountNum, iFSCCode, status) ? 0 : -1;
        }

        public int SetCustUSDTInfo(int custId, string accountName, IEnumerable<CryptoWalletDetail> uSDTInfo, IEnumerable<CryptoWalletDetailLog> uSDTInfoLog)
        {
            var custInfo = GetCustInfoWithCache(custId);
            return _custInfoService.SettCustUSDTInfo(custId, custInfo.SiteId, accountName, uSDTInfo, uSDTInfoLog) ? 0 : -1;
        }
        public CustBankInfo GetCustBankInfo(int custId)
        {
            var custBankInfo = _custInfoService.GetCustBankInfo(custId);

            if (custBankInfo != null)
            {
                var bankInfoDetailList = _custInfoService.GetCustBankDetail(custId);
                custBankInfo.bankInfoDetail = bankInfoDetailList;

                var cryptoWalletDetail = _custInfoService.GetCustCryptoWalletDetail(custId);
                custBankInfo.usdtDetail = cryptoWalletDetail;
            }

            return custBankInfo;
        }
        public string GetDefAccountNo(int custId)
        {
            var custInfo = GetCustInfoWithCache(custId);
            return _custInfoService.GetDefAccountNo(custId, custInfo.SiteId);
        }
        public bool ExistsUserName(string userName)
        {
            var serverInfo = _serverInfoBll.GetServerInfo();
            return _custInfoService.ExistsUserName(userName, serverInfo.SiteId);
        }

        public bool ExistsPhoneNo(int custId, string AreaCode, string PhoneNo)
        {
            var serverInfo = _serverInfoBll.GetServerInfo();
            return _custInfoService.ExistsPhoneNo(custId, serverInfo.SiteId, AreaCode, PhoneNo);
        }
        public bool ExistsEmail(int custId, string email)
        {
            var siteId = 0;
            if (custId == 0)
            {
                var serverInfo = _serverInfoBll.GetServerInfo();
                siteId = serverInfo.SiteId;
            }
            else
            {
                var custInfo = GetCustInfoWithCache(custId);
                siteId = custInfo.SiteId;
            }

            return _custInfoService.ExistsEmail(custId, email, siteId);
        }

        /// <summary>
        /// Check user's have AccountName or not
        /// </summary>
        /// <param name="custId"></param>
        /// <returns>
        /// true=>existed
        /// false=>not exists
        /// </returns>
        public bool CheckAccountName(int custId)
        {
            var custBankInfo = GetCustBankInfo(custId);
            return custBankInfo != null;
        }
        public CustInfoProfile GetCustInfoProfile(int custId)
        {
            return _custInfoService.GetCustInfoProfile(custId);
        }
        public CustInfoProfile GetCustInfoProfileWithCache(int custId)
        {
            var key = $"CustInfoProfile_{custId}";
            return _cacheHelper.TryGetOrCreate(key,
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(1)),
                i => GetCustInfoProfile(custId));
        }
        public int SubmittedCustPhotoId(int custId)
        {
            var custInfo = GetCustInfoWithCache(custId);
            return _custInfoService.SubmittedCustPhotoId(custId, custInfo.SiteId, custInfo.CurrencyId);
        }
        public void ForgetPwd1(int custId, string resetPasswordUrl)
        {
            _commonBll.SendResetPwdMail(custId, resetPasswordUrl);
        }

        public async Task<ApiResult> ForgetPwd1Async(int custId, string resetPasswordUrl)
        {
            var result = await _commonBll.SendResetPwdMailAsync(custId, resetPasswordUrl);
            return result;
        }

        public async Task<ApiResult> VerifyOTPAsync(int custId, string inputOTP)
        {
            var apiResult = await _commonBll.VerifyOTPAsync(custId, inputOTP);

            if (apiResult.ErrorCode == 0 && !await _custInfoService.GetPhoneVerified(custId))
            {
                _custInfoService.SetPhoneVerified(custId);
                await _verificationRewardBll.PhoneReward(custId);
            }
            return apiResult;
        }

        public async Task<ApiResult> VerifyEmailAsync(int custId, string inputOtp)
        {
            var apiResult = await _commonBll.VerifyEmailAsync(custId, inputOtp);

            if (apiResult.ErrorCode == 0 && !await _custInfoService.GetEmailVerified(custId))
            {
                _custInfoService.SetEmailVerified(custId);
                await _verificationRewardBll.EmailReward(custId);
            }
            return apiResult;
        }

        public string GetAffiliateId(int custId)
        {
            var custInfo = GetCustInfoWithCache(custId);
            var affiliateId = _agentInfoService.GetAffiliateId(custId, custInfo.SiteId);
            while (affiliateId == null)
            {
                var _affiliateId = GenerateRandomAffiliateId();
                try
                {
                    _agentInfoService.AddAffiliateId(_affiliateId, custId, custInfo.SiteId);
                    affiliateId = _affiliateId;
                }
                catch (MySql.Data.MySqlClient.MySqlException mySqlEx)
                {
                    if (mySqlEx.Number == 1062)/*Duplicate for key 'PRIMARY'*/
                    {
                        affiliateId = _agentInfoService.GetAffiliateId(custId, custInfo.SiteId, DataBase.Main);
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return affiliateId;
        }
        public static string GenerateRandomAffiliateId()
        {
            var chars = "1234567890abcdefghijklmnopqrstuvwxyz";
            var stringChars = new System.Text.StringBuilder();
            var random = new Random();

            for (var i = 0; i < 8; i++)
            {
                stringChars.Append(chars[random.Next(chars.Length)]);
            }

            return stringChars.ToString();
        }
        public IEnumerable<ReferralSetting> GetReferralSetting(int siteId, int currencyId)
        {
            return _agentInfoService.GetReferralSetting(siteId, currencyId);
        }

        public async Task<ReferralSettingDetail> GetReferralSettingDetailAsync(int siteId, int currencyId, string langId)
        {
            var referralData = await _agentInfoService.GetReferralSettingDetailAsync(siteId, currencyId);
            if (referralData.Status == ReferralStatusEnum.Disable) { return null; }

            var referralSettingDetail = new ReferralSettingDetail()
            {
                ReferralReward = referralData.BonusAmount,
                InvitationLimit = referralData.InvitationLimit,
                AgentValidBet = referralData.AgentValidBet,
                AgentDeposit = referralData.AgentDeposit,
                MemberReward = referralData.ReferralFreeCredit,
                Deposit = referralData.Deposit,
                VaildBet = referralData.VaildBet,
                PeriodDays = referralData.PeriodDays,
                DepositRewardPercentage = referralData.DepositRewardPercentage,
                MinPayout = referralData.MinPayout,
                MaxPayout = referralData.MaxPayout,
                ReferralContent = DeserializeReferralContent(referralData.Status, referralData.Details, langId)
            };
            return referralSettingDetail;
        }

        private ReferralContent DeserializeReferralContent(ReferralStatusEnum status, string details, string langId)
        {
            var oldLang = langId.Split('_');
            var lang = oldLang.Length == 2 ? oldLang[1] : oldLang[0];
            var referralContents = JsonConvert.DeserializeObject<IEnumerable<ReferralContent>>(details);
            return referralContents?.FirstOrDefault(a => a.LangId == lang && (int)a.Type == (int)status);
        }

        public int GetAffiliateAgentBll(string affId)
        {
            var serverInfo = _serverInfoBll.GetServerInfo();
            if (!string.IsNullOrEmpty(affId))
            {
                var agent = _agentInfoService.GetAffiliateAgent(affId, serverInfo.SiteId);

                if (agent == null)
                {
                    return -19;
                }

                return agent.CurrencyId;
            }

            return -1;

        }

        public int AffiliateAndReferralCheck(string affCode, out int currencyId)
        {
            currencyId = 0;
            var serverInfo = _serverInfoBll.GetServerInfo();
            if (affCode.Length == 5)
            {
                var affCodeObj = new AffCodeObject(affCode);
                if (!affCodeObj.IsValid())
                {
                    return -9;
                }
                if (!affCodeObj.IsExists(serverInfo.SiteId, _affiliateService.ExistsAffiliateCode))
                {
                    return -10;
                }
                return 0;
            }
            else if (affCode.Length == 8)
            {
                var rffCodeObj = new ReferralObject(affCode);
                if (!rffCodeObj.IsValid())
                {
                    return -9;
                }
                if (rffCodeObj.IsExists(GetAffiliateAgentBll) < 0)
                {
                    return -10;
                }
                currencyId = rffCodeObj.CurrencyId;
                return 0;
            }

            return -10;
        }

        public void GetReferralSummary(int custId, ReferralTypeEnum type, out int referralCount, out int conditionCount, out int amountCount, out int amountSum, out int hasReferral)
        {
            _agentInfoService.GetReferralSummary(custId, type, out referralCount, out conditionCount, out amountCount, out amountSum, out hasReferral);
        }
        public IEnumerable<ReferralFriend> GetRefFriendList(int custId, long? StartDate, long? EndDate, int PageSize, ref int PageNumber, ref int TotalPages, ref int RowCount)
        {
            return _agentInfoService.GetRefFriendList(custId, StartDate, EndDate, PageSize, ref PageNumber, ref TotalPages, ref RowCount);
        }

        public IEnumerable<DepositReferralDao> GetDepositReferralList(int custId, long? startDate, long? endDate, int pageSize, int pageNumber, ref int totalPages, ref int rowCount)
        {
            return _agentInfoService.GetDepositReferralList(custId, startDate, endDate, Math.Abs(pageSize), (Math.Abs(pageNumber) - 1) * Math.Abs(pageSize), ref totalPages, ref rowCount);
        }

        public IEnumerable<DepositReferralDao> GetDepositReferralDetail(int custId, long? startDate, long? endDate, int pageSize, int pageNumber, ref int totalPages, ref int rowCount)
        {
            return _agentInfoService.GetDepositReferralDetail(custId, startDate, endDate, Math.Abs(pageSize), (Math.Abs(pageNumber) - 1) * Math.Abs(pageSize), ref totalPages, ref rowCount);
        }

        public bool VerifyCustomerStatus(int status)
        {
            if (status == (int)CustStatus.Suspend || status == (int)CustStatus.Closed ||
                status == (int)CustStatus.Lock)
            {
                {
                    return false;
                }
            }

            return true;
        }

        private static List<string> AddressKeys = new List<string>() { "Country", "State", "PostCode", "City", "LineFirst", "LineSecond" };
        private Dictionary<string, string> ConvertAddress(Dictionary<string, string> addressProfile)
        {
            var result = new Dictionary<string, string>();
            foreach (var key in AddressKeys)
            {
                if (addressProfile.ContainsKey($"Address{key}"))
                {
                    result.Add(key, (string.IsNullOrWhiteSpace(addressProfile[$"Address{key}"]) ? "" : addressProfile[$"Address{key}"]));
                    addressProfile.Remove($"Address{key}");
                }
                else
                {
                    result.Add(key, "");
                }
            }

            return result;
            ;
        }

        public IEnumerable<BankInfoDetailData> GetCustBankDetail(int sysId, string accountNo, int siteId)
        {
            return _custInfoService.GetCustBankDetail(sysId, accountNo, siteId);
        }

        public int DeleteCustBankInfo(int custId, int sysId, string accountNum, string iFSCCode, CustBankDetailStatus status)
        {
            var custInfo = GetCustInfoWithCache(custId);
            return _custInfoService.DeleteCustBankInfo(custId, custInfo.SiteId, sysId, accountNum, iFSCCode, status) ? 0 : -1;
        }

        public IEnumerable<EWalletInfoDetail> GetCustEWalletInfo(int custId)
        {
            return _custInfoService.GetCustEWalletInfo(custId);
        }

        public IEnumerable<EWalletInfoDetailData> GetCustEWalletDetail(int sysId, string accountNo, int siteId)
        {
            return _custInfoService.GetCustEWalletDetail(sysId, accountNo, siteId);
        }

        public int InsertCustEWalletDetail(int custId, int sysId, string accountNum, CustEWalletDetailLogStatus status)
        {
            var custInfo = GetCustInfoWithCache(custId);
            return _custInfoService.InsertCustEWalletDetail(custId, custInfo.SiteId, sysId, accountNum, status) ? 0 : -1;
        }

        public int DeleteCustEWalletDetail(int custId, int sysId, string accountNum, CustEWalletDetailStatus status)
        {
            var custInfo = GetCustInfoWithCache(custId);
            return _custInfoService.DeleteCustEWalletDetail(custId, custInfo.SiteId, sysId, accountNum, status) ? 0 : -1;
        }

        public IEnumerable<CryptoWalletDetailData> GetCustCryptoWalletDetail(string address, int siteId)
        {
            return _custInfoService.GetCustCryptoWalletDetail(address, siteId);
        }

        public int InsertCustCryptoWalletDetail(int custId, int currencyId, string network, string address, string nickName, string accountName)
        {
            var custInfo = GetCustInfoWithCache(custId);
            return _custInfoService.InsertCustCryptoWalletDetail(custId, custInfo.SiteId, currencyId, network, address, nickName, accountName) ? 0 : -1;
        }

        public int DeleteCustCryptoWalletDetail(int custId, int currencyId, string network, string address, string nickName)
        {
            var custInfo = GetCustInfoWithCache(custId);
            return _custInfoService.DeleteCustCryptoWalletDetail(custId, custInfo.SiteId, currencyId, network, address, nickName) ? 0 : -1;
        }

        public IEnumerable<CryptoWalletDetail> GetCustCryptoWalletDetail(int custId)
        {
            return _custInfoService.GetCustCryptoWalletDetail(custId);
        }

        public async Task<IEnumerable<CustTpGameTypeSetting>> GetCustTpGameTypeSetting(int custId)
        {
            var result = new List<CustTpGameTypeSetting>();
            var types = (CustTpGameTypeSettingType[])Enum.GetValues(typeof(CustTpGameTypeSettingType));
            var key = $"GetCustTpGameTypeSetting:{custId}";
            var typeResult = await _redisHelper.TryGetOrCreateAsync(key,
                TimeSpan.FromMinutes(10),
                async () =>
                {
                    return await _custInfoService.GetCustTpGameTypeSetting(custId, types.Select(x => (int)x));
                });

            var gameTypeList = typeResult.Select(x => x.GameType).Distinct();
            foreach (var gameType in gameTypeList)
            {
                result.Add(new CustTpGameTypeSetting(
                    typeResult.First(x => x.GameType == gameType).CategoryId,
                    typeResult.First(x => x.GameType == gameType).TpId,
                    gameType,
                    typeResult.Where(x => x.GameType == gameType && x.Type == CustTpGameTypeSettingType.NoShow).Any(),
                    typeResult.Where(x => x.GameType == gameType && x.Type == CustTpGameTypeSettingType.NoPlay).Any()));
            }
            return result;
        }

        public async Task<IEnumerable<CustRecentlyViewedInfo>> GetCustRecentlyViewed(int custId)
        {
            var key = $"GetCustRecentlyViewed:{custId}";
            var result = await _redisHelper.TryGetOrCreateAsync(key,
                TimeSpan.FromMinutes(3),
                async () =>
                {
                    return await _custInfoService.GetCustRecentlyViewed(custId);
                });
            return result;
        }

        public CustVIPProgress GetCustVIPProgress(int custId)
        {
            CustVIPProgress result = new CustVIPProgress();
            var customer = _custInfoService.GetCustInfo(custId);
            result.CurrentVipLevel = customer.UserLevel;
            var vipSystem = _vIPService.GetVipSystem(customer.SiteId).Where(x=> x.CurrencyId == customer.CurrencyId).ToList();

            if (vipSystem != null)
            {
                var upGradeData = vipSystem.Find(x => x.Level == customer.UserLevel + 1);
                var downGradeData = vipSystem.Find(x => x.Level == customer.UserLevel);

                if (upGradeData != null)
                    result.UpDeposit = upGradeData.UpDeposit;
                if (downGradeData != null)
                {
                    result.DownDeposit = downGradeData.DownDeposit;
                    result.DownTurnover = downGradeData.DownTurnover;
                }

                result.ActualDeposit = GetActualDeposit(customer.SiteId, custId);
            }
            return result;
        }
        private decimal GetActualDeposit(int siteId, int custId)
        {
            var currentTime = DateTime.UtcNow.AddHours(8);
            DateTime startTime = new DateTime(currentTime.Year, currentTime.Month, 1)
                .Date;
            DateTime endTime = startTime.AddMonths(1).AddTicks(-1);

            var actualDepositData = _depositService.GetActualDepositData(siteId, custId, startTime, endTime);

            return actualDepositData == null ? 0 : actualDepositData.ActualDeposit;
        }
    }
}