using System.Text.Json.Serialization;

using MemberSite.Service.Model.enums;

namespace MemberSite.Service.Model
{
    #region 隊伍設定

    public class CupEventPredictionSettingRequest
    {
        /// <summary>
        /// 語系 Id
        /// </summary>
        public string LangId { get; set; }
    }

    public class CupEventPredictionSettingResponse
    {
        /// <summary>
        /// 32強
        /// </summary>
        [JsonPropertyName("top24Team")]
        public IEnumerable<CupEventSetting> Top24Team { get; set; }

        /// <summary>
        /// 16 強
        /// </summary>
        [JsonPropertyName("roundOf16")]
        public IEnumerable<CupEventSetting> RoundOf16 { get; set; }

        /// <summary>
        /// 8強
        /// </summary>
        [JsonPropertyName("quarterFinals")]
        public IEnumerable<CupEventSetting> QuarterFinals { get; set; }


        /// <summary>
        /// 4強
        /// </summary>
        [JsonPropertyName("semiFinals")]
        public IEnumerable<CupEventSetting> SemiFinals { get; set; }

        /// <summary>
        /// 季軍戰爭奪隊伍
        /// </summary>
        [JsonPropertyName("3rdPlace")]
        public IEnumerable<CupEventSetting> ThirdRdPlace { get; set; }

        /// <summary>
        /// 冠軍戰爭奪隊伍
        /// </summary>
        [JsonPropertyName("final")]
        public IEnumerable<CupEventSetting> Final { get; set; }

        /// <summary>
        /// 16 強之後所有比賽時間
        /// </summary>
        [JsonPropertyName("matchTimes")]
        public IEnumerable<MatchTimeInfoDto> MatchTimes { get; set; }

        /// <summary>
        /// 隊伍選單，手機版分組
        /// </summary>
        [JsonPropertyName("selectOptions")]
        public SelectOptions SelectOptions { get; set; }
    }

    public class CupEventPredictionCountry
    {
        /// <summary>
        /// 國家Id
        /// </summary>
        [JsonPropertyName("countryId")]
        public int CountryId { get; set; }

        /// <summary>
        /// 國家名稱
        /// </summary>
        [JsonPropertyName("countryName")]
        public string CountryName { get; set; }
    }

    public class CupEventSetting
    {
        /// <summary>
        /// 國家名稱
        /// </summary>
        [JsonPropertyName("countryName")]
        public string CountryName { get; set; }

        /// <summary>
        /// 圖片名稱一律小寫
        /// </summary>
        [JsonPropertyName("imageName")]
        public string ImageName { get; set; }

        /// <summary>
        /// 排序
        /// </summary>
        [JsonPropertyName("sort")]
        public int Sort { get; set; }
    }

    public class CupEventPredictionOptionsDto
    {
        /// <summary>
        /// 隊伍Id
        /// </summary>
        public int TeamId { get; set; }

        /// <summary>
        /// 隊伍名稱
        /// </summary>
        public string TeamName { get; set; }
    }

    public class CupEventPredictionSettingDto
    {
        /// <summary>
        /// 隊伍Id
        /// </summary>
        public int TeamId { get; set; }

        /// <summary>
        /// 隊伍名稱
        /// </summary>
        public string TeamName { get; set; }

        /// <summary>
        /// 階層名稱
        /// </summary>
        public string LevelName { get; set; }

        /// <summary>
        /// 排序
        /// </summary>
        public int Sort { get; set; }
    }

    public class MatchTimeInfoDto
    {
        [JsonPropertyName("matchName")]
        public string MatchName { get; set; }

        [JsonPropertyName("matchTime")]
        public string MatchTime { get; set; }
    }

    #endregion

    #region 充值 & 發票
    
    
    public class MemberDepositTicketInfoResponse
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
        public CupEventPromotionMissionProgress CupEventPromotionMissionProgress { get; set; }

        /// <summary>
        /// 玩家有幾張票可以用
        /// </summary>
        [JsonPropertyName("availableTickets")]
        public int AvailableTickets { get; set; }

        /// <summary>
        /// 玩家已經用掉幾張票
        /// </summary>
        [JsonPropertyName("usedTickets")]
        public int UsedTickets { get; set; }
        
        /// <summary>
        /// 玩家未使用的票數
        /// </summary>
        [JsonPropertyName("unUsedTickets")]
        public int UnUsedTickets { get; set; }

