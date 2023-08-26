using MemberSite.Service.Model.enums;

namespace MemberSite.Extensions;

public static class StoreExtension
{
    private static IEnumerable<int> _supportStoreSiteIds = new[]
    {
        (int) SiteId.Nova88
    };

    private static IEnumerable<int> _supportStoreCurrencyIds = new[]
    {
        (int) CurrencyId.MYR,
        (int) CurrencyId.THB,
        (int) CurrencyId.IDR
    };

    private static Dictionary<string, CurrencyId> _langCurrencyMapping =
        new(StringComparer.OrdinalIgnoreCase)
        {
            { "en", CurrencyId.MYR },
            { "cs", CurrencyId.MYR },
            { "my", CurrencyId.MYR },
            { "th_en", CurrencyId.THB },
            { "th", CurrencyId.THB },
            { "id_en", CurrencyId.IDR },
            { "id", CurrencyId.IDR }
        };

    public static bool SupportStore(int siteId, int currencyId)
        => _supportStoreSiteIds.Contains(siteId)
           && _supportStoreCurrencyIds.Contains(currencyId);

    public static CurrencyId GetCurrencyByLangId(string langId)
    {
        if (string.IsNullOrEmpty(langId))
        {
            return CurrencyId.MYR;
        }

        return _langCurrencyMapping.ContainsKey(langId)
            ? _langCurrencyMapping[langId]
            : CurrencyId.MYR;
    }
}