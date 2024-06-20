using System.Text.Json.Serialization;

namespace MemberSite.Service.Model
{
    public class OlympicMemberDepositTicketInfoResponse
    {
            /// <summary>
            /// 玩家名稱
            /// </summary>
            [JsonPropertyName("userName")]
            public string UserName { get; set; }

            /// <summary>
            /// 活動任務
            /// </summary>
            public IEnumerable<PromotionMission> PromotionMission { get; set; }

            /// <summary>
            /// 活動進度
            /// </summary>
            public OlympicEventPromotionMissionProgress OlympicEventPromotionMissionProgress { get; set; }

            /// <summary>
            /// 玩家有幾張票可以用
            /// </summary>
            [JsonPropertyName("availableGoldMetals")]
            public int AvailableGoldMetals { get; set; }
            public int ErrorCode { get; set; }
    }

    public class OlympicEventPromotionMissionProgress
    {
        public decimal DepositAmount { get; set; }
        public decimal TurnoverSum { get; set; }
    }

    public class OlympicEventSBSWModel
    {
        public string txId { get; set; }
        public string refId { get; set; }
        public string leagueName_en { get; set; }
    }
    
    public class OlympicEventDivisionResult
    {
        public int Count { get; set; }
        public decimal Remainder { get; set; }
    }

    public class ClaimOlympicEventPrizeRequest
    {
        public int CustId { get; set; }
        public int UsedGoldMetalCount { get; set; }
    }

    public class ClaimOlympicEventPrizeResponse
    {
        public int ErrorCode { get; set;}
        public string Message { get; set;}
    }
}