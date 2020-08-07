using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemberSite.Service.Model
{
    public class BonusWallet
    {
        public decimal Bonus { get; set; }
        public decimal Rollover { get; set; }
        public decimal Turnover { get; set; }
        public string CategoryId { get; set; }
    }
}
