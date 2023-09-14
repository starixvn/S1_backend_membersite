using MemberSite.Extensions;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.Model.Game
{
    public class GetSlotGamePageRequest
    {
        /// <summary>
        /// 裝置類型
        /// </summary>
        public int DeviceType { get; set; }

        public int? CurrencyId { get; set; }

        /// <summary>
        /// 1	Featured
        /// 2	Jackpot
        /// 3	Table & Cards
        /// 4	Buy Features Games 
        /// 5	Megaways
        /// 6	Mini
        /// </summary>
        public IEnumerable<int> SlotGameCategoryList { get; set; } = new List<int>();
    }

    /// <summary>
    /// 前端parse json預設都是array，新增欄位可能會噴錯，新增要跟前端確認
    /// </summary>
    public class GetSlotGamePageResponse
    {
        public string ImgUrl { get; set; }
        public List<GameProvider> ProviderList { get; set; } = new List<GameProvider>();
        public List<SlotGameInfo> All { get; set; } = new List<SlotGameInfo>();
        public List<SlotGameInfo> Random { get; set; } = new List<SlotGameInfo>();
        public List<SlotGameInfo> RecentlyPlayed { get; set; } = new List<SlotGameInfo>();
        public List<SlotGameInfo> New { get; set; } = new List<SlotGameInfo>();
        public List<SlotGameInfo> Featured { get; set; } = new List<SlotGameInfo>();
        public List<SlotGameInfo> Jackpot { get; set; } = new List<SlotGameInfo>();
        public List<SlotGameInfo> TableAndCards { get; set; } = new List<SlotGameInfo>();
        public List<SlotGameInfo> BuyFeaturesGames { get; set; } = new List<SlotGameInfo>();
        public List<SlotGameInfo> Megaways { get; set; } = new List<SlotGameInfo>();
        public List<SlotGameInfo> MyFavorite { get; set; } = new List<SlotGameInfo>();
        public List<SlotGameInfo> Mini { get; set; } = new List<SlotGameInfo>();
        public GetSlotGamePageResponse()
        {

        }

        //先寫死nova88指定回傳的jackpot
        private readonly List<string> jackpotList = new()
        {
            "510250",
            "510017",
            "810069",
            "1010105",
            "3410345",
            "3410292",
            "3710016",
            "3810192",
            "3910015",
            "5110336"
        };

        /// <summary>
        /// 未登入回傳的SlotGamePage
        /// </summary>
        public GetSlotGamePageResponse(string imgUrl, List<GameProvider> providerList, List<SlotGameInfo> slotGameList, IEnumerable<int> slotGameCategoryList)
        {
            ImgUrl = imgUrl;
            ProviderList = providerList;
            SetSlotGameCategory(slotGameList, null, null, slotGameCategoryList);
        }

        /// <summary>
        /// 登入回傳的SlotGamePage
        /// </summary>
        public GetSlotGamePageResponse(string imgUrl, List<GameProvider> providerList, List<SlotGameInfo> slotGameList, List<UserGameInfo> favorite, List<UserGameInfo> recentlyPlayed, IEnumerable<DisableTpGame> disableGame, IEnumerable<int> slotGameCategoryList)
        {
            ImgUrl = imgUrl;
            ExcludeBlockGames(slotGameList, providerList, disableGame);

            ProviderList = providerList;
            SetSlotGameCategory(slotGameList, favorite, recentlyPlayed, slotGameCategoryList);
        }

        private void SetSlotGameCategory(List<SlotGameInfo> slotGameList, List<UserGameInfo> favorite, List<UserGameInfo> recentlyPlayed, IEnumerable<int> slotGameCategoryList)
        {
            // HomePageJackpot先拿來放寫死nova88指定回傳的jackpot
            var homePageJackpot = new List<SlotGameInfo>();

            //沒帶slotGameCategoryList會回傳全部
            var returnAllCategory = !slotGameCategoryList.Any();

            //沒有指定回傳Category才需要設定這兩個類別
            if (returnAllCategory)
            {
                All = slotGameList;
                Random = GetRandom(slotGameList);
            }

            var checkTime = DateTime.UtcNow.AddMonths(-1);

            //逐筆檢查每一個SlotGame，進行分類
            foreach (var game in slotGameList)
            {
                if (returnAllCategory && game.LaunchDate > checkTime)
                {
                    New.Add(game);
                }

                // MyFavorite
                if (returnAllCategory && favorite != null && favorite.Exists(i => i.TpId == game.TpId && i.GameId == game.GameId))
                {
                    MyFavorite.Add(game);
                }

                // RecentlyPlayed
                if (returnAllCategory && recentlyPlayed != null && recentlyPlayed.Exists(i => i.TpId == game.TpId && i.GameId == game.GameId))
                {
                    RecentlyPlayed.Add(game);
                }

                foreach (var category in game.SlotGameCategoryList)
                {
                    // Featured
                    if (returnAllCategory && category == (int)SlotGameCategory.Featured)
                    {
                        Featured.Add(game);
                    }
                    if (category == (int)SlotGameCategory.Featured && slotGameCategoryList.Contains(category))
                    {
                        Featured.Add(game);
                    }

                    // Jackpot
                    if (returnAllCategory && category == (int)SlotGameCategory.Jackpot) 
                    {             
                        Jackpot.Add(game);
                    }
                    if (category == (int)SlotGameCategory.Jackpot && slotGameCategoryList.Contains(category))
                    {
                        Jackpot.Add(game);
                    }

                    // TableAndCards
                    if (returnAllCategory && category == (int)SlotGameCategory.TableAndCards)
                    {
                        TableAndCards.Add(game);
                    }
                    if (category == (int)SlotGameCategory.TableAndCards && slotGameCategoryList.Contains(category))
                    {
                        TableAndCards.Add(game);
                    }

                    // BuyFeaturesGames
                    if (returnAllCategory && category == (int)SlotGameCategory.BuyFeaturesGames)
                    {
                        BuyFeaturesGames.Add(game);
                    }
                    if (category == (int)SlotGameCategory.BuyFeaturesGames && slotGameCategoryList.Contains(category))
                    {
                        BuyFeaturesGames.Add(game);
                    }

                    // Megaways
                    if (returnAllCategory && category == (int)SlotGameCategory.Megaways)
                    {
                        Megaways.Add(game);
                    }
                    if (category == (int)SlotGameCategory.Megaways && slotGameCategoryList.Contains(category))
                    {
                        Megaways.Add(game);
                    }

                    // Mini
                    if (returnAllCategory && category == (int)SlotGameCategory.Mini)
                    {
                        Mini.Add(game);
                    }
                    if (category == (int)SlotGameCategory.Mini && slotGameCategoryList.Contains(category))
                    {
                        Mini.Add(game);
                    }       
                }

                // HomePageJackpot先寫死nova88指定回傳的jackpot
                if (jackpotList.Contains(game.SlotGameId))
                {
                    homePageJackpot.Add(game);
                }
            }

            // 先寫死nova88指定回傳的jackpot
            if (!returnAllCategory && homePageJackpot.Any())
            {
                Jackpot = homePageJackpot;
            }
           
        }

        private List<SlotGameInfo> GetRandom(List<SlotGameInfo> slotGameList)
        {
            var result = new List<SlotGameInfo>();
            foreach (var tpGameList in slotGameList.GroupBy(x => x.TpId))
            {
                if (tpGameList.Any())
                {
                    result.Add(tpGameList.GetRandom());
                }
            }
            return result;
        }

        private void ExcludeBlockGames(List<SlotGameInfo> slotGameList, List<GameProvider> providers, IEnumerable<DisableTpGame> disableGames)
        {
            foreach (var disableGame in disableGames)
            {
                if (disableGame.GameList != null && disableGame.GameList.Any())
                {
                    foreach (var gameId in disableGame.GameList)
                    {
                        var excludeGame = slotGameList.Find(a => a.GameId == gameId && a.TpId == disableGame.TpId);
                        if (excludeGame != null)
                        {
                            slotGameList.Remove(excludeGame);
                        }
                    }
                }
                else
                {
                    var excludeTp = providers.Find(a => a.TpId == disableGame.TpId);
                    if (excludeTp != null)
                    {
                        providers.Remove(excludeTp);
                        slotGameList.RemoveAll(a => a.TpId == disableGame.TpId);
                    }
                }
            }
        }
    }



}
