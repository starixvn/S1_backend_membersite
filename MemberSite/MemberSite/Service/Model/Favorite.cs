namespace MemberSite.Service.Model
{
    public class MemberFavorite
    {
        public int CustId { get; set; }
        public int ProviderId { get; set; }
        public string GameId { get; set; }
        public bool IsRemove { get; set; }
    }

    public class FavoriteGames
    {
        public int CustId { get; set; }
        public string Favorite { get; set; }
        public string RecentlyPlayed { get; set; }
    }

    public class FavoriteAndRecentlyPlayed
    {
        public List<UserGameInfo> FavoriteList { get; set; } = new List<UserGameInfo>();
        public List<UserGameInfo> RecentlyPlayedList { get; set; } = new List<UserGameInfo>();
    }

    public class UserGameInfo
    {
        public int TpId { get; set; }
        public string GameId { get; set; }
        public UserGameInfo() { }
        public UserGameInfo(string gameInfo)
        {
            try
            {
                var underlineIndex = gameInfo.IndexOf('_');
                if (string.IsNullOrWhiteSpace(gameInfo) || underlineIndex == -1)
                {
                    TpId = 0;
                    GameId = string.Empty;
                }
                else
                {
                    TpId = int.Parse(gameInfo.Substring(0, underlineIndex));
                    GameId = gameInfo.Substring(underlineIndex + 1);
                }          
            }
            catch 
            {
                TpId = 0;
                GameId = string.Empty;
            }
        }
    }
}
