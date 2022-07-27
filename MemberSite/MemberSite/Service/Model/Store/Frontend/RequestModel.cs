namespace MemberSite.Service.Model.Store.Frontend;


public class StoreGetGiftListRequest : GetGiftListRequest
{
    public int SiteId { get; set; }
    public int CurrencyId { get; set; }
    public string Lang { get; set; }
}

public class GetGiftListRequest
{
    public IEnumerable<int> CategoryIds { get; set; }
    public IEnumerable<int> Tags { get; set; }
    public string OrderByField { get; set; }
    public bool Desc { get; set; }
}

public class StoreGetMyCoinRequest
{
    public int CustId { get; set; }
}

