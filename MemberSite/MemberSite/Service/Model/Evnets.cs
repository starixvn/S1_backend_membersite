namespace MemberSite.Service.Model
{

    #region LeaderBoard

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

    public class GetLeaderboardListReq
    {
        public int LBCategoryId { get; set; }
        public string EventCode { get; set; }
        public int TopNum { get; set; }
        public int BeforeDays { get; set; }
    }

    public class GetCustRankReq
    {
        public int LBCategoryId { get; set; }
        public string EventCode { get; set; }
        public int BeforeDays { get; set; }
    }

    public class GetCustRankMonthReq
    {
        public int LBCategoryId { get; set; }
        public string EventCode { get; set; }
        public DateTime SelectDays { get; set; }
    }
    
    public class GetLeaderboardListMonthReq
    {
        public int LBCategoryId { get; set; }
        public string EventCode { get; set; }
        public int TopNum { get; set; }
        public DateTime SelectDays { get; set; }
    }
    
    #endregion
    
    #region DrawPrize

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
        public int Rate { get; set; }
        
        public int TestCount { get; set; }
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

    public class DrawPrizeReq
    {
        public int EventId { get; set; }
    }

    public class MultiDrawPrizeReq
    {
        public int EventId { get; set; }
        public int DrawTimes { get; set; }
    }
    
    #endregion

    #region PromotionMission

    public class PromotionMission
    {
        public string MissionDate { get; set; }
        public int MissionNo { get; set; }
        public bool DepositFlag { get; set; }
        public decimal DepositAmount { get; set; }
        public bool TurnoverFlag { get; set; }
        public decimal TurnoverAmount { get; set; }
        public int TpCategoryId { get; set; }
        public bool AffFlag { get; set; }
        public int AffCount { get; set; }
        public int EggCount { get; set; }
        public bool IsCompleted { get; set; }
    }
    
    public class PromotionMissionProgress
    {
        public decimal UnUsedDepositAmount { get; set; }
        public decimal TurnoverSum { get; set; }
        public int AffCount { get; set; }
    }
    
    public class PromotionMissionMessage
    {
        public int Type { get; set; }
        public string LangId { get; set; }
        public string Message { get; set; }
        public int TpCategoryId { get; set; }
    }

    public class CustPromotionMission
    {
        public string MissionDate { get; set; }
        public int MissionNo { get; set; }
    }
    
    public class NewYearMission2024
    {
        public string MissionDate { get; set; }
        public string MissionName { get; set; }
        public int MissionNo { get; set; }
        public decimal GoalAmount { get; set; }
        public decimal ProgressAmount { get; set; }
        public int EggCount { get; set; }
        public bool IsCompleted { get; set; }
    }
    
    public class CheckPromotionMissionResponse
    {
        public IEnumerable<NewYearMission2024> NewYearMission2024 { get; set; }
        public int EggTicket { get; set; }
    }
    
    public class CustBlessingsStickerResponse
    {
        public bool IsClaimedBigPrize { get; set; }
        public int Stickers { get; set; }
    }

    #endregion
    
}
