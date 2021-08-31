using MemberSite.Service.DAL;
using MemberSite.Service.Model;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;

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
                    var (providerId, gameId) = SplitGameInfo(gameInfo); 
                    if(providerId != 0 && !string.IsNullOrEmpty(gameId))
                        result.Add(new MemberFavorite() { ProviderId = providerId, GameId = gameId });
                }
            }

            return result;
        }

        public void SetFavoriteGames(int CustId, int ProviderId, string GameId, bool IsRemove)
        {
            var gameInfo = $"{ProviderId}_{GameId}";
            casinoService.SetFavoriteGames(CustId, gameInfo, IsRemove);
        }

        public (int providerId, string gameId) SplitGameInfo(string gameInfo)
        {
            var underlineIndex = gameInfo.IndexOf('_');
            if (string.IsNullOrWhiteSpace(gameInfo) || underlineIndex == -1) return (0, string.Empty);
            
            var strProviderId = gameInfo.Substring(0, underlineIndex); 
            var gameId = gameInfo.Substring(underlineIndex + 1); 
            int.TryParse(strProviderId, out var providerId);
            return (providerId, gameId);
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

        public IEnumerable<DisableTpGame> GetTpDisableUserByCustId(int custId)
        {
            return casinoService.GetTpDisableUserByCustId(custId);
        }

        public void ExcludeBlockGames(List<GameCasinoListData> casinoGames, List<Provider> providers, IEnumerable<DisableTpGame> disableGames)
        {
            foreach (var disableGame in disableGames)
            {
                if (disableGame.GameList != null && disableGame.GameList.Any())
                {
                    foreach (var gameId in disableGame.GameList)
                    {
                        var excludeGame = casinoGames.Find(a=>a.GameId == gameId);
                        if (excludeGame != null)
                            casinoGames.Remove(excludeGame);
                        
                    }
                }
                else
                {
                    var excludeTp = providers.Find(a => a.Value == disableGame.TpId);
                    if(excludeTp != null)
                        providers.Remove(excludeTp);
                }
            }
        }
    }
}
