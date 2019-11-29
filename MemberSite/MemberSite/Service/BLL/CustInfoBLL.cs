using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
        public CustInfoBLL(IMemoryCache cache, Tools tools, CustInfoService custInfoService, AuthenticateService authenticateService, ServerInfoBLL serverInfoBLL, AgentInfoService agentInfoService, RegexUtilities regexUtilities)
        {
            this.tools = tools;
            this.custInfoService = custInfoService;
            this.authenticateService = authenticateService;
            this.serverInfoBLL = serverInfoBLL;
            this.agentInfoService = agentInfoService;
            this.cache = cache;
            this.regexUtilities = regexUtilities;
        }
        public int AddCustInfo(string UserName, string Userpwd, int CurrencyId, string UserLang, out int custId)
        {
            //int custId = 0;
            var serverInfo = serverInfoBLL.GetServerInfo();
            if (!serverInfo.ICurrencyList.Contains(CurrencyId))
            {
                custId = 0;
                return -1;
            }
            else if (!tools.CheckUserName(UserName) || !tools.CheckUserInputPassword(Userpwd))
            {
                custId = 0;
                return -2;
            }
            else if (!serverInfo.ILangList.Contains(UserLang))
            {
                custId = 0;
                return -3;
            }
            else
            {
                Customer agent = agentInfoService.GetSuperAgent(serverInfo.SiteId, CurrencyId);

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

                return custInfoService.AddCustInfo(regCustInfo, out custId);
            }
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

        public int UserLogin(string userName, int siteId, string pwd, string ip, string fromUrl, string userAgent, ref Customer customer, out string token)
        {
            customer = custInfoService.GetCustInfoByUserName(userName, siteId);
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

        public int SetProfile(int custId, Dictionary<string, string> custProfile)
        {
            var custInfo = GetCustInfoWithCache(custId);
            return custInfoService.SetProfile(custId, custInfo.SiteId, custProfile) ? 0 : -1;
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
    }
}
