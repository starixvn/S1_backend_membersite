using MemberSite.Service.Model.enums;
namespace MemberSite.Service.Library;
using StackExchange.Redis;

public interface ICacheHelper
{
    TItem TryGetOrCreate<TItem>(string key, TimeSpan timeSpan, Func<TItem> factory, RedisDataBase redisDataBase = RedisDataBase.Member);

    Task<TItem> TryGetOrCreateAsync<TItem>(string key, TimeSpan? timeSpan, Func<Task<TItem>> factory, RedisDataBase redisDataBase = RedisDataBase.Member);

    Task<bool> StringFireAndForgetSetAsync(string key, RedisValue value, TimeSpan? timeSpan, RedisDataBase redisDataBase = RedisDataBase.Member);

    bool StringSet(string key, RedisValue value, TimeSpan? timeSpan, RedisDataBase redisDataBase = RedisDataBase.Member);

    string StringGet(string key, RedisDataBase redisDataBase = RedisDataBase.Member);

    Task<long> StringIncrementAsync(string key, TimeSpan? timeSpan, RedisDataBase redisDataBase = RedisDataBase.Member);

    bool KeyExists(string key, RedisDataBase redisDataBase = RedisDataBase.Member);

    Task<bool> KeyExistsAsync(string key, RedisDataBase redisDataBase = RedisDataBase.Member);

    bool InsertList<TItem>(string key, TimeSpan? timeSpan, IEnumerable<TItem> list, RedisDataBase redisDataBase = RedisDataBase.Member);

    List<TItem> GetListByRange<TItem>(string key, long startIndex, long endIndex, RedisDataBase redisDataBase = RedisDataBase.Member);

    long GetListLength(string key, RedisDataBase redisDataBase = RedisDataBase.Member);

    Task<bool> KeyDeleteAsync(string key, RedisDataBase redisDataBase = RedisDataBase.Member);

    Task<bool> SetNxAsync(string key, string value, TimeSpan timeSpan, RedisDataBase redisDataBase = RedisDataBase.Member);

    Task<bool> TryCreateLockAsync(RedisKey lockKey, RedisValue lockValue, TimeSpan? expiry, RedisDataBase redisDataBase = RedisDataBase.Member);

    Task<bool> UnLockAsync(RedisKey lockKey, RedisValue lockValue, RedisDataBase redisDataBase = RedisDataBase.Member);
}
