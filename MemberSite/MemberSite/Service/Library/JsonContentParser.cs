using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MemberSite.Service.Library
{
    public static class JsonContentParser
    {
        public static string Parse(string content, string languageCode, string defaultLanguage)
        {
            var json = JsonConvert.DeserializeObject<Dictionary<string, string>>(content);
            if (json.TryGetValue(languageCode, out var value) && !string.IsNullOrEmpty(value)) return value;

            if (json.TryGetValue(defaultLanguage, out value)) return value;

            return value;
        }

        public static string ParseByJObject(string content, string languageCode, string defaultLanguage, string column) 
        {
            var msg = "";
            if (JObject.Parse(content)[languageCode] != null)
                msg = JObject.Parse(content)[languageCode].ToString();

            else if (JObject.Parse(content)[defaultLanguage] != null)
                msg = JObject.Parse(content)[defaultLanguage].ToString();

            return string.IsNullOrEmpty(msg) ? msg : JObject.Parse(msg)[column].ToString();
            
        }
    }
}
