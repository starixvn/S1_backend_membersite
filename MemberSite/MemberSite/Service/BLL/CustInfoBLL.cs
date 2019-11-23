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
        private readonly ServerInfoBLL serverInfoBLL;
        private readonly AgentInfoService agentInfoService;

        public CustInfoBLL(Tools tools, CustInfoService custInfoService, AuthenticateService authenticateService, ServerInfoBLL serverInfoBLL, AgentInfoService agentInfoService)
        {
            this.tools = tools;
            this.custInfoService = custInfoService;
            this.authenticateService = authenticateService;
            this.serverInfoBLL = serverInfoBLL;
            this.agentInfoService = agentInfoService;
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
    }
}
