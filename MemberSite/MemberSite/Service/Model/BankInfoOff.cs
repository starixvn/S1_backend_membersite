using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemberSite.Service.Model
{
    public class BankInfoOff
    {
        public int BankId { get; set; }
        public string AccountName { get; set; }
        public string AccountNo { get; set; }
        public int SysId { get; set; }
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
        public int PointLevel { get; set; }
        public string Memo { get; set; }
        public int BSort { get; set; }
        public int CurrencyId { get; set; }
        public string BankCode { get; set; }
        public string BankName { get; set; }
        public string BankLogo { get; set; }
        public string BankSite { get; set; }
        public int Sort { get; set; }
        public int Purpose { get; set; }
        public int Ver { get; set; }
    }
}
