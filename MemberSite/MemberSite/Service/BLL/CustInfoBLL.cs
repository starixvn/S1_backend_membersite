using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
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
        public bool IsConvertRequired(int CurrencyId)
        {
            return isConvert.ContainsKey(CurrencyId);
        }
        public int AddCustInfo(string UserName, string Userpwd, string Email, int CurrencyId, string UserLang, string refId, string affId, string aff, string affCode, out int custId, string signupUrl = null, string PhoneNo = null, string AreaCode = null, string RealName=null)
        {
            var serverInfo = serverInfoBLL.GetServerInfo();
            if (string.IsNullOrEmpty(affId) && !serverInfo.ICurrencyList.Contains(CurrencyId))
            {
                custId = 0;
                return - 1;
            }
            else if (!tools.CheckUserName(UserName))
            {
                custId = 0;
                return -2;
            }
            else if (!tools.CheckUserInputPassword(Userpwd))
            {
                custId = 0;
                return -3;
            }
            else if (!serverInfo.ILangList.Contains(UserLang))
            {
                custId = 0;
                return -4;
            }
            else
            {
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
                        return -6;/*agent Currency != CurrencyId*/
                    }
                }

                if (agent == null)
                    agent = agentInfoService.GetSuperAgent(serverInfo.SiteId, CurrencyId);

                Customer regCustInfo = new Customer();
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
                /**/
                var result = custInfoService.AddCustInfo(regCustInfo, Email, out custId, signupUrl, PhoneNo, AreaCode, RealName);
                if (result != 0)
                {
                    return -5;
                }
            }

            return 0;
        }

        private string GetAllAgentByUserId(Customer agent)
        {
            if (agent.AgentId == 0 || agent.AllAgentId == null)
            {
                return agent.AllAgentId = $"[{agent.CustId}]";
            }
            else
            {
                List<int> allAgentId = JsonConvert.DeserializeObject<List<int>>(agent.AllAgentId);
                allAgentId.Add(agent.CustId);
                return JsonConvert.SerializeObject(allAgentId);
            }
        }

        public Customer GetCustInfoByUserName(string userName)
        {
            var serverInfo = serverInfoBLL.GetServerInfo();
            string key = $"CustInfoByUserName.{serverInfo.SiteId}.{userName}";
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
            else if (customer.Userpwd != tools.MD5Encrypt(pwd))
            {
                return -2;
            }
            else if (customer.Status == (int)CustStatus.Suspend || customer.Status == (int)CustStatus.Closed)
            {
                return -3;
            }
            else
            {
                authenticateService.AddLoginRecord(customer.CustId, customer.SiteId, ip, out token);
                authenticateService.AddUserLoginLog(customer.CustId, customer.UserName, customer.SiteId, ip, fromUrl, userAgent);
                return 0;
            }
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
            else if (customer.Status == (int)CustStatus.Suspend || customer.Status == (int)CustStatus.Closed)
            {
                return -3;
            }
            else
            {
                authenticateService.AddLoginRecord(customer.CustId, customer.SiteId, ip, out token);
                authenticateService.AddUserLoginLog(customer.CustId, customer.UserName, customer.SiteId, ip, fromUrl, userAgent);
                return 0;
            }
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
            string key = $"CustInfo_{custId}";
            var customer = cache.Get<Customer>(key);
            if (customer != null)
            {
                return customer;
            }

            customer = custInfoService.GetCustInfo(custId);
            if (customer != null)
            {
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(1));
                cache.Set(key, customer, cacheEntryOptions);
            }

            return customer;
        }

        public Customer GetCustInfo(int custId)
        {
            return custInfoService.GetCustInfo(custId);
        }

        public Customer GetCustInfoByMail(string email)
        {
            var serverInfo = serverInfoBLL.GetServerInfo();
            string key = $"CustInfoByMail.{serverInfo.SiteId}.{email}";
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

        public int GetVipLevel(int custId)
        {
            return custInfoService.GetVipLevel(custId);
        }

        public int GetVipLevelWithCache(int custId)
        {
            string key = $"VipLevelCache_{custId}";
            var vipLevelCache = cache.Get<int?>(key) ?? -1;
            if (vipLevelCache != -1)
            {
                return vipLevelCache;
            }

            vipLevelCache = custInfoService.GetVipLevel(custId);
            if (vipLevelCache != 0)
            {
                cache.Set(key, vipLevelCache, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(30)));
            }

            return vipLevelCache;
        }

        public int SetPassword(int custId, string oldPassword, string newPassword)
        {
            if (!tools.CheckUserInputPassword(newPassword))
            {
                return -2;
            }

            string md5OldPassword = tools.MD5Encrypt(oldPassword);
            string md5NewPassword = tools.MD5Encrypt(newPassword);
            string md5Userpwd = null;


            bool isSet = custInfoService.SetPassword(custId, md5OldPassword, md5NewPassword, out md5Userpwd);
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
            string md5NewPassword = tools.MD5Encrypt(newPassword);
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
        public CustBankInfo GetCustBankInfo(int custId)
        {
            var custInfo = GetCustInfoWithCache(custId);
            var custBankInfo = custInfoService.GetCustBankInfo(custId, custInfo.SiteId);

            if (custBankInfo != null && custBankInfo.BankInfo != null)
            {
                custBankInfo.bankInfoDetail = Newtonsoft.Json.JsonConvert.DeserializeObject<IEnumerable<BankInfoDetail>>(custBankInfo.BankInfo);
            }

            return custBankInfo;
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
            int siteId = 0;
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
            customer.UserLevel = custInfoService.GetCustLevel(custId);
            return customer;
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
                    string NewPassword = codeGenerator.Generate();
                    string md5NewPassword = tools.MD5Encrypt(NewPassword);
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
            ApiResult result = await commonBLL.SendResetPwdMailAsync(custId, resetPasswordUrl);
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

            if(apiResult.ErrorCode== 0)
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
                string _affiliateId = GenerateRandomAffiliateId();
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
            var chars = "abcdefghjkmnopqrstuvwxyz";
            var stringChars = new System.Text.StringBuilder();
            var random = new Random();

            for (int i = 0; i < 8; i++)
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
        public void GetReferralSummary(int custId, out int referralCount, out int amountCount, out int amountSum)
        {
            agentInfoService.GetReferralSummary(custId, out referralCount, out amountCount, out  amountSum);
        }
    }
}
