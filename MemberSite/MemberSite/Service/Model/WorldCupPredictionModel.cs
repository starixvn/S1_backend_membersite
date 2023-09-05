using System.Text.Json.Serialization;

using MemberSite.Service.Model.enums;

namespace MemberSite.Service.Model
{
    #region 隊伍設定

    public class WorldCupPredictionSettingRequest
    {
        /// <summary>
        /// 語系 Id
        /// </summary>
        public string LangId { get; set; }
    }

    public class WorldCupPredictionSettingResponse
    {
        /// <summary>
        /// 32強
        /// </summary>
        [JsonPropertyName("top32Team")]
        public IEnumerable<WorldCupEventSetting> Top32Team { get; set; }

        /// <summary>
        /// 16 強
        /// </summary>
        [JsonPropertyName("roundOf16")]
        public IEnumerable<WorldCupEventSetting> RoundOf16 { get; set; }

        /// <summary>
        /// 8強
        /// </summary>
        [JsonPropertyName("quarterFinals")]
        public IEnumerable<WorldCupEventSetting> QuarterFinals { get; set; }


        /// <summary>
        /// 4強
        /// </summary>
        [JsonPropertyName("semiFinals")]
        public IEnumerable<WorldCupEventSetting> SemiFinals { get; set; }

        /// <summary>
        /// 季軍戰爭奪隊伍
        /// </summary>
        [JsonPropertyName("3rdPlace")]
        public IEnumerable<WorldCupEventSetting> ThirdRdPlace { get; set; }

        /// <summary>
        /// 冠軍戰爭奪隊伍
        /// </summary>
        [JsonPropertyName("final")]
        public IEnumerable<WorldCupEventSetting> Final { get; set; }

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

    public class WorldCupPredictionCountry
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

    public class WorldCupEventSetting
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

    public class WorldCupPredictionOptionsDto
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

    public class WorldCupPredictionSettingDto
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
        /// 票券門檻，第一張到第三張分別需要達標金額
        /// </summary>
        [JsonPropertyName("ticketLimit")]
        public List<int> TicketLimit { get; set; }

        /// <summary>
        /// 玩家期間內累積充值金額 2022/08/01 ~ 2022/10/31
        /// </summary>
        [JsonPropertyName("DepositAmount")]
        public decimal DepositAmount { get; set; }

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
        /// 玩家投票紀錄
        /// </summary>
        [JsonPropertyName("userTicketResult")]
        public UserTicketResult UserTicketResult { get; set; }

        /// <summary>
        /// 隊伍選單
        /// </summary>
        [JsonPropertyName("selectOptions")]
        public SelectOptions SelectOptions { get; set; }
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

        [JsonPropertyName("groupG")]
        public List<GroupSelect> GroupG { get; set; }

        [JsonPropertyName("groupH")]
        public List<GroupSelect> GroupH { get; set; }
    }

    public class UserTicket
    {
        /// <summary>
        /// 國家Id
        /// </summary>
        [JsonPropertyName("countryId")]
        public int? CountryId { get; set; }

        /// <summary>
        /// 票券狀態 1:Active 可用
        /// 票券狀態 2:Finished 已選
        /// 票券狀態 3:Unstated 為選
        /// </summary>
        [JsonPropertyName("ticketStatus")]
        public WorldCupUserTicketStatus TicketStatus { get; set; }
    }

    public class UserTicketResult
    {
        [JsonPropertyName("ticket1")]
        public UserTicket Ticket1 { get; set; }

        [JsonPropertyName("ticket2")]
        public UserTicket Ticket2 { get; set; }

        [JsonPropertyName("ticket3")]
        public UserTicket Ticket3 { get; set; }
    }

    public class MemberDepositAmountDailyDto
    {
        /// <summary>
        /// 充值金額
        /// </summary>
        public decimal DepositAmount { get; set; }

        /// <summary>
        /// 歸帳日期
        /// </summary>
        public DateTime AccountDate { get; set; }
    }

    public class DepositAmountSettingDto
    {
        public int SiteId { get; set; }

        public int CurrencyId { get; set; }

        /// <summary>
        /// 累積存款標準
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// 票券類型 Ticket1:1;Ticket2:2;Ticket3:3
        /// </summary>
        public int TicketType { get; set; }
    }

    public class WorldCupMemberTicketInfoDto
    {
        public int TicketType { get; set; }
        public int? TeamId { get; set; }
    }

    public class WorldCupDateSettingDto
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

    #endregion
}
