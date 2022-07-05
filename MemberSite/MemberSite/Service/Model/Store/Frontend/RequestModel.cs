namespace MemberSite.Service.Model.Store.Frontend;


public class GetListRequest
{
    public int SiteId { get; set; }
    public int CurrencyId { get; set; }
    public IEnumerable<int> CategoryIds { get; set; }
    public IEnumerable<int> Tags { get; set; }
    public string Lang { get; set; }
    public string OrderByField { get; set; }
    public bool Desc { get; set; }
}

