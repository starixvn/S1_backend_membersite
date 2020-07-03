using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
    }

    public class BannerSettingItem
    {
        public string actionLink { get; set; }
        public string urlWithLang { get; set; }
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
    }

    public class ActionLinkInfo
    {
        public string lang { get; set; }
        public string link { get; set; }
        public bool outerLink { get; set; }
    }
}
