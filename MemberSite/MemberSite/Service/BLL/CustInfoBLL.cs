using System.Text.RegularExpressions;
using MemberSite.Controllers.Api;
using MemberSite.Service.DAL;
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
        private readonly CustInfoService _custInfoService;
        private readonly AuthenticateService _authenticateService;
        private readonly ServerInfoBll _serverInfoBll;
        private readonly AgentInfoService _agentInfoService;
        private readonly RolloverService _rollOverService;
        private readonly CommonBLL _commonBll;
        private readonly CacheHelper _cacheHelper;
        private readonly SelLangBLL _selLangBll;
        private readonly IovationBLL _iovationBll;
        private readonly Dictionary<int, bool> _isConvert = new()
            {
                {51,false },{15,false }
            };

        private readonly Dictionary<string, string> _areaCodeCountryList;
        private readonly Regex _regexUserName = new(@"^[a-zA-Z0-9]{6,18}$");
        private readonly Regex _regexAffCode = new(@"^[0-9]{5,20}$");

        public CustInfoBll(IServiceProvider provider)
        {
            _tools = provider.GetRequiredService<Tools>();
            _verificationRewardBll = provider.GetRequiredService<VerificationRewardBLL>();
            _custInfoService = provider.GetRequiredService<CustInfoService>();
            _authenticateService = provider.GetRequiredService<AuthenticateService>();
            _serverInfoBll = provider.GetRequiredService<ServerInfoBll>();
            _agentInfoService = provider.GetRequiredService<AgentInfoService>();
            _rollOverService = provider.GetRequiredService<RolloverService>();
            _commonBll = provider.GetRequiredService<CommonBLL>();
            _cacheHelper = provider.GetRequiredService<CacheHelper>();
            _areaCodeCountryList = new CurrencyMapping().AreaCodeMap;
            _selLangBll = provider.GetRequiredService<SelLangBLL>();
            _iovationBll = provider.GetRequiredService<IovationBLL>();
        }
        public string GetCountryNameByAreaCode(string areaCode)
        {
            string countryName = null;
            if (areaCode != null)
                _areaCodeCountryList.TryGetValue(areaCode, out countryName);
            return countryName;
        }
        
        public string GetAreaCodeByCountryName(string countryName)
        {
            var areaCode = _areaCodeCountryList.FirstOrDefault(x => x.Value == countryName).Key;
            return areaCode;
        }
        
        public bool IsConvertRequired(int currencyId)
        {
            return _isConvert.ContainsKey(currencyId);
        }

        public int AddCustInfo(string UserName, string Userpwd, string Email, int CurrencyId, string UserLang,
            string refId, string affId, string aff, string affCode, out int custId, string signupUrl = null,
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

            Customer agent = null;
            if (!string.IsNullOrEmpty(affId))
            {
                agent = _agentInfoService.GetAffiliateAgent(affId, serverInfo.SiteId);

                if (agent == null)
                {
                    custId = 0;
                    return -19;
                }
            }
            else if (!string.IsNullOrEmpty(refId))
            {
                agent = _agentInfoService.GetAffiliateAgent($"{refId}_{CurrencyId}", serverInfo.SiteId);

                if (agent?.CurrencyId != CurrencyId)
                {
                    custId = 0;
                    return -6; /*agent Currency != CurrencyId*/
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
                AffCode = affCode
            };

            if (string.IsNullOrEmpty(signupUrl))
            {
                signupUrl = _tools.GetFromUrl();
            }

            var result = _custInfoService.AddCustInfo(regCustInfo, Email, out custId, signupUrl, referralUrl, PhoneNo, AreaCode,
                RealName, promoCode, Birth_d, Birth_m, Birth_y);
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
        public int UserLogin(string userName, string pwd, string ip, string fromUrl, string userAgent,int deviceType ,ref Customer customer, out string token)
        {
            var serverInfo = _serverInfoBll.GetServerInfo();
            customer = _custInfoService.GetCustInfoByUserName(userName, serverInfo.SiteId);
            token = null;
            if (customer == null)
            {
                return -1;
            }

            if (customer.Userpwd != _tools.MD5Encrypt(pwd))
            {
                return -2;
            }

            if (!VerifyCustomerStatus(customer.Status))
            {
                return -3;
            }

            _authenticateService.AddLoginRecord(customer.CustId, customer.SiteId, ip , deviceType , out token);
            _authenticateService.AddUserLoginLog(customer.CustId, customer.UserName, customer.SiteId, ip, fromUrl, userAgent);
            return 0;
        }

        public int UserLoginForResetPwd(string userName, string ip, string fromUrl, string userAgent,int deviceType, ref Customer customer, out string token)
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

            _authenticateService.AddLoginRecord(customer.CustId, customer.SiteId, ip,deviceType, out token);
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
        public Customer GetCustInfoWithCache(int custId)
        {
            var key = $"CustInfo_{custId}";
            return _cacheHelper.TryGetOrCreate(key,
                new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(1)),
                i => _custInfoService.GetCustInfo(custId));
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
            var key = $"PointLevelCache_{custId}";
            return _cacheHelper.TryGetOrCreate(key,
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(30)),
                i => _custInfoService.GetPointLevel(custId));
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

        public int SetCustBankInfo(int custId, string accountName, IEnumerable<BankInfoDetail> bankInfo)
        {
            var custInfo = GetCustInfoWithCache(custId);
            return _custInfoService.SetCustBankInfo(custId, custInfo.SiteId, accountName, bankInfo) ? 0 : -1;
        }
        public int InsertCustBankInfo(int custId, string accountName, IEnumerable<BankInfoDetail> bankInfo, IEnumerable<BankInfoDetail> banklog)
        {
            var custInfo = GetCustInfoWithCache(custId);
            return _custInfoService.InsertCustBankInfo(custId, custInfo.SiteId, accountName, bankInfo, banklog) ? 0 : -1;
        }

        public int SetCustUSDTInfo(int custId, string accountName, IEnumerable<USDTDetail> uSDTInfo)
        {
            var custInfo = GetCustInfoWithCache(custId);
            return _custInfoService.SetCustUSDTInfo(custId, custInfo.SiteId, accountName, uSDTInfo) ? 0 : -1;
        }
        public int InsertCustUSDTInfo(int custId, string accountName, IEnumerable<USDTDetail> uSDTInfo ,IEnumerable<USDTDetail> uSDTInfoLog)
        {
            var custInfo = GetCustInfoWithCache(custId);
            return _custInfoService.InsertCustUSDTInfo(custId, custInfo.SiteId, accountName, uSDTInfo ,uSDTInfoLog) ? 0 : -1;
        }
        public CustBankInfo GetCustBankInfo(int custId)
        {
            var custInfo = GetCustInfoWithCache(custId);
            var custBankInfo = _custInfoService.GetCustBankInfo(custId, custInfo.SiteId);

            if (custBankInfo != null && custBankInfo.BankInfo != null)
            {
                var bankInfoDetailList = JsonConvert.DeserializeObject<IEnumerable<BankInfoDetail>>(custBankInfo.BankInfo);
                if (bankInfoDetailList != null)
                {
                    bankInfoDetailList = bankInfoDetailList.Where(x => x.SysId != 0 && x.AccountNo != null);//filiter null data
                    if (bankInfoDetailList.Any())
                    {
                        custBankInfo.bankInfoDetail = bankInfoDetailList;
                    }
                }
                //if (custInfo.CurrencyId == 96)
                //{
                //    custBankInfo.uSDTDetail = Newtonsoft.Json.JsonConvert.DeserializeObject<IEnumerable<USDTDetail>>(custBankInfo.BankInfo);
                //}
                //else
                //{
                //    custBankInfo.bankInfoDetail = Newtonsoft.Json.JsonConvert.DeserializeObject<IEnumerable<BankInfoDetail>>(custBankInfo.BankInfo);
                //}

            }
            if (custBankInfo != null && custBankInfo.USDTInfo != null)
            {
                var uSDTDetailList = JsonConvert.DeserializeObject<IEnumerable<USDTDetail>>(custBankInfo.USDTInfo);
                if (uSDTDetailList != null)
                {
                    uSDTDetailList = uSDTDetailList.Where(x => x.USDTAddress != null);//filiter null data
                    if (uSDTDetailList.Any())
                    {
                        custBankInfo.usdtDetail = uSDTDetailList;
                    }
                }
                //if (custInfo.CurrencyId == 96)
                //{
                //    custBankInfo.uSDTDetail = Newtonsoft.Json.JsonConvert.DeserializeObject<IEnumerable<USDTDetail>>(custBankInfo.BankInfo);
                //}
                //else
                //{
                //    custBankInfo.bankInfoDetail = Newtonsoft.Json.JsonConvert.DeserializeObject<IEnumerable<BankInfoDetail>>(custBankInfo.BankInfo);
                //}

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
            var custInfo = GetCustInfoWithCache(custId);
            var customer = _custInfoService.GetCustInfoProfile(custId, custInfo.SiteId);
            //customer.UserLevel = custInfoService.GetCustLevel(custId);
            return customer;
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
        public int SetCustPhotoId(int custId, int siteId, string picFileName, byte[] photoIdPic)
        {
            return _custInfoService.SetCustPhotoId(custId, siteId, picFileName, photoIdPic);
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
        public async Task<ApiResult> SendVerifySMSAsync(int custId)
        {
            var custInfo = GetCustInfoWithCache(custId);
            var apiResult = await _commonBll.SendVerifySMSAsync(custInfo);
            return apiResult;
        }
        public async Task<ApiResult> AcknowledgeVerifySMS(int custId, string siteName, string request_id, string code)
        {
            var custInfo = GetCustInfoWithCache(custId);
            var apiResult = await _commonBll.AcknowledgeVerifySMS(custInfo, siteName, request_id, code);

            if (apiResult.ErrorCode == 0)
            {
                _custInfoService.SetPhoneVerified(custId);
            }

            return apiResult;
        }
        public CustRollOver GetCustRollOver(int custId, string bonusCode)
        {
            return _rollOverService.GetCustRollOver(custId, bonusCode);
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
                        affiliateId = _agentInfoService.GetAffiliateId(custId, custInfo.SiteId);
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
                else
                {
                    return agent.CurrencyId;
                }
            }
            else
            {
                return -1;
            }

        }
        public void GetReferralSummary(int custId, out int referralCount, out int conditionCount, out int amountCount, out int amountSum)
        {
            _agentInfoService.GetReferralSummary(custId, out referralCount, out conditionCount, out amountCount, out amountSum);
        }
        public IEnumerable<ReferralFriend> GetRefFriendList(int custId, long? StartDate, long? EndDate, int PageSize, ref int PageNumber, ref int TotalPages, ref int RowCount)
        {
            return _agentInfoService.GetRefFriendList(custId, StartDate, EndDate, PageSize, ref PageNumber, ref TotalPages, ref RowCount);
        }

        private static bool VerifyCustomerStatus(int status)
        {
            if (status == (int) CustStatus.Suspend || status == (int) CustStatus.Closed ||
                status == (int) CustStatus.Lock)
            {
                {
                    return false;
                }
            }

            return true;
        }
        public int SetFireBaseTroken(int custId, int siteId, string fireBaseToken)
        {
            return _custInfoService.SetFireBaseTroken( custId, siteId, fireBaseToken);
        }
        public int SetCustActStatus(int custId, string Operator)
        {
            return _custInfoService.SetCustActStatus(custId, Operator);
        }
        
        public Customer Signup(SiteController.SignupModel signupModel, ApiResult apiResult, bool isAuthenticated, string host, string userAgent)
        {
            Customer customer = null;
            if (isAuthenticated ||
                (string.IsNullOrEmpty(signupModel.AreaCode) && !string.IsNullOrEmpty(signupModel.PhoneNo)))
            {
                apiResult.ErrorCode = 9000;
                return customer;
            }

            if (!_regexUserName.IsMatch(signupModel.UserName))
            {
                apiResult.ErrorCode = -8;
                apiResult.Message = _selLangBll.GetMsg("msg_UsernameValidate");
                return customer;
            }

            if (!string.IsNullOrEmpty(signupModel.AffCode?.Trim()))
            {
                signupModel.AffCode = signupModel.AffCode.Trim();
                if (!_regexAffCode.IsMatch(signupModel.AffCode))
                {
                    apiResult.ErrorCode = -9;
                    apiResult.Message = _selLangBll.GetMsg("msg_AffCodeValidate");
                    return customer;
                }
            }

            if (string.IsNullOrEmpty(signupModel.SignupUrl))
            {
                signupModel.SignupUrl = host;
            }

            // URL tag with specific affcode
            if (host is "www.nova88fun.com" or "m.nova88fun.com")
            {
                signupModel.AffCode = "nova88tvads";
            }

            var custId = 0;
            var result = -99;
            var phoneExists = false;
            var mailExists = false;

            if (long.TryParse(signupModel.PhoneNo, out var phoneNo))
            {
                signupModel.PhoneNo = phoneNo.ToString();
                phoneExists = ExistsPhoneNo(0, signupModel.AreaCode, signupModel.PhoneNo);
                if (phoneExists)
                    result = -6;
            }
            else if (!string.IsNullOrEmpty(signupModel.PhoneNo))
            {
                apiResult.ErrorCode = -20;
                apiResult.Message = $"{_selLangBll.GetResourceValue("lbl_Phone")} {_selLangBll.GetMsg("msg_PhoneWrongFormat")}";
                return customer;
            }

            if (!string.IsNullOrWhiteSpace(signupModel.Email))
            {
                mailExists = ExistsEmail(0, signupModel.Email);
                if (mailExists)
                    result = -10;
            }

            if (!phoneExists && !mailExists)
            {
                result = AddCustInfo(signupModel.UserName, signupModel.Userpwd, signupModel.Email,
                    signupModel.CurrencyId, signupModel.UserLang, signupModel.RefId, signupModel.AffId, signupModel.Aff,
                    signupModel.AffCode, out custId, signupModel.SignupUrl, signupModel.ReferralUrl, signupModel.PhoneNo,
                    signupModel.AreaCode, signupModel.RealName, signupModel.PromoCode, signupModel.Birth_d, signupModel.Birth_m,
                    signupModel.Birth_y);
            }

            switch (result)
            {
                case -1:
                    apiResult.ErrorCode = result;
                    apiResult.Message = _selLangBll.GetMsg("err_ThisCurrencyNotSupport");
                    break;
                case -2:
                    apiResult.ErrorCode = result;
                    apiResult.Message = _selLangBll.GetMsg("msg_UsernameValidate");
                    break;
                case -3:
                    apiResult.ErrorCode = result;
                    apiResult.Message = _selLangBll.GetMsg("msg_PasswordValidate");
                    break;
                case -4:
                    apiResult.ErrorCode = result; /**/
                    apiResult.Message = "";
                    break;
                case -5:
                    apiResult.ErrorCode = result;
                    apiResult.Message = _selLangBll.GetMsg("msg_UsernameNotAvailable");
                    break;
                case -6:
                    apiResult.ErrorCode = result;
                    apiResult.Message = $"{_selLangBll.GetResourceValue("lbl_Phone")} {_selLangBll.GetMsg("msg_HasExist")}";
                    break;
                case -7:
                    apiResult.ErrorCode = result;
                    apiResult.Message = _selLangBll.GetMsg("msg_PromoCodeIncorrect");
                    break;
                case -10:
                    apiResult.ErrorCode = result;
                    apiResult.Message = $"{_selLangBll.GetResourceValue("lbl_Email")} {_selLangBll.GetMsg("msg_HasExist")}";
                    break;
                case -19:
                    apiResult.ErrorCode = result;
                    apiResult.Message = _selLangBll.GetMsg("mag_AffiliateNotExist");
                    break;
                default:
                {
                    if (custId == 0)
                    {
                        apiResult.ErrorCode = result;
                        apiResult.Message = _selLangBll.GetMsg("msg_UsernameNotAvailable");
                    }
                    else if (result != 0)
                    {
                        apiResult.ErrorCode = result;
                        apiResult.Message = _selLangBll.GetMsg("msg_UsernameNotAvailable");
                    }
                    else
                    {
                        _ = Task.Run(async () =>
                        {
                            _commonBll.SendWelcomeMail(custId);

                            if (signupModel.blackbox != null)
                            {
                                var apiResultIovation = await _iovationBll.IovationChecks(custId, signupModel.CurrencyId,
                                    signupModel.UserName, signupModel.blackbox, IovationType.registration);
                                if (apiResultIovation.ErrorCode == 0)
                                    _iovationBll.SetIovation(custId, apiResultIovation.Message, IovationType.registration);
                            }
                        });

                        var fromUrl = _tools.GetFromUrl();
                        var deviceType = (int)_tools.GetDeviceId(fromUrl);
                        var userLoginResult = UserLogin(signupModel.UserName, signupModel.Userpwd,
                            _tools.GetClientIP(), fromUrl, userAgent, deviceType, ref customer,
                            out var token);

                        switch (userLoginResult)
                        {
                            case -1:
                                apiResult.ErrorCode = 1101;
                                apiResult.Message = _selLangBll.GetMsg("msg_DepositProcessLogin_LoginFailed");
                                break;
                            case -2:
                                apiResult.ErrorCode = 1001;
                                apiResult.Message = _selLangBll.GetMsg("msg_DepositProcessLogin_LoginFailed");
                                break;
                            case -3:
                                apiResult.ErrorCode = 1102;
                                apiResult.Message = _selLangBll.GetMsg("msg_DepositProcessLogin_ACLocked");
                                break;
                            default:
                            {
                                if (userLoginResult != 0)
                                {
                                    apiResult.ErrorCode = 9000;
                                }
                                else
                                {
                                    if (!string.IsNullOrEmpty(signupModel.RealName))
                                    {
                                        SetCustBankInfo(custId, signupModel.RealName, null);
                                    }

                                    customer.Token = token;
                                    
                                    apiResult.ErrorCode = 0;
                                    return customer;
                                }

                                break;
                            }
                        }
                    }

                    break;
                }
            }

            return customer;
        }
    }
}
