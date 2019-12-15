using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemberSite.Service.Model
{

    public interface IDepositInfo
    {
        int Ver { get; set; }
    }
    public class DepositDetailInfo
    {
        public int DepositId { get; set; }
        public string TransId { get; set; }
        public int DepositType { get; set; }
        public int CustId { get; set; }
        public string UserName { get; set; }
        public int SiteId { get; set; }
        public string DepositInfo { get; set; }
        public decimal Amount { get; set; }
        public decimal ActualDeposit { get; set; }
        public string Memo { get; set; }
        public string PostScript { get; set; }
        public int Status { get; set; }
    }
    public class DepositInfo1_1 : IDepositInfo
    {
        public int Ver { get; set; }
    }
    public class DepositInfo2_1 : IDepositInfo
    {
        public int Ver { get; set; }
    }
    public class DepositInfo3_1 : IDepositInfo
    {
        public int Ver { get; set; }
        public int SysId { get; set; }
        public string AccountName { get; set; }
        public string AccountNo { get; set; }
    }

    public class DepositRep3
    {
        public string AccountNo { get; set; }
        public string AccountName { get; set; }
        public string transId { get; set; }
        public string postScript { get; set; }
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
    }
}
