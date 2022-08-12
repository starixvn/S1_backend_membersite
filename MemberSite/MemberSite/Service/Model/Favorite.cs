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

    public class UserGameInfo
    {
        public int TpId { get; set; }
        public string GameId { get; set; }
        public UserGameInfo(string gameInfo)
        {
            try
            {
                var game = gameInfo.Split('_');
                TpId = int.Parse(game[0]);
                GameId= game[1];
            }
            catch 
            {
                TpId = 0;
                GameId = string.Empty;
            }
        }
    }
}
