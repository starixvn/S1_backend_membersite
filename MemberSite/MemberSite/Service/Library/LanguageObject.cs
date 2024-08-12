using MemberSite.Service.BLL;
using MemberSite.Service.Model;

namespace MemberSite.Service.Library
{
    public class LanguageObject
    {
        private static Dictionary<int, IEnumerable<SpecialLangRule>> _langSpecialRule;

        public LanguageObject(IServiceProvider provider)
        {
            var configBll = provider.GetRequiredService<ConfigBLL>();
            _langSpecialRule = configBll.GetConfigValuesBySiteId<IEnumerable<SpecialLangRule>>("LangSpecialRule");
        }

        public static LanguageObject Create(ConfigBLL configBll, int siteId, int? currency, string languageCode, string defaultLanguage = "en")
        {
            var langSpecialRule = configBll.GetConfigValuesBySiteId<IEnumerable<SpecialLangRule>>("LangSpecialRule")[siteId];
            var rule = langSpecialRule?.FirstOrDefault(x => x.SuffixLangId == languageCode);
            if (rule is not null)
            {
                if (currency.HasValue && rule.CurrencyAndLangMap.Any(x => x.CurrencyId == currency) && languageCode == "en")
                {
                    var lang = rule.CurrencyAndLangMap.FirstOrDefault(x => x.CurrencyId == currency).PrefixLangId;
                    languageCode = $"{lang}_en";
                }
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