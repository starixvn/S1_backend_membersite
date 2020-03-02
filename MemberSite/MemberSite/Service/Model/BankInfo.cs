using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemberSite.Service.Model
{
    public class BankInfoSys
    {
        public int SysId { set; get; }
        public int CurrencyId { set; get; }
        public string BankCode { set; get; }
        public string BankName { set; get; }
        public string BankLogo { set; get; }
        public string BankSite { set; get; }
    }

    public class PaymentFront
    {
        public int PayId { get; set; }
        public int ChannelId { get; set; }
        public string PayName { get; set; }
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
    }

    public class SysBankInfoFront
    {
        public int SysId { get; set; }
        public string BankCode { get; set; }
        public string BankName { get; set; }
        public string BankLogo { get; set; }
        public string BankSite { get; set; }
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
    }
}
