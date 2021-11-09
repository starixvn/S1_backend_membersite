namespace MemberSite.Service.Model
{
    public class HotGame
    {
        public int TpId { get; set; }
        public string GameId { get; set; }
        public int TpGameType { get; set; }
        public string GameName { get; set; }
        public string GameLogo { get; set; }
        public string TpName { get; set; }
        public string TpLogo { get; set; }
    }
    
    public class HotGameData
    {
        public int TpId { get; set; }
        public string GameId { get; set; }
        public int TpGameType { get; set; }
        public string GameName { get; set; }
        public string GameLogo { get; set; }
        public string TpName { get; set; }
        public string TpLogo { get; set; }
    }

    public class HotGameLogoUrl
    {
        public string HotGamePcUrl { get; set; }
        public string HotGameMobileUrl { get; set; }
    }
}