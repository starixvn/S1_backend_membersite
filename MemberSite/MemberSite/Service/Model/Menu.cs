namespace MemberSite.Service.Model
{
    public class MenuOfGame
    {
        public int MenuId { get; set; }
        public List<MenuItem> MenuItems { get; set; }
    }

    public class MenuItem
    {
        public int TpId { get; set; }
        public string GameId { get; set; }
        public int TpGameType { get; set; }
        public int Sort { get; set; }
    }

    public class MenuOfGameData
    {
        public int MenuId { get; set; }
        public int TpId { get; set; }
        public string GameId { get; set; }
        public int TpGameType { get; set; }
        public int Sort { get; set; }
    }
}