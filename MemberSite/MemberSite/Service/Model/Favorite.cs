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
    }
}
