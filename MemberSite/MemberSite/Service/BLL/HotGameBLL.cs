using MemberSite.Service.BLL.Interface;
using MemberSite.Service.DAL.Interface;
using MemberSite.Service.Model;
using Newtonsoft.Json;

namespace MemberSite.Service.BLL
{
    public class HotGameBLL : IHotGameBLL
    {
        private readonly IHotGameService _hotGameService;

        public HotGameBLL(IHotGameService hotGameService)
        {
            this._hotGameService = hotGameService;
        }

        public async Task<Dictionary<string, IEnumerable<HotGameResponse>>> GetHotGames(int siteId, int currencyId, int deviceType)
        {
            var hotGames = await _hotGameService.GetHotGames(siteId, currencyId);

            var result = hotGames.GroupBy(r => r.TopCategory.ToString())
                .ToDictionary(item => item.Key,
                    item => item.OrderBy(x => x.Sort)
                        .Select(r => new HotGameResponse()
                        {
                            TpId = r.TpId,
                            GameId = r.GameId,
                            TpGameType = r.TpGameType,
                            TpCategory = r.TpCategory,
                            GameName = r.GameName,
                            GameLogo = DeserializeGameLogo(r.GameLogo, deviceType),
                            TpName = r.TpName,
                            TpLogo = r.TpLogo,
                            Sort = r.Sort
                        }));

            return result;
        }

        private string DeserializeGameLogo(string jsonGameLogo, int device)
        {
            var gameLogo = JsonConvert.DeserializeObject<HotGameLogoUrl>(jsonGameLogo);
            return device switch
            {
                1 => gameLogo.HotGamePcUrl,
                2 => gameLogo.HotGameMobileUrl,
                _ => ""
            };
        }
    }
}
