using MemberSite.Service.BLL.Interface;
using MemberSite.Service.DAL.Interface;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
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

        public async Task<Dictionary<string, IEnumerable<HotGameResponse>>> GetHotGames(int siteId, int currencyId, HotGameDeviceType deviceType)
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

        private string DeserializeGameLogo(string jsonGameLogo, HotGameDeviceType deviceType)
        {
            var gameLogo = JsonConvert.DeserializeObject<HotGameLogoUrl>(jsonGameLogo);
            return deviceType switch
            {
                HotGameDeviceType.Pc => gameLogo.HotGamePcUrl,
                HotGameDeviceType.Mobile => gameLogo.HotGameMobileUrl,
                _ => ""
            };
        }
    }
}
