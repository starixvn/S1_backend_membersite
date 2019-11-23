using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
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

        public CustInfoBLL(Tools tools, CustInfoService custInfoService, AuthenticateService authenticateService)
        {
            this.tools = tools;
            this.custInfoService = custInfoService;
            this.authenticateService = authenticateService;
        }
        public int AddCustInfo(Customer customer, Customer agent, ServerInfo serverInfo, out int custId)
        {
            //int custId = 0;
            if (!serverInfo.ICurrencyList.Contains(customer.CurrencyId))
            {
                custId = 0;
                return -1;
            }
            else if (!tools.CheckUserName(customer.UserName) || !tools.CheckUserInputPassword(customer.Userpwd))
            {
                custId = 0;
                return -2;
            }
            else if (!serverInfo.ILangList.Contains(customer.UserLang))
            {
                custId = 0;
                return -3;
            }
            else
            {
                Customer regCustInfo = new Customer();
                regCustInfo.UserName = customer.UserName;
                regCustInfo.Userpwd = tools.MD5Encrypt(customer.Userpwd);
                regCustInfo.AgentId = agent.CustId;
                regCustInfo.AllAgentId = GetAllAgentByUserId(agent);
                regCustInfo.ActStatus = (int)CustActStatus.NonDepoist;
                regCustInfo.CurrencyId = agent.CurrencyId;
                regCustInfo.IsTest = agent.IsTest;
                regCustInfo.SiteId = serverInfo.SiteId;
                regCustInfo.UserLevel = 1;
                regCustInfo.UserLang = customer.UserLang;

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
    }
}
