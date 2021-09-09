using System;
using System.Collections.Generic;
using System.Linq;
using MemberSite.Service.DAL;
using MemberSite.Service.Model;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace MemberSite.Service.Library
{
    public class CacheHelper
    {
        private readonly IMemoryCache _cache;
        private readonly ThirdPartyService thirdPartyService;
        public CacheHelper(IServiceProvider provider)
        {
            _cache = provider.GetRequiredService<IMemoryCache>();
            thirdPartyService = provider.GetRequiredService<ThirdPartyService>();
        }

        public TResult CacheTryGetValueSet<TResult>(string key, TimeSpan expiration, Func<TResult, TResult> func)
        {
            // Look for cache key.
            if (!_cache.TryGetValue<TResult>(key, out var result))
            {
                result = func.Invoke(result);
                //Check Expiration
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                { 
                    SlidingExpiration= expiration,
                }.SetAbsoluteExpiration(expiration);

                _cache.Set(key, result, cacheEntryOptions);
            }
            return result;
        }
    }

}