using MemberSite.Service.Model.enums;

namespace MemberSite.Service.Model.Game
{
    public class GetLobbyHeaderRequest
    {
        public int SiteId { get; set; }
        public int CurrencyId { get; set; }
    }
    
    public class LobbyHeaderResponse
    {
        public int LobbyCategoryId { get; set; }
        public IEnumerable<LobbyHeaderDetail> Data { get; set; }
    }
    
    public class LobbyHeaderDetail
    {
        public string LobbyCategory { get; set; }
        public int TpId { get; set; }
        public string TpName { get; set; }
        public string ProductName { get; set; }
        public int TpGameIdNo { get; set; }
        public string GameId { get; set; }
        public int TpGameType { get; set; }
        public Dictionary<string,string> CurrencyGameName { get; set; }
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
        public string ProductName { get; set; }
        public string CurrencyGameName { get; set; }
        public string GameId { get; set; }
        public int TpGameType { get; set; }
        public HeaderIconTypeEnum IconType { get; set; }
        public DateTime IconStartTime { get; set; }
        public DateTime IconEndTime { get; set; }
        public int Sort { get; set; }
        public string ImgProductName => ProductName.ToLower().Replace(" ","-");
    }
    
    public class LobbyHeaderSequenceData
    {
        public int SiteId { get; set; }
        public int CurrencyId { get; set; }
        public string LobbyCategoryList { get; set; }
    }
}

