using System.Text.Json.Serialization;
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
    
    #region Lottery
    
    public class GetChineseZodiacStickerResponse
    {
        public ChineseZodiacStickerId StickerId { get; set; }
        public decimal StickerPrize { get; set; }
        public bool CanBuy { get; set; }
    }
    
    public class RaffleTicketStickerSetting
    {
        public int SiteId { get; set; }
        public int CurrencyId { get; set; }
        public decimal StickerPrize { get; set; }
        public decimal TurnOver { get; set; }
    }

    public class LotteryEventDraw
    {
        public int SiteId { get; set; }
        public int RoundId { get; set; }
        private DateTimeOffset _startDate { get; set; }
        public DateTimeOffset StartDate
        {
            get => _startDate;
            set => _startDate = new DateTimeOffset(value.DateTime, new TimeSpan(8, 0, 0));
        }
        private DateTimeOffset _endDate { get; set; }
        public DateTimeOffset EndDate
        {
            get => _endDate;
            set => _endDate = new DateTimeOffset(value.DateTime, new TimeSpan(8, 0, 0));
        }
        private DateTimeOffset _drawDate { get; set; }
        public DateTimeOffset DrawDate
        {
            get => _drawDate;
            set => _drawDate = new DateTimeOffset(value.DateTime, new TimeSpan(8, 0, 0));
        }
        public int Status { get; set; }
    }

    public class CustLotteryTicket
    {
        public int SQ { get; set; }
        public int CustId { get; set; }
        public int RoundId { get; set; }
        public string TicketNo { get; set; }
        public ChineseZodiacStickerId StickerId { get; set; }
        public int Type { get; set; }
        public LotteryPrizeCategory LotteryPrizeCategory { get; set; }
        public DateTime CreateTime { get; set; }
    }
    
    public class BuyChineseZodiacStickerReq
    {
        [JsonPropertyName("stickerid")]
        public int StickerId { get; set; }
    }
    
    public class BuyChineseZodiacStickerResponse
    {
        public int ErrorCode { get; set;}
        public string Message { get; set;}
    }

    public class CustLotteryTicketInsertRequest
    {
        public int CustId { get; set; }
        public string TicketNo { get; set; }
        public int RoundId { get; set; }
        public ChineseZodiacStickerId StickerId { get; set; }
        public int Type { get; set; }
    }

    public class BuyZodiacStickerRequest
    {
        public int SiteId { get; set; }
        public int CustId { get; set; }
        public string UserName { get; set; }
        public string StickerId { get; set; }
        public decimal StickerPrize { get; set; }
        public string Memo { get; set; }
    }
    
    public class LotteryMaintain
    {
        public bool IsMaintain { get; set; }
        public bool isRestricted { get; set; }
    }
    
    public class LotteryMaintainModel
    {
        public DateTime BeginDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsMaintain { get; set; }
        public string UpdateUser { get; set; }
    }
    
    public class LotteryInfo
    {
        public LotteryEventDraw LotteryRoundInfo { get; set; }
        public List<LotteryEventDrawTurnOverSetting> LotteryTierList { get; set; }
        public List<string> TicketNo { get; set; }
        public int TicketCount => TicketNo?.Count ?? 0;
        public decimal TurnoverGoal { get; set; }
        public decimal CustTurnoverProccessing { get; set; }
        public decimal CustTurnoverSummary { get; set; }
        public decimal JackpotAmount { get; set; }
    }
    
    public class LotteryEventDrawTurnOverSetting
    {
        public int SiteId { get; set; }
        public int CurrencyId { get; set; }
        public int Level { get; set; }
        public decimal TurnOverStart { get; set; }
        public decimal TurnOverEnd { get; set; }
    }
    
    public class EventRestriction
    {
        public int CurrencyId { get; set; }
        public int CustId { get; set; }
    }
    
    public class LotteryWinnerData
    {
        public int SiteId { get; set; }
        public int RoundId { get; set; }
        public string FirstPrize { get; set; }
        public string SecondPrize { get; set; }
        public string ThirdPrize { get; set; }
        public string ConsolationPrize { get; set; }
        public string SmallPrize { get; set; }
        public ChineseZodiacStickerId StickerId { get; set; }
        public DateTime CreateTime { get; set; }
        private DateTimeOffset _startDate { get; set; }
        public DateTimeOffset StartDate
        {
            get => _startDate;
            set => _startDate = new DateTimeOffset(value.DateTime, new TimeSpan(8, 0, 0));
        }
        private DateTimeOffset _endDate { get; set; }
        public DateTimeOffset EndDate
        {
            get => _endDate;
            set => _endDate = new DateTimeOffset(value.DateTime, new TimeSpan(8, 0, 0));
        }
        private DateTimeOffset _drawDate { get; set; }
        public DateTimeOffset DrawDate
        {
            get => _drawDate;
            set => _drawDate = new DateTimeOffset(value.DateTime, new TimeSpan(8, 0, 0));
        }
    }
    
    public class GetLotteryWinnerListRequest
    {
        public int SiteId { get; set; }
        public int RoundId { get; set; }
    }
    
    public class GetCurrentLotteryWinnerListResponse
    {
        public int SiteId { get; set; }
        public int RoundId { get; set; }
        public List<string> FirstPrize { get; set; }
        public List<string> SecondPrize { get; set; }
        public List<string> ThirdPrize { get; set; }
        public List<string> ConsolationPrize { get; set; }
        public List<string> SmallPrize { get; set; }
        public ChineseZodiacStickerId StickerId { get; set; }
        public  List<string> TicketNo { get; set; }
        public  CustLotteryTicket CustWinTicketNo { get; set; }
        public ChineseZodiacStickerId CustStickerId { get; set; }
     
    }
    
    public class GetLotteryRoundListResponse
    {
        public int SiteId { get; set; }
        public int RoundId { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }
        public DateTimeOffset DrawDate { get; set; }
    }
    
    public class GetCustLotteryWinningResponse
    {
        public int RoundId { get; set; }
        public string TicketNo { get; set; }
        private DateTimeOffset _startDate { get; set; }
        public DateTimeOffset StartDate
        {
            get => _startDate;
            set => _startDate = new DateTimeOffset(value.DateTime, new TimeSpan(8, 0, 0));
        }
        private DateTimeOffset _endDate { get; set; }
        public DateTimeOffset EndDate
        {
            get => _endDate;
            set => _endDate = new DateTimeOffset(value.DateTime, new TimeSpan(8, 0, 0));
        }
        public LotteryPrizeCategory LotteryPrizeCategory { get; set; }
        public ChineseZodiacStickerId StickerId { get; set; }
        public int Jackpot { get; set; }
    }
    
    public class DrawPrizeJackpotAccumulation
    {
        public int PrizeId { get; set; }
        public int JackpotAmount { get; set; }
    }
    
    #endregion
}
