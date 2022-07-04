namespace MemberSite.Service.Model.Store.Redemption;


public class CreateRedemptionRequest
{
    public int CustId { get; set; }
    public int SiteId { get; set; }
    public int CurrencyId { get; set; }
    public int GiftId { get; set; }
    public int Point { get; set; }
}
