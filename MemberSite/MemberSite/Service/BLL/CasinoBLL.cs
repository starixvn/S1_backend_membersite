using MemberSite.Service.DAL;
using MemberSite.Service.Model;
using MemberSite.Service.Library;
using MemberSite.Service.Model.Game;

namespace MemberSite.Service.BLL
{
    public class CasinoBll
    {
        private readonly CasinoService _casinoService;
        private readonly ICacheHelper _redisHelper;
        
        public CasinoBll(IServiceProvider provider)
        {
            _casinoService = provider.GetRequiredService<CasinoService>();
            _redisHelper = provider.GetRequiredService<ICacheHelper>();
        }

        public IEnumerable<MemberFavorite> GetFavoriteGames(int custId)
        {
            var data = _casinoService.GetFavoriteGames(custId);
            var result = new List<MemberFavorite>();
            if (data is { Favorite: { } })
            {
                var tmp = Newtonsoft.Json.JsonConvert.DeserializeObject<IEnumerable<string>>(data.Favorite);
                foreach (var gameInfo in tmp)
                {
                    var (providerId, gameId) = SplitGameInfo(gameInfo);
                    if (providerId != 0 && !string.IsNullOrEmpty(gameId))
                        result.Add(new MemberFavorite() { ProviderId = providerId, GameId = gameId });
                }
            }

            return result;
        }

        public void SetFavoriteGames(int custId, int providerId, string gameId, bool isRemove)
        {
            var gameInfo = $"{providerId}_{gameId}";
            _casinoService.SetFavoriteGames(custId, gameInfo, isRemove);
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

        public IEnumerable<int> GetProviderIdByCurrency(int currencyId, int siteId)
        {
            return _redisHelper.TryGetOrCreate($"ProviderIdByCurrency:{currencyId}:{siteId}", TimeSpan.FromMinutes(10),
                () => _casinoService.GetProviderIdByCurrency(currencyId, siteId));
        }
        public IEnumerable<GameCasionByCurrencyAndSite> GetGameCasinoByCurrencyAndSite(int currencyId, int siteId)
        {
           return _casinoService.GetGameCasinoByCurrencyAndSite(currencyId, siteId);
        }

        public IEnumerable<DisableTpGame> GetTpDisableUserByCustId(int custId)
        {
            return _casinoService.GetTpDisableUserByCustId(custId);
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

        /// <summary>
        /// Slot遊戲優先排序列表
        /// </summary>
        public async Task<List<SlotGameTopRankingInfo>> GetSlotGameTopRankingList()
        {
            var key = $"GetSlotGameTopRankingList";
            var result = await _redisHelper.TryGetOrCreateAsync(key,
                TimeSpan.FromHours(2),
                async () =>
                {
                    var result = await _casinoService.GetSlotGameTopRankingList();
                    return result;
                });
            return result.ToList();
        }

        /// <summary>
        /// 依條件取得Slot遊戲優先排序 (SiteId、CurrencyId)
        /// </summary>
        public async Task<List<SlotGameTopRankingInfo>> GetLobbyCategorySequenceByLobbyCategoryId(int siteId, int currencyId)
        {
            var list = await GetSlotGameTopRankingList();
            return list.Where(x => x.SiteId == siteId && x.CurrencyId == currencyId).ToList();
        }
    }
}
