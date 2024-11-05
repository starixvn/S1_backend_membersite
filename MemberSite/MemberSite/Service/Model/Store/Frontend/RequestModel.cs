namespace MemberSite.Service.Model.Store.Frontend;


public class StoreGetGiftListRequest : GetGiftListRequest
{
    public int CustId { get; set; }
    public int SiteId { get; set; }
}

public class GetGiftListRequest
{
    public IEnumerable<int> CategoryIds { get; set; }
    public IEnumerable<int> Tags { get; set; }
    public string OrderByField { get; set; }
    public bool Desc { get; set; }
    public int PageSize { get; set; }
    public int CurrencyId { get; set; }
    public string LangId { get; set; }
}

public class GetGiftTagsRequest
{
    public string LangId { get; set; }
}

public class StoreGetMyCoinRequest
{
    public int CustId { get; set; }
}

public class SetFavoriteRequest
{
    public int GiftId { get; set; }
    public bool IsFavorite { get; set; }
}

public class StoreSetFavoriteRequest : SetFavoriteRequest
{
    public int CustId { get; set; }
}

public class StoreGetBalanceRequest
{
    public int CustId { get; set; }
}

public class StoreGetMyPurchesRequest : GetMyPurchesRequest
{
    public int CustId { get; set; }
    public string Lang { get; set; }
}

public class GetMyPurchesRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class StoreGetTagsRequest
{
    public IEnumerable<string> LangCodes { get; set; }
}
