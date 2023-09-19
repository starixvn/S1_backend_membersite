namespace MemberSite.Service.Library
{
    public class LanguageObject
    {
        public static readonly Dictionary<int, string> CurrencyMap = new()
        {
            { 4, "th"},
            { 15, "id"},
            { 61, "hi"}
        };
        public static LanguageObject Create(int? currency, string languageCode, string defaultLanguage = "en")
        {
            if (currency.HasValue && CurrencyMap.TryGetValue(currency.Value, out var lang) && languageCode == "en")
            {
                languageCode = $"{lang}_en";
            }
            return new LanguageObject(languageCode, defaultLanguage);
        }
        public string LanguageCode { get; private set; }
        public string DefaultLanguage { get; private set; }

        private LanguageObject(string languageCode, string defaultLanguage)
        {
            LanguageCode = languageCode;
            DefaultLanguage = defaultLanguage;
        }
    }
}
