using System.Text.Json.Serialization;

namespace MemberSite.Service.Model.Store.Frontend;

public class GiftListModel
{
    public int GiftId { get; set; }
    public int SiteId { get; set; }
    public string GiftName { get; set; }
    public string EnGiftName { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; }
    public string GiftDescription { get; set; }
    public string GiftPictureUrl { get; set; }
    [JsonIgnore]
    public string Tags { get; set; }
    public IEnumerable<GiftTag> TagList { get; set; }
    public int Point { get; set; }
    public decimal PriceWorth { get; set; }
    public bool ShowStock { get; set; }
    public int Stock { get; set; }
    public bool IsFavorite { get; set; }
}
