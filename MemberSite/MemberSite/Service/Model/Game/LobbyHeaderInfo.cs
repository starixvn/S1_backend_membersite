using MemberSite.Service.Model.enums;

namespace MemberSite.Service.Model.Game
{
    public class GetLobbyHeaderRequest
    {
        public int SiteId { get; set; }
        public int CurrencyId { get; set; }
    }

    public class GetLobbyGameRequest
    {
        public int SiteId { get; set; }
        public int CurrencyId { get; set; }
        public string LobbyCategoryIds { get; set; }
        public string LangId { get; set; }
    }

    public class LobbyHeaderResponse
    {
        public int LobbyCategoryId { get; set; }
        public IEnumerable<LobbyHeaderDetail> Data { get; set; }
    }

    public class LobbyGameResponse
    {
        public List<GameProvider> ProviderList { get; set; } = new List<GameProvider>();
        public List<LobbyHeaderData> All { get; set; } = new List<LobbyHeaderData>();
        public List<LobbyHeaderData> RecentlyPlayed { get; set; } = new List<LobbyHeaderData>();
        public List<LobbyHeaderData> New { get; set; } = new List<LobbyHeaderData>();
        public List<LobbyHeaderData> MyFavorite { get; set; } = new List<LobbyHeaderData>();

        public void SetLobbyCategory(List<LobbyHeaderData> slotGameList, List<UserGameInfo> favorite, List<UserGameInfo> recentlyPlayed)
        {
            var checkTime = DateTime.UtcNow.AddMonths(-1);
            All = slotGameList;

            foreach (var game in slotGameList)
            {
                if (game.LaunchDate > checkTime)
                {
                    New.Add(game);
                }

                // MyFavorite
                if (favorite != null && favorite.Exists(i => i.TpId == game.TpId && i.GameId == game.GameId))
                {
                    MyFavorite.Add(game);
                }

                // RecentlyPlayed
                if (recentlyPlayed != null && recentlyPlayed.Exists(i => i.TpId == game.TpId && i.GameId == game.GameId))
                {
                    RecentlyPlayed.Add(game);
                }
            }
        }
    }

    public class LobbyHeaderDetail
    {
        public string LobbyCategory { get; set; }
        public int TpId { get; set; }
        public string TpName { get; set; }
        public string GameName { get; set; }
        public int TpGameIdNo { get; set; }
        public string GameId { get; set; }
        public int TpGameType { get; set; }
        public string BetType { get; set; }
        public Dictionary<string, string> CurrencyGameName { get; set; }
        public HeaderIconTypeEnum IconType { get; set; }
        public DateTime IconStartTime { get; set; }
        public DateTime IconEndTime { get; set; }
        public string RwdHeaderPicUrl { get; set; }
        public string RwdLobbyPicUrl { get; set; }
        public string PcHeaderPicUrl { get; set; }
        public string PcLobbyPicUrl { get; set; }
        public string MobileHeaderPicUrl { get; set; }
        public string MobileLobbyPicUrl { get; set; }
        public int Sort { get; set; }
    }

    public class LobbyHeaderData
    {
        public int LobbyCategoryId { get; set; }
        public string LobbyCategory { get; set; }
        public int TpId { get; set; }
        public string TpName { get; set; }
        public int TpGameIdNo { get; set; }
        public string GameName { get; set; }
        public string CurrencyGameName { get; set; }
        public string GameId { get; set; }
        public int TpGameType { get; set; }
        public string TpCategory { get; set; }
        public string DisplayText { get; set; }
        public string BetType { get; set; }
        public HeaderIconTypeEnum IconType { get; set; }
        public DateTime IconStartTime { get; set; }
        public DateTime IconEndTime { get; set; }
        public DateTime LaunchDate { get; set; }
        public int Sort { get; set; }
        public string ImgGameName => GameName?.ToLower().Replace("\u00A0", "-").Replace(" ", "-").Replace(":", "").Replace("'", "-");
    }

    public class LobbyHeaderSequenceData
    {
        public int SiteId { get; set; }
        public int CurrencyId { get; set; }
        public string LobbyCategoryList { get; set; }
    }
}