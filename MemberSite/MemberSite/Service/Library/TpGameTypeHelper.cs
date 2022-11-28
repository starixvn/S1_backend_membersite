using MemberSite.Service.Model;

namespace MemberSite.Service.Library
{
    public class TpGameTypeHelper
    {
        private readonly Dictionary<string, List<TpGameTypeOption>> tpGameTypeConfig;

        private readonly Dictionary<string, Func<BannerV3, int, int>> _tpHandler;

        public TpGameTypeHelper(IServiceProvider provider)
        {
            var tpGameTypeOptions = provider.GetRequiredService<IEnumerable<TpGameTypeOption>>();
            tpGameTypeConfig = tpGameTypeOptions.GroupBy(item => item.TpId)
                .ToDictionary(item => item.Key.ToString(), item => item.ToList());
            _tpHandler = new Dictionary<string, Func<BannerV3, int, int>>()
            {
                { "1", GetSabaGameType},
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

                if (!tpGameTypeConfig.TryGetValue(banner.TpId, out var typeList))
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
            if (banner.TpId == "1" && int.TryParse(banner.GameId, out var gameId))
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
            return gameId switch
            {
                // OW Number Game
                161 => (2, 6),
                164 => (2, 6),
                168 => (2, 6),
                //OW Virtual Sports
                180 => (3, 7),
                181 => (3, 7),
                182 => (3, 7),
                183 => (3, 7),
                184 => (3, 7),
                185 => (3, 7),
                186 => (3, 7),
                //OW RNG
                208 => (4, 2),
                //OW Keno
                202 => (5, 6),
                //OW Lottery
                220 => (19, 6),
                //OW Togel 4D
                223 => (20, 6),
                //OW Card Club
                165 => (21, 2),
                //OW Virtual Games
                245 => (22, 2),
                //OW Table Game
                222 => (23, 2),
                //OW Virtual Sports 2
                190 => (24, 7),
                191 => (24, 7),
                192 => (24, 7),
                193 => (24, 7),
                194 => (24, 7),
                195 => (24, 7),
                196 => (24, 7),
                197 => (24, 7),
                199 => (24, 7),
                //OW Saba Club
                200 => (25, 2),
                //OW Macau Game
                212 => (26, 2),
                //OW China Lottery
                56 => (38, 6),
                //OW Vgaming
                171 => (59, 2),
                _ => (1, 1),
            };
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
