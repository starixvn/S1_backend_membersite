using MemberSite.Service.Model.enums;
using System.Text.Json.Serialization;

namespace MemberSite.Service.Model.Store.Frontend;

public class GiftListModel
{
    public int GiftId { get; set; }
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
}

public class GiftTag
{
    public int TagId { get; set; }
    public string TagName { get; set; }
    public string Lang { get; set; }
}

public class CustPointSummary
{
    public int CustId { get; set; }
    public decimal PointBalance { get; set; }
    public decimal PointEarned { get; set; }
    public decimal PointUsed { get; set; }
    public decimal PointLocked { get; set; }
    public List<RewardPointSummary> Detail { get; set; }
}

public class RewardPointSummary
{
    public PointTypeEnum PointType { get; set; }
    public string PointTypeName
    {
        get
        {
            return this.PointType.ToString();
        }
    }
    public decimal PlusSummary { get; set; }
    public decimal MinusSummary { get; set; }
    public decimal TotalSummary { get; set; }
    public Dictionary<string, decimal> DetailRewardCoin { get; set; }
}
