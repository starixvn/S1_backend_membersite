namespace MemberSite.Service.Model;

public class GetRebateExcludeGameListRequest
{

    public int SiteId { get; set; }
    public int CurrencyId { get; set; }
    public int VipLevel { get; set; }
    public string LangId { get; set; }

}


public class RebateExcludeGameDetail
{
    public string TpName { get; set; }
    public string TpCategoryName { get; set; }
    public int TpGameType { get; set; }
    public int TpGameTypeName { get; set; }
    public int GameName { get; set; }
}


