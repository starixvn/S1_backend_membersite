namespace MemberSite.Service.Model.Game
{

    public class RecentlyViewedInfo
    {
        public int TpId { get; set; }
        public int TpGameType { get; set; }
        public int CategoryId { get; set; }
        public string GameId { get; set; }
        public string BetType { get; set; }
        public string TpName { get; set; }
        public Dictionary<string, string> GameName { get; set; }
        public Dictionary<string, string> GameLogo { get; set; }
        public string TpLogo { get; set; }
        public int TpGameIdNo { get; set; }
        public bool IsMaintain { get; set; }
    }

    public class RecentlyViewedRequest
    {
        /// <summary>
        /// 裝置類型
        /// </summary>
        public int DeviceType { get; set; }
        public int? CurrencyId { get; set; }
    }

    /// <summary>
    /// 近期瀏覽
    /// </summary>
    public class RecentlyViewedResponse
    {
        public string imgUrl { get; set; }
        public string bannerUrl { get; set; }
        public List<RecentlyViewedInfo> RecentlyViewedList { get; set; } = new List<RecentlyViewedInfo>();

        public RecentlyViewedResponse(string imgUrl, string bannerUrl, List<RecentlyViewedInfo> recentlyViewedList)
        {
            this.imgUrl = imgUrl;
            this.bannerUrl = bannerUrl;
            RecentlyViewedList = recentlyViewedList;
        }
    }
}
