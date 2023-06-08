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

        /// <summary>
        /// 未登入回傳的SlotGamePage
        /// </summary>
        public GetSlotGamePageResponse(string imgUrl, List<GameProvider> providerList, List<SlotGameInfo> slotGameList)
        {
            ImgUrl = imgUrl;
            ProviderList = providerList;
            SetSlotGameCategory(slotGameList, null, null);
        }

        /// <summary>
        /// 登入回傳的SlotGamePage
        /// </summary>
        public GetSlotGamePageResponse(string imgUrl, List<GameProvider> providerList, List<SlotGameInfo> slotGameList, List<UserGameInfo> favorite, List<UserGameInfo> recentlyPlayed, IEnumerable<DisableTpGame> disableGame)
        {
            ImgUrl = imgUrl;
            ExcludeBlockGames(slotGameList, providerList, disableGame);

            ProviderList = providerList;
            SetSlotGameCategory(slotGameList, favorite, recentlyPlayed);
        }

        private void SetSlotGameCategory(List<SlotGameInfo> slotGameList, List<UserGameInfo> favorite, List<UserGameInfo> recentlyPlayed)
        {
            All = slotGameList;
            Random = GetRandom(slotGameList);

            var checkTime = DateTime.UtcNow.AddMonths(-1);
            foreach (var game in slotGameList)
            {
                if (game.LaunchDate > checkTime)
                {
                    New.Add(game);
                }

                if (favorite != null && favorite.Any(i => i.TpId == game.TpId && i.GameId == game.GameId))
                {
                    MyFavorite.Add(game);
                }

                if (recentlyPlayed != null && recentlyPlayed.Any(i => i.TpId == game.TpId && i.GameId == game.GameId))
                {
                    RecentlyPlayed.Add(game);
                }

                foreach (var category in game.SlotGameCategoryList)
                {
                    if (category == (int)SlotGameCategory.Featured)
                    {
                        Featured.Add(game);
                    }

                    if (category == (int)SlotGameCategory.Jackpot)
                    {
                        Jackpot.Add(game);
                    }

                    if (category == (int)SlotGameCategory.TableAndCards)
                    {
                        TableAndCards.Add(game);
                    }

                    if (category == (int)SlotGameCategory.BuyFeaturesGames)
                    {
                        BuyFeaturesGames.Add(game);
                    }

                    if (category == (int)SlotGameCategory.Megaways)
                    {
                        Megaways.Add(game);
                    }

                    if (category == (int)SlotGameCategory.Mini)
                    {
                        Mini.Add(game);
                    }
                }
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
