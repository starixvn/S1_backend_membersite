using Newtonsoft.Json;

namespace MemberSite.Service.Model.Game
{
    public class SlotGameInfoDTO
    {
        public string SlotGameId { get; set; }
        public int TpId { get; set; }
        public string GameId { get; set; }
        public string AppId { get; set; }
        public DateTime LaunchDate { get; set; }
        public string Reels { get; set; }
        public string RTP { get; set; }
        public string Paylines { get; set; }
        public bool PlayFree { get; set; }
        public Dictionary<string, string> GameName 
        {
            get 
            {
                return string.IsNullOrEmpty(Name) ? default : JsonConvert.DeserializeObject<Dictionary<string, string>>(Name);
            }
        }

        [JsonIgnore]
        public string Name { get; set; }

        public IEnumerable<int> SlotGameCategoryList
        {
            get
            {
                return string.IsNullOrEmpty(SlotGameCategory) ? default : JsonConvert.DeserializeObject<IEnumerable<int>>(SlotGameCategory);
            }
        }

        [JsonIgnore]
        public string SlotGameCategory { get; set; }

        public Dictionary<string, string> ImgName 
        {
            get 
            {
                if (GameName != null)
                {
                    var imgName = GameName.ContainsKey("en") ? GameName["en"] : string.Empty;
                    return GameName.ToDictionary(x => x.Key, x => $"{OrgFileNameFilter($"{imgName}-{x.Key}")}.jpg");
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
                                       .ToLower();
        }
    }

    public class SlotGameInfo
    {
        public SlotGameInfo() { }

        public SlotGameInfo(SlotGameInfoDTO slotGameInfoDTO)
        {
            SlotGameId = slotGameInfoDTO.SlotGameId;
            TpId = slotGameInfoDTO.TpId;
            GameId = slotGameInfoDTO.GameId;
            AppId = slotGameInfoDTO.AppId;
            LaunchDate = slotGameInfoDTO.LaunchDate;
            Reels = slotGameInfoDTO.Reels;
            RTP = slotGameInfoDTO.RTP;
            Paylines = slotGameInfoDTO.Paylines;
            PlayFree = slotGameInfoDTO.PlayFree;
            GameName = slotGameInfoDTO.GameName;
            SlotGameCategoryList = slotGameInfoDTO.SlotGameCategoryList;
            ImgName = slotGameInfoDTO.ImgName;
        }
        public string SlotGameId { get; set; }
        public int TpId { get; set; }
        public string GameId { get; set; }
        public string AppId { get; set; }
        public DateTime LaunchDate { get; set; }
        public string Reels { get; set; }
        public string RTP { get; set; }
        public string Paylines { get; set; }
        public bool PlayFree { get; set; }
        public Dictionary<string, string> GameName { get; set; }
        public IEnumerable<int> SlotGameCategoryList { get; set; }
        public Dictionary<string, string> ImgName { get; set; }
    }
}
