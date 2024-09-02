using MemberSite.Service.Model.enums;

namespace MemberSite.Service.Model.Store.Redemption
{
    public class GiftInfo
    {
        public int SiteId { get; set; }
        public int CurrencyId { get; set; }
        public int GiftId { get; set; }
        public string GiftName { get; set; }
        public int CategoryId { get; set; }
        public string Description { get; set; }
        public string PictureUrl { get; set; }
        public GeneralStatusEnum Status { get; set; }

        public int Point { get; set; }
        public decimal PriceWorth { get; set; }
        public bool ShowStock { get; set; }
        public int Stock { get; set; }
        public int Sort { get; set; }

        public int GiftType { get; set; }

        public int Round { get; set; }
    }
}
