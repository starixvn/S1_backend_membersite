using System.Text.Json.Serialization;

namespace MemberSite.Service.Model
{
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
}