        /// <summary>
        /// 玩家投票紀錄
        /// </summary>
        [JsonPropertyName("userTicketResult")]
        public UserTicketResult UserTicketResult { get; set; }

        /// <summary>
        /// 隊伍選單
        /// </summary>
        [JsonPropertyName("selectOptions")]
        public SelectOptions SelectOptions { get; set; }
        
        public int ErrorCode { get; set; }
    }

    public class GroupSelect
    {
        /// <summary>
        /// 國家Id
        /// </summary>
        [JsonPropertyName("countryId")]
        public int CountryId { get; set; }

        /// <summary>
        /// 國家名稱
        /// </summary>
        [JsonPropertyName("countryName")]
        public string CountryName { get; set; }

        /// <summary>
        ///  圖片名稱
        /// </summary>
        [JsonPropertyName("imageName")]
        public string ImageName { get; set; }
    }

    public class SelectOptions
    {
        [JsonPropertyName("groupA")]
        public List<GroupSelect> GroupA { get; set; }

        [JsonPropertyName("groupB")]
        public List<GroupSelect> GroupB { get; set; }

        [JsonPropertyName("groupC")]
        public List<GroupSelect> GroupC { get; set; }

        [JsonPropertyName("groupD")]
        public List<GroupSelect> GroupD { get; set; }

        [JsonPropertyName("groupE")]
        public List<GroupSelect> GroupE { get; set; }

        [JsonPropertyName("groupF")]
        public List<GroupSelect> GroupF { get; set; }

        // [JsonPropertyName("groupG")]
        // public List<GroupSelect> GroupG { get; set; }
        //
        // [JsonPropertyName("groupH")]
        // public List<GroupSelect> GroupH { get; set; }
    }

    public class UserTicket
    {
        public int TicketType { get; set; }
        /// <summary>
        /// 國家Id
        /// </summary>
        [JsonPropertyName("countryId")]
        public int? CountryId { get; set; }

        /// <summary>
        /// 票券狀態 1:Active 可用
        /// 票券狀態 2:Finished 已選
        /// 票券狀態 3:Unstated 未選
        /// </summary>
        [JsonPropertyName("ticketStatus")]
        public CupEventUserTicketStatus TicketStatus { get; set; }
    }

    public class GroupCountryTicket
    {
        /// <summary>
        /// 國家Id
        /// </summary>
        [JsonPropertyName("countryId")]
        public int? CountryId { get; set; }

        /// <summary>
        /// Group By CountryId 的票數
        /// </summary>
        [JsonPropertyName("count")]
        public int Count { get; set; }
    }
    
    public class UserTicketResult
    {
        public List<UserTicket> TicketList { get; set; }
        public List<GroupCountryTicket> GroupCountryTicket { get; set; }
    }

    public class MemberDepositDto
    {
        /// <summary>
        /// 存款單
        /// </summary>
        public string TransId { get; set; }
        
        /// <summary>
        /// 充值金額
        /// </summary>
        public decimal DepositAmount { get; set; }

        /// <summary>
        /// 存款日期
        /// </summary>
        public DateTime StatementTime { get; set; }
    }

    
    public class CustTicketInfoDto
    {
        public int SiteId { get; set; }

        public int CustId { get; set; }

        public int CurrencyId { get; set; }

        public int TicketType { get; set; }

        public byte TicketStatus { get; set; }
        public string TransId { get; set; }
    }
    
    public class CupEventMemberTicketInfoDto
    {
        public int SQ { get; set; }
        public int TicketType { get; set; }
        public int? TeamId { get; set; }
        
        /// <summary>
        /// 使用綁定的存款單
        /// </summary>
        public string TransId { get; set; }
    }

    public class CupEventDateSettingDto
    {
        public int SiteId { get; set; }

        public int CurrencyId { get; set; }

        public DateTime DepositStartTime { get; set; }

        public DateTime DepositEndTime { get; set; }

        public DateTime TicketOpenTime { get; set; }

        public DateTime TicketCloseTime { get; set; }

        /// <summary>
        /// 時區
        /// </summary>
        public string TimeZone { get; set; }

    }


    public class UserPollTicketRequest
    {
        [JsonPropertyName("ticketType")]
        public int TicketType { get; set; }

        [JsonPropertyName("countryId")]
        public int CountryId { get; set; }
    }

    public class MultiUserPollTicketRequest
    {
        public UserPollTicketRequest UserPollTicket { get; set; }
        public int Count { get; set; }
    }
    
    #endregion
}
