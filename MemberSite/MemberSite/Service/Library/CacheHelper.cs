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
        
        public TResult CacheTryGetValueSet<T, TResult>(string key, double expirationSecond, TResult t , Func<T, TResult> func)
        {
            // Look for cache key.
            if (!_cache.TryGetValue(key, out t))
            {
                // t = func(t);
                _cache.Set(key, t, new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(expirationSecond)));
            }


            return t;
        }
    }
}