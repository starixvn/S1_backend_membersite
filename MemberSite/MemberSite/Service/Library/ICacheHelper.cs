namespace MemberSite.Service.Library;

public interface ICacheHelper
{
    TItem TryGetOrCreate<TItem>(string key, TimeSpan? timeSpan, Func<string, TItem> factory);
    Task<TItem> TryGetOrCreateAsync<TItem>(string key, TimeSpan? timeSpan, Func<string, Task<TItem>> factory);
}