namespace MemberSite.Service.Model.Game
{
    public class SlotGameInfo
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
        public Dictionary<string, string> Name { get; set; }
        public List<int> SlotGameCategory { get; set; }        
        public Dictionary<string, string> ImgName 
        {
            get 
            {
                var imgName = Name.ContainsKey("en") ? Name["en"] : string.Empty;
                return Name.ToDictionary(x => x.Key, x => $"{OrgFileNameFilter($"{imgName}-{x.Key}")}.jpg");
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
}
