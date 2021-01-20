using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemberSite.Service.Model
{
    public class Evnets
    {

    }
    public class Leaderboard
    {
        public DateTime Date { get; set; }
        public int LBCategoryId { get; set; }
        public string LBCategoryName { get; set; }
        public int CustId { get; set; }
        public string UserName { get; set; }
        public decimal WTurnover { get; set; }
    }
    public class LeaderboardCustRank
    {
        public int CustRank { get; set; }
        public decimal point { get; set; }
    }
    public class Prize
    {
        public int PrizeId { get; set; }
        public int PrizeType { get; set; }
        public string PrizeName { get; set; }
        public decimal Amount { get; set; }
        public int Quota { get; set; }
    }

    public class Prizeleft
    {
        public int PrizeId { get; set; }
        public int EventId { get; set; }
        public string GiftName { get; set; }
        public int Quantity { get; set; }
        public int UsedQuantity { get; set; }
        public int LeftCount { get; set; }
        public int Cumsum { get; set; }
    }
    public class CustGift
    {
        public int PrizeId { get; set; }
        public string GiftName { get; set; }
    }
    public class CustGiftList
    {
        public DateTime EventDate { get; set; }
        public int PrizeId { get; set; }
        public string PrizeName { get; set; }
        public decimal Amount { get; set; }
        public DateTime DrawTime { get; set; }


    }

}
