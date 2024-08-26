namespace MemberSite.Service.Model;

public class LanguageObject
{
    public string LanguageCode { get; private set; }
    public string DefaultLanguage { get; private set; }

    public LanguageObject(string languageCode, string defaultLanguage)
    {
        LanguageCode = languageCode;
        DefaultLanguage = defaultLanguage;
    }
}