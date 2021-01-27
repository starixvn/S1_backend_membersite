using Newtonsoft.Json;
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
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
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
        public string PrizeName { get; set; }
    }
    public class CustGiftList
    {
        public DateTime EventDate { get; set; }
        public int PrizeId { get; set; }
        public string PrizeName { get; set; }
        public decimal Amount { get; set; }
        public DateTime DrawTime { get; set; }
    }
    public class LeaderboardEvent
    {
        public string NowEventCode { get; set; }
        public string LastEventCode { get; set; }
    }
    public class LeaderboardInfo
    {
        public string EventCode { get; set; }
        public int SiteId { get; set; }
        public string Tital { get; set; }
        public Dictionary<string, string> TitalList { get; set; }
        public string LobbyPic { get; set; }
        public string RewardPic { get; set; }
        public string RewardDESC { get; set; }
        public Dictionary<string, Dictionary<string, string>> RewardDESCList { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public IEnumerable<RewardInfo> RewardInfolist { get; set; }
       
    }
    public class RewardInfo
    {
        public int CurrencyId { get; set; }
        public int CategoryId { get; set; }
        public string DetailUrl { get; set; }
        public string Rewards { get; set; }
        public int RewardsTotal { get; set; }
        public Dictionary<string, string> DetailUrlList { get; set; } 
        public Dictionary<string, List<Dictionary<string, string>>> RewardsList { get; set; }

    }
}
