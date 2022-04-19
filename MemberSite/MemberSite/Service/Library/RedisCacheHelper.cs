using StackExchange.Redis;
using Newtonsoft.Json;

namespace MemberSite.Service.Library
{
    public class RedisCacheHelper : ICacheHelper
    {
        private readonly IDatabase _redis;
        
        public RedisCacheHelper(IConnectionMultiplexer multiplexer)
        {
            _redis =  multiplexer.GetDatabase();
        }
        public TItem TryGetOrCreate<TItem>(string key, TimeSpan timeSpan, Func<string, TItem> factory)
        {
            if (!_redis.KeyExists(key))
            {
                var result = factory("");

                if (result != null)
                {
                    _redis.StringSet(key, JsonConvert.SerializeObject(result), timeSpan);
                }

                return result;
            }

            var json = _redis.StringGet(key);
            return JsonConvert.DeserializeObject<TItem>(json);
        }
        
        public async Task<TItem> TryGetOrCreateAsync<TItem>(string key, TimeSpan? timeSpan, Func<string, Task<TItem>> factory)
        {
            if (!_redis.KeyExists(key))
            {
                var result = await factory(key);

                if (result != null)
                {
                    await _redis.StringSetAsync(key, JsonConvert.SerializeObject(result), timeSpan);
                }

                return result;
            }

            var json = _redis.StringGet(key);
            return JsonConvert.DeserializeObject<TItem>(json);
        }
    }
}