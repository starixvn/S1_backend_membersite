using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;

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



    }
}
