namespace MemberSite.Service.Model
{
    public class Leaderboard
    {
        public int CustId { get; set; }
        public string UserName { get; set; }
        public decimal WTurnover { get; set; }
        public int Status { get; set; }
    }
    public class LeaderboardCustRank
    {
        public int CustRank { get; set; }
        public decimal point { get; set; }
        public int Status { get; set; }
    }
    public class Prize
    {
        public int PrizeId { get; set; }
        public int PrizeType { get; set; }
        public int Jackpot { get; set; }
        public string PrizeName { get; set; }
        public decimal Amount { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int Quota { get; set; }
    }

    public class CustGift
    {
        public int Jackpot { get; set; }
        public int PrizeId { get; set; }
        public string PrizeName { get; set; }
        public int TicketCount { get; set; }
    }
    public class CustGiftList
    {
        public DateTime EventDate { get; set; }
        public int PrizeId { get; set; }
        public string PrizeName { get; set; }
        public decimal Amount { get; set; }
        public DateTime DrawTime { get; set; }
    }
    public class PrizeList
    {
        public int PrizeIndex { get; set; }
        public int PrizeId { get; set; }
        public string PrizeName { get; set; }
        public int PrizeType { get; set; }
    }
    public class PrizeDisPlayList
    {
        public int EventId { get; set; }
        public int Index { get; set; }
        public int PrizeIndex { get; set; }
        public int PrizeId { get; set; }
        public string PrizeName { get; set; }
    }
    public class DrawPrizeMessage
    {
        public string Tital { get; set; }
        public string Message { get; set; }
    }

    public class EventDrawInfo
    {
        public string EventName { get; set; }
        public string ConditionSp { get; set; }
    }
    public class AngPowTicket 
    {
        public int CustId { get; set; }
        public int TicketNo { get; set; }
        public bool IsUsed { get; set; }
    }
}
