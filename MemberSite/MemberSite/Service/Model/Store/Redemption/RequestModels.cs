namespace MemberSite.Service.Model.Store.Redemption;


public class SendCreateRedemptionRequest
{
    public int GiftId { get; set; }
    public int Point { get; set; }
}

public class SendSetShoppingCartRequest
{
    public IEnumerable<int> GiftIds { get; set; }
    public int IsAdd { get; set; }
}

public class CreateRedemptionRequest
{
    public int CustId { get; set; }
    public int SiteId { get; set; }
    public int CurrencyId { get; set; }
    public int GiftId { get; set; }
    public int Point { get; set; }
}

public class SetShoppingCartRequest
{
    public int SiteId { get; set; }
    public int CurrencyId { get; set; }
    public int CustId { get; set; }
    public IEnumerable<int> GiftIds { get; set; }
    public int IsAdd { get; set; }
}

public class AddPrize
{
    public int TpId { set; get; }
    public int CustId { set; get; }
    public int SiteId { set; get; }
    public Dictionary<string, string> inputs { set; get; }
}

public class SetRedemptionInfoRequest
{
    public int SiteId { get; set; }
    public string TransId { get; set; }
    public int Action { get; set; }
    public string Comment { get; set; }
    public decimal? ActualCost { get; set; }
    public IEnumerable<string> ReceiptUrls { get; set; }
    public string Operator { get; set; }
}