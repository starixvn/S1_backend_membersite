using Newtonsoft.Json;

namespace MemberSite.Service.Model
{
    public class Provider
    {
        public short Value { get; set; }
        public string Name { get; set; }
        public string DisplayText { get; set; }
        public bool PlayFree { get; set; }
        public GameDesc GameList { get; set; }
        public string DisplayKey { get; set; }
    }

    public class GameInfo
    {
        public string ImgUrl { get; set; }
        public List<Provider> ProviderList { get; set; }
    }

    public class GameDesc
    {
        public string ImgUrl { get; set; }

        public List<Datum> Data { get; set; }
    }

    public partial class Datum
    {
        public Dictionary<string, string> Name { get; set; }
        public string AppId { get; set; }
        public string GameId { get; set; }
        public string Category1 { get; set; }
        public string Paylines { get; set; }
        public string Rtp { get; set; }
        public string Reels { get; set; }
        public Dictionary<string, string> ImgName { get; set; }
        public DateTime LaunchDate { get; set; }
        public bool Hot { get; set; }
        public bool New { get; set; }
        public bool ImageExist { get; set; }
        public int promotion { get; set; }
        public bool featured { get; set; }
        public bool PlayFree { get; set; }
    }
    public class GameCasinoListData
    {
        public string Name { get; set; }
        public string AppId { get; set; }
        public string GameId { get; set; }
        public string Paylines { get; set; }
        public string Rtp { get; set; }
        public string Reel { get; set; }
        public DateTime LaunchDate { get; set; }
        public bool Hot { get; set; }
        public bool New { get; set; }
        public bool ImageExist { get; set; }
        public bool promotion { get; set; }
        public bool featured { get; set; }
        public int TypeId { get; set; }
        public bool Fun { get; set; }
        public int TpId { get; set; }
        public int GameCasinoId { get; set; }
    }
    public class AffiliateAgent
    {
        public int CurrencyId { get; set; }
    }
    public class GameCasionByCurrencyAndSite
    {
        public bool Hot { get; set; }
        public bool Featured { get; set; }
        public bool Status { get; set; }
        public bool Promotion { get; set; }
        public string GameCasinoId { get; set; }
    }

    public class TpGameIdListData
    {
        public int TpGameIdNo { get; set; }
        public int TpId { get; set; }
        public int TpGameType { get; set; }
        public string GameId { get; set; }
        public string BetType { get; set; }
        public int Entrance { get; set; }
        public int TpGameIdStatus { get; set; }
        public int ImgLobbyCategoryId { get; set; }
        public string TypeName { get; set; }
        public int CategoryId { get; set; }
        public string DisplayName { get; set; }
        public string ImgTpName { get; set; }
        public string LobbyCategoryName { get; set; }
        public Dictionary<string, string> GameName
        {
            get
            {
                if (_gameName == null)
                {
                    return string.IsNullOrEmpty(Name) ? default : JsonConvert.DeserializeObject<Dictionary<string, string>>(Name);
                }
                return _gameName;
            }
            set
            {
                _gameName = value;
            }
        }
        private Dictionary<string, string> _gameName { get; set; }

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public string Name { get; set; }

        public string ImgName
        {
            get
            {
                if (GameName != null)
                {
                    var imgName = GameName.ContainsKey("en") ? GameName["en"] : string.Empty;
                    return $"{OrgFileNameFilter($"{imgName}")}.jpg";
                }
                return default;
            }
        }

        private static string OrgFileNameFilter(string fileName)
        {
            return fileName.Replace(" ", "-")
                           .Replace("&", "-")
                           .Replace(":", "-")
                           .Replace("'", "")
                           .Replace("’", "")
                           .Replace(".", "")
                           .Replace("™", "")
                           .Replace("---", "-")
                           .Replace("--", "-")
                           .Replace("\u200B", "")
                           .Replace(",", "")
                           .Replace("?", "")
                           .ToLower();
        }
    }
}
