using Newtonsoft.Json.Linq;

namespace MemberSite.Extensions;

public static class JsonExtension
{
    public static JObject AddOrSet(this JObject obj, string key, JToken value)
    {
        if (obj.ContainsKey(key))
        {
            obj[key] = value;
        }
        else
        {
            obj.Add(key, value);
        }

        return obj;
    }
}