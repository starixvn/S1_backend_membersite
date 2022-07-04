using MemberSite.Service.Model.enums;

namespace MemberSite.Service.Library;

public interface ICacheHelper
{
    TItem TryGetOrCreate<TItem>(string key, TimeSpan timeSpan, Func<TItem> factory,
        RedisDataBase redisDataBase = RedisDataBase.MemberSite);

    Task<TItem> TryGetOrCreateAsync<TItem>(string key, TimeSpan? timeSpan, Func<Task<TItem>> factory,
        RedisDataBase redisDataBase = RedisDataBase.MemberSite);
}
