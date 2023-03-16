using MemberSite.Service.Model;
using MemberSite.Service.Model.Options;

namespace MemberSite.Service.Library
{
    public class TpGameTypeHelper
    {
        private readonly Dictionary<string, List<TpGameTypeOption>> _tpGameTypeConfig;

        private readonly Dictionary<int, SabaSportTypeOption> _sportTypeConfig;

        private readonly Dictionary<string, Func<BannerV3, int, int>> _tpHandler;

        public TpGameTypeHelper(IServiceProvider provider)
        {
            _tpGameTypeConfig = provider.GetRequiredService<IEnumerable<TpGameTypeOption>>()
                .GroupBy(item => item.TpId)
                .ToDictionary(item => item.Key.ToString(), item => item.ToList());

            _sportTypeConfig = provider.GetRequiredService<IEnumerable<SabaSportTypeOption>>()
                .ToDictionary(item => item.SportType, item => item);

            _tpHandler = new Dictionary<string, Func<BannerV3, int, int>>()
            {
                { "41", GetSabaGameType},
                { "23", GetOne789GameType},
                { "25", GetV8GameType},
                { "33", GetTCGGameType},
                { "35", GetMPGameType}
            };        
        }

        public int GetTpGameType(BannerV3 banner, int currencyId)
        {
            try
            {          
                if (banner.ActionMode != 2 && banner.ActionMode != 6)//只有這兩種需要回傳tpGameType
                {
                    return 0;
                }

                if (!_tpGameTypeConfig.TryGetValue(banner.TpId, out var typeList))
                {
                    return -1;
                }

                //先用bannerType取得TpCategoryId
                var tpCategoryId = banner.BannerType == 1 ? MainBannerGetTpCategoryId(banner, typeList) : GetTpCategoryIdByBannerType(banner.BannerType);

                var result = typeList.Where(i => i.CategoryId == tpCategoryId);
                if (!result.Any())
                {
                    return 0;
                }

                //分類超過一筆以上依照TpId分別處理
                if (result.Count() > 1)
                {
                    return _tpHandler[banner.TpId].Invoke(banner, currencyId);
                }

                return result.First().Type;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public int GetTpCategoryIdByBannerType(int bannerType)
        {
            return bannerType switch
            {
                2 => 1,//HotEvent=>Sports
                3 => 2,//Slots=>Casino
                4 => 3,//Live Casino=>Live Casino
                5 => 4,//Fishing=>Fishing
                6 => 6,//Lottery=>Lottery
                _ => 0,
            };
        }

        private int MainBannerGetTpCategoryId(BannerV3 banner, List<TpGameTypeOption> typeList)
        {
            //Get by UrlRouter
            if (banner.UrlRouter?.IndexOf("sports") > -1)
            {
                return 1;
            }

            if (banner.UrlRouter?.IndexOf("casino") > -1)
            {
                return 2;
            }

            //Get by Saba 
            if (banner.TpId == "41" && int.TryParse(banner.GameId, out int gameId))
            {
                var (_, tpCategoryId) = GetSabaDetailByGameId(gameId);
                return tpCategoryId;
            }

            //Get By BannerName
            var result = typeList.FirstOrDefault(itme => banner.BannerName.Contains(itme.TypeName, StringComparison.OrdinalIgnoreCase));
            if (result != null)
            {
                return result.CategoryId;
            }

            //Get By TpId
            if (int.TryParse(banner.TpId, out var TpId) && TpId != 0)
            {
                return typeList.FirstOrDefault(itme => itme.TpId == TpId)?.CategoryId ?? 0;
            }

            return 0;
        }

        private int GetSabaGameType(BannerV3 banner, int currencyId)
        {
            if (!int.TryParse(banner.GameId, out var gameId))
            {
                return 1;
            }

            var (tpGameType, _) = GetSabaDetailByGameId(gameId);
            return tpGameType;
        }

        private (int tpGameType, int tpCategoryId) GetSabaDetailByGameId(int gameId)
        {
            if (!_sportTypeConfig.TryGetValue(gameId, out var sabaSportType))
            {
                return (0, 0);
            }

            return (sabaSportType.TpGameType, sabaSportType.TpCategoryId);
        }

        private int GetOne789GameType(BannerV3 banner, int currencyId)
        {
            return banner.GameId switch
            {
                "0" => 35,
                "1" => 36,
                "2" => 37,
                _ => 35,
            };
        }

        private int GetV8GameType(BannerV3 banner, int currencyId)
        {
            return 42;
        }

        private int GetTCGGameType(BannerV3 banner, int currencyId)
        {
            return currencyId switch
            {
                51 => 55,
                _ => 54,
            };
        }

        private int GetMPGameType(BannerV3 banner, int currencyId)
        {
            return 60;
        }
    }
}
