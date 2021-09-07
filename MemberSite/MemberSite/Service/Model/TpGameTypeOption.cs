using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemberSite.Service.Model
{
    public class TpGameTypeOption
    {
        public int TpId { get; set; }
        public int TpGameType { get; set; }
        public int TpCategoryId { get; set; }
        public int CurrencyId { get; set; }
    }
}
