using Newtonsoft.Json;

namespace MemberSite.Service.Library
{
    public static class JsonContentParser
    {
        public static string ParseByLanguage(string content, string languageCode, string defaultLanguage)
        {
            var json = JsonConvert.DeserializeObject<Dictionary<string, string>>(content);
            if (json.TryGetValue(languageCode, out var value)) return value;

            if (json.TryGetValue(defaultLanguage, out value)) return value;

            return value;
        }
    }
}
