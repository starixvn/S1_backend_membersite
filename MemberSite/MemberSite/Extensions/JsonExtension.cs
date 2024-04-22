using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MemberSite.Extensions;

public static class JsonExtension
{
    public static string AddOrSetExtraData(this string json, IDictionary<string, string> extraData)
    {
        if (extraData == null)
        {
            return json;
        }
        
        if (string.IsNullOrEmpty(json))
        {
            return JsonConvert.SerializeObject(extraData);
        }

        var obj = JObject.Parse(json);
        
        foreach (var item in extraData)
        {
            obj.AddOrSet(item.Key, item.Value);
        }

        return obj.ToString();
    }
    
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