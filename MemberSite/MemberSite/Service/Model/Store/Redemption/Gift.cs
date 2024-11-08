using MemberSite.Service.Model.enums;
using MemberSite.Service.Model.Store.Frontend;
using System.Text.Json.Serialization;

namespace MemberSite.Service.Model.Store.Redemption;

public class Gift
{
    public int GiftId { get; set; }
    public string GiftName { get; set; }
    public int SiteId { get; set; }
    public string SiteName { get; set; }
    public int CurrencyId { get; set; }
    public string IsoCode { get; set; }
    public int CategoryId { get; set; }
    public string Lang { get; set; }
    public string Description { get; set; }
    public string PictureUrl { get; set; }
    public GeneralStatusEnum Status { get; set; }
    [JsonIgnore]
    public string TagIds { get; set; }
    public IEnumerable<GiftTag> Tags { get; set; }
    public int Point { get; set; }
    public decimal PriceWorth { get; set; }
    public bool ShowStock { get; set; }
    public int Stock { get; set; }
    public int Sort { get; set; }
}

public class LastUpdateModel
{
    private DateTimeOffset _lastUpdateTime { get; set; }
    public string LastUpdater { get; set; }
    public DateTimeOffset LastUpdateTime
    {
        get => _lastUpdateTime;
        set => _lastUpdateTime = new DateTimeOffset(value.DateTime, TimeSpan.Zero);
    }
}
