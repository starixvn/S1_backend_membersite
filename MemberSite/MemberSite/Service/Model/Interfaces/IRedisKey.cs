using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace MemberSite.Service.Model.Interfaces
{
    public interface IRedisKey
    {
        public string GetRedisKey()
        {
            var jObject = JObject.Parse(JsonConvert.SerializeObject(this));
            var key = new StringBuilder();
            foreach (var item in jObject.Properties())
            {
                key.Append($"{item.Value}:");
            }
            return key.ToString();
        }
    }
}
