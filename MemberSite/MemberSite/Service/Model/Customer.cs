using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MemberSite.Service.Model
{
    public class Customer
    {
        public int CustId { get; set; }
        public int SiteId { get; set; }
        public string UserName { get; set; }
        public string Userpwd { get; set; }
        public int CurrencyId { get; set; }
        public string UserLang { get; set; }
        public int ActStatus { get; set; }
        public int AgentId { get; set; }
        public string AllAgentId { get; set; }
        public int UserLevel { get; set; }
        public bool IsTest { get; set; }
        public DateTime LastLoginTime { get; set; }
        public DateTime RegTime { get; set; }
        public int Status { get; set; }
    }

    public class CustProfile
    {
        public string RealName { get; set; }
        public string EMail { get; set; }
    }
    //{"Ver": 1, "SysId": 8, "AccountNo": "9876543210", "AccountName": "Eddie Wu"}
    public class CustBankInfo
    {
        public string AccountName { set; get; }
        [JsonIgnore]
        public string BankInfo { set; get; }
        public IEnumerable<BankInfoDetail> bankInfoDetail { set; get; }
    }

    public class BankInfoDetail
    {
        public int SysId { set; get; }
        public string AccountNo { set; get; }
    }

    public class BankInfoDetailRep : BankInfoDetail
    {
        public string BankName { set; get; }
        public string BankLogo { set; get; }
        public string BankSite { set; get; }
    }

    public class CustBankInfoRep
    {
        public string AccountName { set; get; }
        public IEnumerable<BankInfoDetailRep> bankInfoDetail { set; get; }
    }
}
