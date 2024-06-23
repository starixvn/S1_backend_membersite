using MemberSite.Service.Model.enums;

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
    
    public class GetLeaderboardSettingReq
    {
        public int SiteId { get; set; }
        public int CurrencyId { get; set; }
    }
    
    public class LeaderboardSetting
    {
        public LeaderboardPointCalculation LeaderboardPointCalculation { get; set; }
        public IEnumerable<LeaderboardRankingBonus> LeaderboardRankingBonus { get; set; }
    }

    public class LeaderboardPointCalculation
    {
        public string Date { get; set; }
        public int SiteId { get; set; }
        public int CurrencyId { get; set; }
        public decimal SportTurnover { get; set; }
        public decimal SlotTurnover { get; set; }
        public decimal LiveCasinoTurnover { get; set; }
    }

    public class LeaderboardRankingBonus
    {
        public int SiteId { get; set; }
        public string SiteName { get; set; }
        public string Currency { get; set; }
        public int CurrencyId { get; set; }
        public string Ranking { get; set; }
        public decimal MinPoint { get; set; }
        public decimal Bonus { get; set; }
    }

    #endregion

    #region DrawPrize

    public class Prize
    {
        public int PrizeId { get; set; }
        public int EventId { get; set; }
        public int CurrencyId { get; set; }
        public int PrizeType { get; set; }
        public int Jackpot { get; set; }
        public string PrizeName { get; set; }
        public decimal Amount { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int Quota { get; set; }
        public int Rate { get; set; }
        public DrawPrizeTypeEnum DrawPrizeType { get; set; }
    }

    public class CustGift
    {
        public int Jackpot { get; set; }
        public int PrizeId { get; set; }
        public string PrizeName { get; set; }
        public int TicketCount { get; set; }
        public DrawPrizeTypeEnum DrawPrizeType { get; set; }
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

    public class ReCheckPromotionReq
    {
        public int CustId { get; set; }
        /// <summary>
        /// ex:2024-02-07
        /// </summary>
        public DateTime Date { get; set; }

    }

    #endregion

    #region PromotionMission

    public class PromotionMission
    {
        public string MissionName { get; set; }
        public int MissionNo { get; set; }
        public bool DepositFlag { get; set; }
        public decimal DepositAmount { get; set; }
        public bool TurnoverFlag { get; set; }
        public decimal TurnoverAmount { get; set; }
        public int TpCategoryId { get; set; }
        public bool AffFlag { get; set; }
        public int AffCount { get; set; }
        public int TicketCount { get; set; }
        public bool IsCompleted { get; set; }
    }

    public class PromotionMissionProgress
    {
        public decimal UnUsedDepositAmount { get; set; }
        public decimal TurnoverSum { get; set; }
        public int AffCount { get; set; }
    }
    
    public class CupEventPromotionMissionProgress
    {
        public decimal DepositAmount { get; set; }
        public decimal TurnoverSum { get; set; }
        public IEnumerable<CupEventAffResponse> AffList { get; set; }
        public int CompletedBetCount { get; set; }
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
        public int TpCategoryId { get; set; }
        public int MissionNo { get; set; }
        /// <summary>
        /// 目標金額
        /// </summary>
        public decimal GoalAmount { get; set; }
        /// <summary>
        /// 進度金額
        /// </summary>
        public decimal ProgressAmount { get; set; }
        public int EggCount { get; set; }
        public bool IsCompleted { get; set; }
        public PromotionMissionTypeEnum Type { get; set; }
    }

    public class CheckPromotionMissionResponse
    {
        public IEnumerable<NewYearMission2024> NewYearMission2024 { get; set; }
        /// <summary>
        /// 目前抽獎券數量
        /// </summary>
        public int EggTicket { get; set; }
    }

    public class CustBlessingsStickerResponse
    {
        public bool IsClaimedBigPrize { get; set; }
        public int Stickers { get; set; }
    }

    public class EggTicketData
    {
        public int Id { get; set; }
        public int CustId { get; set; }

        public string MissionDate { get; set; }

        public int IsUsed { get; set; }

        public DateTime CreateTime { get; set; }

        public DateTime UpdateTime { get; set; }
    }

    #endregion

}
