using MemberSite.Service.Model.enums;

namespace MemberSite.Service.Model
{
    public class HotGame
    {
        public int TpId { get; set; }
        public string GameId { get; set; }
        public int TpGameType { get; set; }
        public int TpCategory { get; set; }
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
        public int TpCategory { get; set; }
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


    public class GetHotGameDataListRequest
    {
        public HotGameDeviceType DeviceType { get; set; }
        public int? CurrencyId { get; set; }
    }


    public class HotGameDto
    {
        public int TpId { get; set; }
        public string GameId { get; set; }
        public int TpGameType { get; set; }
        public int TpCategory { get; set; }
        public string GameName { get; set; }
        public string GameLogo { get; set; }
        public string TpName { get; set; }
        public string TpLogo { get; set; }
        public int Sort { get; set; }
        public HotGameTopCategory TopCategory { get; set; }
    }


    public class HotGameResponse
    {
        public int TpId { get; set; }
        public string GameId { get; set; }
        public int TpGameType { get; set; }
        public int TpCategory { get; set; }
        public string GameName { get; set; }
        public string GameLogo { get; set; }
        public string TpName { get; set; }
        public string TpLogo { get; set; }
        public int Sort { get; set; }
    }
}