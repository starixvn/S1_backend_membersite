using System;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace MemberSite.Service.Library
{
    public class CacheHelper
    {
        private readonly IMemoryCache _cache;
        public CacheHelper(IServiceProvider provider)
        {
            _cache = provider.GetRequiredService<IMemoryCache>();
        }

        public TResult CacheTryGetValueSet<TResult>(string key, MemoryCacheEntryOptions options, Func<TResult, TResult> func)
        {
            if (!_cache.TryGetValue<TResult>(key, out var result))
            {
                result = func.Invoke(result);
                _cache.Set(key, result, options);
            }
            return result;
        }
    }

}
