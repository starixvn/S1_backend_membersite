using MemberSite.Service.BLL.Interface;
using MemberSite.Service.DAL.Interface;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

using Newtonsoft.Json;

namespace MemberSite.Service.BLL
{
    public class HotGameBLL : IHotGameBLL
    {
        private readonly IHotGameService _hotGameService;
        private readonly ICacheHelper _redisHelper;

        public HotGameBLL(IHotGameService hotGameService, ICacheHelper redisHelper)
        {
            this._hotGameService = hotGameService;
            this._redisHelper = redisHelper;
        }

        public async Task<Dictionary<string, IEnumerable<HotGameResponse>>> GetHotGames(int siteId, int currencyId, HotGameDeviceType deviceType)
        {
            var key = $"GetHotGamesV2:{siteId}:{currencyId}";
            var hotGames = await _redisHelper.TryGetOrCreateAsync(key, null,
                async () => await _hotGameService.GetHotGames(siteId, currencyId));

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
