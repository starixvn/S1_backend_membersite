namespace MemberSite.Service.Model
{
    //public class Banner
    //{
    //    //public int SiteId { get; set; }
    //    public string FolderName { get; set; }
    //    public string Lang { get; set; }
    //    public string ImgName { get; set; }
    //    public string href { get; set; }
    //    //public int CurrencyId { get; set; }
    //    public string Title { get; set; }
    //}

    public class BannerSetting
    {
        public IEnumerable<BannerSettingItem> Promotion { get; set; }
        public IEnumerable<BannerSettingItem> Casino { get; set; }
        public IEnumerable<BannerSettingItem> Sports { get; set; }
    }

    public class BannerSettingItem
    {
        public string actionLink { get; set; }
        public bool urlWithLang { get; set; }
        public bool outerLink { get; set; }
        public IEnumerable<ActionLinkInfo> actionLinkByLang { get; set; }
        public string img { get; set; }
        public string[] lang { get; set; }
        public int? tpId { get; set; }
        public int? gameId { get; set; }
        public bool checkPromo { get; set; }
        public int[] currencyId { get; set; }
        public string imgFolder { get; set; }
        public bool isBonus { get; set; }
        public string[] refId { get; set; }
        public string imgURL { get; set; }
        public bool fromDB { get; set; }
        public bool hideInHome { get; set; }
        public int? matchid { get; set; }
    }

    public class ActionLinkInfo
    {
        public string lang { get; set; }
        public string link { get; set; }
        public bool? outerLink { get; set; }
    }

    public class BannerV2
    {
        public string BannerName { get; set; }
        public int BannerType { get; set; }
        public int ActionMode { get; set; }
        public string BonusCode { get; set; }
        public string MatchId { get; set; }
        public string TpId { get; set; }
        public string GameId { get; set; }
        public string UrlRouter { get; set; }
        public string Details { get; set; }
        public string BannerLink { get; set; }
        public string BannerPCUrl { get; set; }
        public string BannerMobileUrl { get; set; }
    }

    public class BannerV3
    {
        public string BannerName { get; set; }
        public int BannerType { get; set; }
        public int ActionMode { get; set; }
        public string BonusCode { get; set; }
        public string TpId { get; set; }
        public string GameId { get; set; }
        public string UrlRouter { get; set; }
        public string Details { get; set; }
        public string BannerLink { get; set; }
        public string BannerPCUrl { get; set; }
        public string BannerMobileUrl { get; set; }
        public string Logo { get; set; }
        public string BackgroundPic { get; set; }
        public int tpGameType { get; set; }
    }
}
