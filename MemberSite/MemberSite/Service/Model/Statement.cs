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
    public class TpBetRecordListOneWeek
    {
        public DateTime SettledDate { get; set; }
        public decimal TurnOver { get; set; }
        public decimal ValidBet { get; set; }
        public decimal WinlostAmount { get; set; }
    }
    public class TpBetRecordListOneDay
    {
        public DateTime SettledDate { get; set; }
        public string CategoryName { get; set; }
        public int CategoryId { get; set; }
        public decimal TurnOver { get; set; }
        public decimal ValidBet { get; set; }
        public decimal WinlostAmount { get; set; }
    }
    public class TpBetRecordbyCategoryId
    {
        public DateTime SettledDate { get; set; }
        public string GameTypeName { get; set; }
        public int GameTypeID { get; set; }
        public decimal TurnOver { get; set; }
        public decimal ValidBet { get; set; }
        public decimal WinlostAmount { get; set; }
    }
    public class TpBetRecordDetailbyGameType
    {
        public string TransId { get; set; }
        public int BonusId { get; set; }
        public string BonusCode { set; get; }
        public decimal TurnOver { get; set; }
        public decimal ValidBet { get; set; }
        public decimal WinlostAmount { get; set; }
        public DateTime TransTime { get; set; }
        public DateTime SettledTime { get; set; }
    }
    public class TpBetRecordListDateRange
    {
        public DateTime SettledDate { get; set; }
        public decimal TurnOver { get; set; }
        public decimal ValidBet { get; set; }
        public decimal WinlostAmount { get; set; }
    }
}
