using MemberSite.Service.DAL;
using MemberSite.Service.Model;
using Microsoft.Extensions.Caching.Memory;
using MemberSite.Service.Library;

namespace MemberSite.Service.BLL
{
    public class GameCasinoBll
    {
        private readonly GameCasinoService _gameCasinoService;
        private readonly StatementService _statementService;
        private readonly CacheHelper _cacheHelper;
        private readonly Dictionary<string, string> _changeDisplayName;

        public GameCasinoBll(IServiceProvider provider)
        {
            _gameCasinoService = provider.GetRequiredService<GameCasinoService>();
            _statementService = provider.GetRequiredService<StatementService>();
            _cacheHelper = provider.GetRequiredService<CacheHelper>();
            _changeDisplayName = new Dictionary<string, string>()
            {
              {"Iconic","AT" },
              {"Kiss918","918Kiss" },
            };
        }

        public List<GameCasinoListData> GetGameCasinoListByDevice(int deviceType)
        {
            var key = $"GetGameCasinoListByDevice_{deviceType}";
            return _cacheHelper.TryGetOrCreate(key,
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(1)),
                i => _gameCasinoService.GetGameCasinoListByDevice(deviceType));
        }
        public List<Provider> GetProviderList()
        {
            
            var provider = new List<Provider>();
            var tpList = _statementService.GetTpListWithCache().ToList();
            tpList.ForEach(x =>
            {
                provider.Add(new Provider {
                    Value = Convert.ToInt16(x.Id),
                    Name = x.TpName,
                    PlayFree = x.Fun,
                    DisplayText = _changeDisplayName.TryGetValue(x.TpName, out var changeName) ? changeName: x.TpName
                });
            });
            return provider;
        }
    }
}
