using MemberSite.Service.Model.enums;
using StackExchange.Redis;
using Newtonsoft.Json;

namespace MemberSite.Service.Library
{
    public class RedisCacheHelper : ICacheHelper
    {
        private readonly IConnectionMultiplexer _multiplexer;
        private readonly ILogger<RedisCacheHelper> _logger;

        public RedisCacheHelper(IConnectionMultiplexer multiplexer, ILogger<RedisCacheHelper> logger)
        {
            _multiplexer = multiplexer;
            _logger = logger;
        }

        public TItem TryGetOrCreate<TItem>(string key, TimeSpan timeSpan, Func<TItem> factory,
            RedisDataBase redisDataBase = RedisDataBase.MemberSite)
        {
            key = $"{redisDataBase}:{key}";
            try
            {
                var db = _multiplexer.GetDatabase((int)redisDataBase);

                if (!db.KeyExists(key))
                {
                    var result = factory();
                    if (result != null)
                    {
                        db.StringSet(key, JsonConvert.SerializeObject(result), timeSpan);
                    }

                    return result;
                }

                var json = db.StringGet(key);
                return JsonConvert.DeserializeObject<TItem>(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{key} | {ex.Message}");
                return factory();
            }
        }

        public async Task<TItem> TryGetOrCreateAsync<TItem>(string key, TimeSpan? timeSpan, Func<Task<TItem>> factory,
            RedisDataBase redisDataBase = RedisDataBase.MemberSite)
        {
            key = $"{redisDataBase}:{key}";
            try
            {
                var db = _multiplexer.GetDatabase((int)redisDataBase);

                if (!db.KeyExists(key))
                {
                    var result = await factory();
                    if (result != null)
                    {
                        await db.StringSetAsync(key, JsonConvert.SerializeObject(result), timeSpan);
                    }

                    return result;
                }

                var json = db.StringGet(key);
                return JsonConvert.DeserializeObject<TItem>(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{key} | {ex.Message}");
                return await factory();
            }
        }
    }
}