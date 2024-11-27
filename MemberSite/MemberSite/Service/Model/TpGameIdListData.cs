using Newtonsoft.Json;

namespace MemberSite.Service.Model
{
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
                           .Replace("\u00A0", "-")
                           .Replace("\u200B", "")
                           .Replace(",", "")
                           .Replace("?", "")
                           .ToLower();
        }
    }
}
