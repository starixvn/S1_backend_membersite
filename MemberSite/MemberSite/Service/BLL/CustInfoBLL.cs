using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MemberSite.Service.BLL
{
    public class CustInfoBLL
    {
        private readonly Tools tools;
        private readonly CustInfoService custInfoService;
        private readonly AuthenticateService authenticateService;
        private readonly ServerInfoBLL serverInfoBLL;
        private readonly AgentInfoService agentInfoService;
        private readonly IMemoryCache cache;
        private readonly RegexUtilities regexUtilities;
        private readonly Common common;
        private readonly IWebHostEnvironment environment;
        private readonly CodeGenerator codeGenerator;
        private readonly SelLangBLL selLangBLL;
        private readonly RolloverService rollOverService;
        private readonly CommonBLL commonBLL;
        private readonly Dictionary<int, bool> isConvert = new Dictionary<int, bool>()
            {
                {51,false },{15,false }
            };
        private readonly Dictionary<string, string> AreacodeCountryList = new Dictionary<string, string>()
            {
                {"+60","MY" },
                {"+66","TH" },
                {"+86","CS" },
                {"+62","ID" },
                {"+91","IN" },
                {"+84","VN" }
            };

        public CustInfoBLL(IMemoryCache cache, IWebHostEnvironment environment, SelLangBLL selLangBLL, CodeGenerator codeGenerator, Tools tools, Common common, CustInfoService custInfoService, AuthenticateService authenticateService, ServerInfoBLL serverInfoBLL, RolloverService rollOverService, AgentInfoService agentInfoService, RegexUtilities regexUtilities, CommonBLL commonBLL)
        {
            this.tools = tools;
            this.custInfoService = custInfoService;
            this.authenticateService = authenticateService;
            this.serverInfoBLL = serverInfoBLL;
            this.agentInfoService = agentInfoService;
            this.cache = cache;
            this.regexUtilities = regexUtilities;
            this.common = common;
            this.environment = environment;
            this.codeGenerator = codeGenerator;
            this.selLangBLL = selLangBLL;
            this.rollOverService = rollOverService;
            this.commonBLL = commonBLL;
        }
        public string GetCountryNameByAreaCode(string areaCode)
        {
            string CountryName = null;
            if(areaCode != null)
                AreacodeCountryList.TryGetValue(areaCode, out  CountryName);
            return CountryName;
        }
        public bool IsConvertRequired(int CurrencyId)
        {
            return isConvert.ContainsKey(CurrencyId);
        }

        public int AddCustInfo(string UserName, string Userpwd, string Email, int CurrencyId, string UserLang,
            string refId, string affId, string aff, string affCode, int siteId, out int custId, string signupUrl = null,
            string PhoneNo = null, string AreaCode = null, string RealName = null, string promoCode = null,
            int Birth_d = 0, int Birth_m = 0, int Birth_y = 0)
        {
            var serverInfo = serverInfoBLL.GetServerInfo();
            if (string.IsNullOrEmpty(affId) && !serverInfo.ICurrencyList.Contains(CurrencyId))
            {
                custId = 0;
                return -1;
            }

            if (!tools.CheckUserName(UserName))
            {
                custId = 0;
                return -2;
            }

            if (!tools.CheckUserInputPassword(Userpwd, siteId))
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
                agent = agentInfoService.GetAffiliateAgent(affId, serverInfo.SiteId);

                if (agent == null)
                {
                    custId = 0;
                    return -19;
                }
            }
            else if (!string.IsNullOrEmpty(refId))
            {
                agent = agentInfoService.GetAffiliateAgent($"{refId}_{CurrencyId}", serverInfo.SiteId);

                if (agent?.CurrencyId != CurrencyId)
                {
                    custId = 0;
                    return -6; /*agent Currency != CurrencyId*/
                }
            }

            agent ??= agentInfoService.GetSuperAgent(serverInfo.SiteId, CurrencyId);

            var regCustInfo = new Customer();
            regCustInfo.UserName = UserName;
            regCustInfo.Userpwd = tools.MD5Encrypt(Userpwd);
            regCustInfo.AgentId = agent.CustId;
            regCustInfo.AllAgentId = GetAllAgentByUserId(agent);
            regCustInfo.ActStatus = (int)CustActStatus.NonDepoist;
            regCustInfo.CurrencyId = agent.CurrencyId;
            regCustInfo.IsTest = agent.IsTest;
            regCustInfo.SiteId = serverInfo.SiteId;
            regCustInfo.UserLevel = 1;
            regCustInfo.UserLang = UserLang;
            regCustInfo.Aff = aff;
            regCustInfo.AffCode = affCode;

            if (string.IsNullOrEmpty(signupUrl))
            {
                signupUrl = tools.GetFromUrl();
            }

            var result = custInfoService.AddCustInfo(regCustInfo, Email, out custId, signupUrl, PhoneNo, AreaCode,
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
            var serverInfo = serverInfoBLL.GetServerInfo();
            var key = $"CustInfoByUserName_{serverInfo.SiteId}_{userName}";
            var customer = cache.Get<Customer>(key);
            if (customer != null)
            {
                return customer;
            }

            customer = custInfoService.GetCustInfoByUserName(userName, serverInfo.SiteId);

            if (customer != null)
            {
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(1));
                cache.Set(key, customer, cacheEntryOptions);
            }

            return customer;
        }
        public int UserLogin(string userName, string pwd, string ip, string fromUrl, string userAgent, ref Customer customer, out string token)
        {
            var serverInfo = serverInfoBLL.GetServerInfo();
            customer = custInfoService.GetCustInfoByUserName(userName, serverInfo.SiteId);
            token = null;
            if (customer == null)
            {
                return -1;
            }

            if (customer.Userpwd != tools.MD5Encrypt(pwd))
            {
                return -2;
            }

            if (!VerifyCustomerStatus(customer.Status))
            {
                return -3;
            }

            authenticateService.AddLoginRecord(customer.CustId, customer.SiteId, ip, out token);
            authenticateService.AddUserLoginLog(customer.CustId, customer.UserName, customer.SiteId, ip, fromUrl, userAgent);
            return 0;
        }

        public int UserLoginForResetPwd(string userName, string ip, string fromUrl, string userAgent, ref Customer customer, out string token)
        {
            var serverInfo = serverInfoBLL.GetServerInfo();
            customer = custInfoService.GetCustInfoByUserName(userName, serverInfo.SiteId);
            token = null;
            if (customer == null)
            {
                return -1;
            }

            if (!VerifyCustomerStatus(customer.Status))
            {
                return -3;
            }

            authenticateService.AddLoginRecord(customer.CustId, customer.SiteId, ip, out token);
            authenticateService.AddUserLoginLog(customer.CustId, customer.UserName, customer.SiteId, ip, fromUrl, userAgent);
            return 0;
        }

        public int SetLanguage(int custId, string langId)
        {
            var serverInfo = serverInfoBLL.GetServerInfo();
            if (!serverInfo.ILangList.Contains(langId))
            {
                return -1;
            }

            custInfoService.SetLanguage(custId, langId);

            return 0;
        }
        public Customer GetCustInfoWithCache(int custId)
        {
            var key = $"CustInfo_{custId}";

            if (!cache.TryGetValue(key, out Customer customer))
            {
                customer = custInfoService.GetCustInfo(custId);
                var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(1));
                cache.Set(key, customer, cacheEntryOptions);
            }

            return customer;
        }

        public Customer GetCustInfo(int custId)
        {
            return custInfoService.GetCustInfo(custId);
        }

        public CustTransSummary GetCustTransSummary(int custId)
        {
            return custInfoService.GetCustTransSummary(custId);
        }

        public Customer GetCustInfoByMail(string email)
        {
            var serverInfo = serverInfoBLL.GetServerInfo();
            var key = $"CustInfoByMai_{serverInfo.SiteId}_{email}";
            var customer = cache.Get<Customer>(key);
            if (customer != null)
            {
                return customer;
            }

            customer = custInfoService.GetCustInfoByMail(email, serverInfo.SiteId);

            if (customer != null)
            {
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(1));
                cache.Set(key, customer, cacheEntryOptions);
            }

            return customer;
        }

        public int GetPointLevel(int custId)
        {
            return custInfoService.GetPointLevel(custId);
        }

        public int GetPointLevelWithCache(int custId)
        {
            var key = $"PointLevelCache_{custId}";
            if (!cache.TryGetValue(key, out int pointLevelCache))
            {
                pointLevelCache = custInfoService.GetPointLevel(custId);
                if (pointLevelCache != 0)
                {
                    cache.Set(key, pointLevelCache,
                        new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(30)));
                }
            }

            return pointLevelCache;
        }

        public int SetPassword(int custId, string oldPassword, string newPassword, int siteId)
        {
            if (!tools.CheckUserInputPassword(newPassword, siteId))
            {
                return -2;
            }

            var md5OldPassword = tools.MD5Encrypt(oldPassword);
            var md5NewPassword = tools.MD5Encrypt(newPassword);
            string md5Userpwd = null;


            var isSet = custInfoService.SetPassword(custId, md5OldPassword, md5NewPassword, out md5Userpwd);
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
            var md5NewPassword = tools.MD5Encrypt(newPassword);
            return custInfoService.SetResetPwd(custId, md5NewPassword);
        }
        public int SetProfile(int custId, Dictionary<string, string> custProfile)
        {
            var custInfo = GetCustInfoWithCache(custId);
            return custInfoService.SetProfile(custId, custInfo.SiteId, custProfile) ? 0 : -1;
        }
        public int SetProfileV2(int custId, Dictionary<string, string> custProfile)
        {
            var custInfo = GetCustInfoWithCache(custId);
            return custInfoService.SetProfileV2(custId, custInfo.SiteId, custProfile) ? 0 : -1;
        }

        public int SetCustBankInfo(int custId, string accountName, IEnumerable<BankInfoDetail> bankInfo)
        {
            var custInfo = GetCustInfoWithCache(custId);
            return custInfoService.SetCustBankInfo(custId, custInfo.SiteId, accountName, bankInfo) ? 0 : -1;
        }
        public int InsertCustBankInfo(int custId, string accountName, IEnumerable<BankInfoDetail> bankInfo, IEnumerable<BankInfoDetail> banklog)
        {
            var custInfo = GetCustInfoWithCache(custId);
            return custInfoService.InsertCustBankInfo(custId, custInfo.SiteId, accountName, bankInfo, banklog) ? 0 : -1;
        }
        public int SetCustUSDTInfo(int custId, string accountName, IEnumerable<USDTDetail> uSDTInfo)
        {
            var custInfo = GetCustInfoWithCache(custId);
            return custInfoService.SetCustUSDTInfo(custId, custInfo.SiteId, accountName, uSDTInfo) ? 0 : -1;
        }
        public CustBankInfo GetCustBankInfo(int custId)
        {
            var custInfo = GetCustInfoWithCache(custId);
            var custBankInfo = custInfoService.GetCustBankInfo(custId, custInfo.SiteId);

            if (custBankInfo != null && custBankInfo.BankInfo != null)
            {
                var bankInfoDetailList = Newtonsoft.Json.JsonConvert.DeserializeObject<IEnumerable<BankInfoDetail>>(custBankInfo.BankInfo);
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
                var uSDTDetailList = Newtonsoft.Json.JsonConvert.DeserializeObject<IEnumerable<USDTDetail>>(custBankInfo.USDTInfo);
                if (uSDTDetailList != null)
                {
                    uSDTDetailList = uSDTDetailList.Where(x => x.USDTAddress != null);//filiter null data
                    if (uSDTDetailList.Any())
                    {
                        custBankInfo.uSDTDetail = uSDTDetailList;
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
            return custInfoService.GetDefAccountNo(custId, custInfo.SiteId);
        }
        public bool ExistsUserName(string userName)
        {
            var serverInfo = serverInfoBLL.GetServerInfo();
            return custInfoService.ExistsUserName(userName, serverInfo.SiteId);
        }

        public bool ExistsPhoneNo(int custId, string AreaCode, string PhoneNo)
        {
            var serverInfo = serverInfoBLL.GetServerInfo();
            return custInfoService.ExistsPhoneNo(custId, serverInfo.SiteId, AreaCode, PhoneNo);
        }
        public bool ExistsEmail(int custId, string email)
        {
            var siteId = 0;
            if (custId == 0)
            {
                var serverInfo = serverInfoBLL.GetServerInfo();
                siteId = serverInfo.SiteId;
            }
            else
            {
                var custInfo = GetCustInfoWithCache(custId);
                siteId = custInfo.SiteId;
            }

            return custInfoService.ExistsEmail(custId, email, siteId);
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
            var customer = custInfoService.GetCustInfoProfile(custId, custInfo.SiteId);
            //customer.UserLevel = custInfoService.GetCustLevel(custId);
            return customer;
        }
        public CustInfoProfile GetCustInfoProfileWithCache(int custId)
        {
            var key = $"CustInfoProfile_{custId}";
            var custInfoProfile = cache.Get<CustInfoProfile>(key);
            if (custInfoProfile != null)
            {
                return custInfoProfile;
            }

            custInfoProfile = GetCustInfoProfile(custId);
            if (custInfoProfile != null)
            {
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(1));
                cache.Set(key, custInfoProfile, cacheEntryOptions);
            }

            return custInfoProfile;
        }
        public int SubmittedCustPhotoId(int custId)
        {
            var custInfo = GetCustInfoWithCache(custId);
            return custInfoService.SubmittedCustPhotoId(custId, custInfo.SiteId, custInfo.CurrencyId);
        }
        public int SetCustPhotoId(int custId, int siteId, string picFileName, byte[] photoIdPic)
        {
            return custInfoService.SetCustPhotoId(custId, siteId, picFileName, photoIdPic);
        }
        public async Task ForgetPwdAsync(int custId)
        {
            var custInfo = GetCustInfoWithCache(custId);
            var serverInfo = serverInfoBLL.GetServerInfo();
            var custInfoProfile = custInfoService.GetCustInfoProfile(custId, custInfo.SiteId);

            if (custInfoProfile?.Email != null)
            {
                var path = $@"{environment.ContentRootPath}\App_Data\{serverInfo.SiteName}\email\{custInfo.UserLang}\new_password.html";
                string MailBody = null;
                if (File.Exists(path))
                {
                    MailBody = File.ReadAllText(path);
                    MailBody = MailBody.Replace("{SiteName}", serverInfo.SiteName);
                    MailBody = MailBody.Replace("{UserName}", custInfo.UserName);
                    var SendDate = new DateTimeOffset(DateTime.Now).ToOffset(TimeSpan.FromHours(8)).ToString("yyyy-MM-dd hh:mm:sstt", System.Globalization.CultureInfo.InvariantCulture);
                    MailBody = MailBody.Replace("{SendDate}", SendDate);
                    codeGenerator.Maximum = 8;
                    codeGenerator.Minimum = 6;
                    codeGenerator.RepeatCharacters = true;
                    var NewPassword = codeGenerator.Generate();
                    var md5NewPassword = tools.MD5Encrypt(NewPassword);
                    custInfoService.SetResetPwd(custId, md5NewPassword);
                    MailBody = MailBody.Replace("{NewPassword}", NewPassword);
                }

                await common.SendMailAsync(serverInfo.SiteName, null, custInfo.UserName, custInfoProfile.Email, selLangBLL.GetResourceValue("lbl_ForgetPassword"), MailBody);
            }
        }
        public void ForgetPwd1(int custId, string resetPasswordUrl)
        {
            commonBLL.SendResetPwdMail(custId, resetPasswordUrl);
        }

        public async Task<ApiResult> ForgetPwd1Async(int custId, string resetPasswordUrl)
        {
            var result = await commonBLL.SendResetPwdMailAsync(custId, resetPasswordUrl);
            return result;
        }
        public async Task<ApiResult> SendVerifySMSAsync(int custId)
        {
            var custInfo = GetCustInfoWithCache(custId);
            var apiResult = await commonBLL.SendVerifySMSAsync(custInfo);
            return apiResult;
        }
        public async Task<ApiResult> AcknowledgeVerifySMS(int custId, string siteName, string request_id, string code)
        {
            var custInfo = GetCustInfoWithCache(custId);
            var apiResult = await commonBLL.AcknowledgeVerifySMS(custInfo, siteName, request_id, code);

            if (apiResult.ErrorCode == 0)
            {
                custInfoService.SetPhoneVerified(custId);
            }

            return apiResult;
        }
        public CustRollOver GetCustRollOver(int custId, string bonusCode)
        {
            return rollOverService.GetCustRollOver(custId, bonusCode);
        }
        public async Task<ApiResult> VerifyOTPAsync(int custId, string inputOTP)
        {
            var apiResult = await commonBLL.VerifyOTPAsync(custId, inputOTP);

            if (apiResult.ErrorCode == 0)
            {
                custInfoService.SetPhoneVerified(custId);
            }
            return apiResult;
        }
        public string GetAffiliateId(int custId)
        {
            var custInfo = GetCustInfoWithCache(custId);
            var affiliateId = agentInfoService.GetAffiliateId(custId, custInfo.SiteId);
            while (affiliateId == null)
            {
                var _affiliateId = GenerateRandomAffiliateId();
                try
                {
                    agentInfoService.AddAffiliateId(_affiliateId, custId, custInfo.SiteId);
                    affiliateId = _affiliateId;
                }
                catch (MySql.Data.MySqlClient.MySqlException mySqlEx)
                {
                    if (mySqlEx.Number == 1062)/*Duplicate for key 'PRIMARY'*/
                    {
                        affiliateId = agentInfoService.GetAffiliateId(custId, custInfo.SiteId);
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
            return agentInfoService.GetReferralSetting(siteId, currencyId);
        }
        public int GetAffiliateAgentBLL(string affId)
        {
            var serverInfo = serverInfoBLL.GetServerInfo();
            if (!string.IsNullOrEmpty(affId))
            {
                var agent = agentInfoService.GetAffiliateAgent(affId, serverInfo.SiteId);

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
            agentInfoService.GetReferralSummary(custId, out referralCount, out conditionCount, out amountCount, out amountSum);
        }
        public IEnumerable<ReferralFriend> GetRefFriendList(int custId, long? StartDate, long? EndDate, int PageSize, ref int PageNumber, ref int TotalPages, ref int RowCount)
        {
            return agentInfoService.GetRefFriendList(custId, StartDate, EndDate, PageSize, ref PageNumber, ref TotalPages, ref RowCount);
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
            return custInfoService.SetFireBaseTroken( custId, siteId, fireBaseToken);
        }
    }
}
