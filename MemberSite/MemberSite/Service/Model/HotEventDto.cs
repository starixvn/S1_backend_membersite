namespace MemberSite.Service.Model
{
    #region Response

    public class HotEventResponse
    {
        /// <summary>
        /// 聯賽標題名稱
        /// </summary>
        public string SportLeaguesTitle { get; set; }

        /// <summary>
        /// SaBa提供的
        /// </summary>
        public int MatchId { get; set; }
        public string TpId { get; set; }
        public string GameId { get; set; }


        /// <summary>
        /// 主隊名稱
        /// </summary>
        public string HomeTeamName { get; set; }


        /// <summary>
        /// 客隊名稱
        /// </summary>
        public string AwayTeamName { get; set; }

        /// <summary>
        /// 比賽時間
        /// </summary>
        public DateTime BattleTime { get; set; }

        /// <summary>
        /// 主隊賠率 TpWager專案 會有 Job 持續更新
        /// </summary>
        public decimal? HomeOdds { get; set; }

        /// <summary>
        /// 客隊賠率，TpWager專案 會有 Job 持續更新
        /// </summary>
        public decimal? AwayOdds { get; set; }

        /// <summary>
        /// 盤口，TpWager專案 會有 Job 持續更新
        /// </summary>
        public decimal? Handicap { get; set; }

        /// <summary>
        /// 背景底圖
        /// </summary>
        public string BackgroundPic { get; set; }

        /// <summary>
        /// 主隊圖片
        /// </summary>
        public string HomeTeamPic { get; set; }

        /// <summary>
        /// 客隊圖片
        /// </summary>
        public string AwayTeamPic { get; set; }
    }

    #endregion

    #region DTO

    public class HotEventDto 
    {
        /// <summary>
        /// 聯賽標題名稱
        /// </summary>
        public string SportLeaguesTitle { get; set; }

        /// <summary>
        /// SaBa提供的
        /// </summary>
        public int MatchId { get; set; }
        public string TpId { get; set; }
        public string GameId { get; set; }

        /// <summary>
        /// 主隊的聯賽Id
        /// </summary>
        public int HomeTeamLeaguesId { get; set; }

        /// <summary>
        /// 主隊隊伍Id
        /// </summary>
        public int HomeTeamId { get; set; }

        /// <summary>
        /// 主隊名稱
        /// </summary>
        public string HomeTeamName { get; set; }

        /// <summary>
        /// 客隊的聯賽Id
        /// </summary>
        public int AwayTeamLeaguesId { get; set; }

        /// <summary>
        /// 客隊隊伍Id
        /// </summary>
        public int AwayTeamId { get; set; }

        /// <summary>
        /// 客隊名稱
        /// </summary>
        public string AwayTeamName { get; set; }

        /// <summary>
        /// 比賽時間
        /// </summary>
        public DateTime BattleTime { get; set; }

        /// <summary>
        /// 主隊賠率 TpWager專案 會有 Job 持續更新
        /// </summary>
        public decimal? HomeOdds { get; set; }

        /// <summary>
        /// 客隊賠率，TpWager專案 會有 Job 持續更新
        /// </summary>
        public decimal? AwayOdds { get; set; }

        /// <summary>
        /// 盤口，TpWager專案 會有 Job 持續更新
        /// </summary>
        public decimal? Handicap { get; set; }
    }

    public class SportTeamsInfoDto
    {
        public int SportLeaguesId { get; set; }

        public int TeamId { get; set; }

        public string TeamName { get; set; }
    }

    public class SportLeaguesInfoDto
    {
        public int SportLeaguesId { get; set; }

        public string SportLeaguesName { get; set; }
    }

    #endregion
}
