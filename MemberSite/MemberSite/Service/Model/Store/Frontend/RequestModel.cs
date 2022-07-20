namespace MemberSite.Service.Model.Store.Frontend;


public class StoreGetGiftListRequest : GetGiftListRequest
{
    public int CustId { get; set; }
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

public class SetFavoriteRequest
{
    public int GiftId { get; set; }
    public bool IsFavorite { get; set; }
}

public class StoreSetFavoriteRequest: SetFavoriteRequest
{
    public int CustId { get; set; }
}

