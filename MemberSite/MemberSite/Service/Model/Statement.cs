using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemberSite.Service.Model
{
    public class Statement
    {
        public string Type { get; set; }
        public decimal ChangeAmount { get; set; }
        public decimal BeforeAmount { get; set; }
        public decimal AfterAmount { get; set; }
        public string RefTransId { get; set; }
        public DateTime CreateTime { get; set; }
        public int Direction { get; set; }

        public string TypeName { get; set; }
    }

    public class BounsLogStatement
    {
        public string Type { get; set; }
        public decimal ChangeBonus { get; set; }
        public decimal BeforeBonus { get; set; }
        public decimal AfterBonus { get; set; }
        public string RefTransId { get; set; }
        public DateTime CreateTime { get; set; }
        public int Direction { get; set; }

        public string TypeName { get; set; }
    }
    public class TpList
    {
        public int Id { get; set; }
        public string TpName { get; set; }
    }
    public class PromotionCategory
    {
        public int id { get; set; }
        public string Name { get; set; }
    }
}
