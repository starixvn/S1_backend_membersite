namespace MemberSite.Service.Model;

public class SpecialLangRule
{
    public string SuffixLangId { get; set; }

    public IEnumerable<CurrencyAndLangMap> CurrencyAndLangMap { get; set; }
}

public class CurrencyAndLangMap
{
    public int CurrencyId { get; set; }

    public string PrefixLangId { get; set; }
}
