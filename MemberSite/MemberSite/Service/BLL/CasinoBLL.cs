using MemberSite.Service.DAL;
using MemberSite.Service.Model;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;

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
            var datas = casinoService.GetFavoriteGames(CustId);
            var result = new List<MemberFavorite>();
            if(datas != null)
            {
                var tmp = Newtonsoft.Json.JsonConvert.DeserializeObject<IEnumerable<string>>(datas.Favorite);
                foreach(var gameInfo in tmp)
                {
                    var info = gameInfo.Split("_");
                    int.TryParse(info[0], out var providerId);
                    result.Add(new MemberFavorite() { ProviderId = providerId, GameId = info[1] });
                }
            }

            return result;
        }

        public void SetFavoriteGames(int CustId, int ProviderId, string GameId, bool IsRemove)
        {
            var gameInfo = $"{ProviderId}_{GameId}";
            casinoService.SetFavoriteGames(CustId, gameInfo, IsRemove);
        }

        public IEnumerable<int> GetProviderIdByCurrency(int CurrencyId, int SiteId)
        {
            var key = $"ProviderIdByCurrency_{CurrencyId}_{SiteId}";
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
        public IEnumerable<GameCasionByCurrencyAndSite> GetGameCasinoByCurrencyAndSite(int CurrencyId, int SiteId)
        {
           return casinoService.GetGameCasinoByCurrencyAndSite(CurrencyId, SiteId);
        }
        
    }
}
