using MemberSite.Service.DAL;
using MemberSite.Service.Model;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemberSite.Service.BLL
{
    public class CasinoBLL
    {
        private readonly CasinoService casinoService;
        private readonly IMemoryCache cache;
        public CasinoBLL(IMemoryCache cache, CasinoService casinoService)
        {
            this.casinoService = casinoService;
            this.cache = cache;
        }

        public IEnumerable<MemberFavorite> GetFavoriteGames(int CustId)
        {
            return casinoService.GetFavoriteGames(CustId);
        }

        public void SetFavoriteGames(int CustId, int ProviderId, string GameId, bool IsRemove)
        {
            casinoService.SetFavoriteGames(CustId, ProviderId, GameId, IsRemove);
        }

        public IEnumerable<int> GetProviderIdByCurrency(int CurrencyId, int SiteId)
        {
            string key = $"ProviderIdByCurrency_{CurrencyId}";
            var providerIdByCurrency = cache.Get<IEnumerable<int>>(key);

            if (providerIdByCurrency == null)
            {
                providerIdByCurrency = casinoService.GetProviderIdByCurrency(CurrencyId, SiteId);
                if (providerIdByCurrency != null)
                {
                    var expireTime = TimeSpan.FromMinutes(10);
                    cache.Set(key, providerIdByCurrency, new MemoryCacheEntryOptions().SetAbsoluteExpiration(expireTime));
                }
            }

            return providerIdByCurrency;
        }
    }
}
