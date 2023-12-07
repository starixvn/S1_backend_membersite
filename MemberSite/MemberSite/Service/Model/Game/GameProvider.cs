namespace MemberSite.Service.Model.Game
{
    public class GameProvider
    {
        public int TpId { get; set; }
        public string TpName { get; set; }
        public bool PlayFree { get; set; }
        public string DisplayText { get; set; }
        public string ImgName { get; set; }
        public string OpenPath { get; set; }
        public string GameImgDisplayText { get; set; }
        public int Sort { get; set; } = 99999;
        public int Type { get; set; }
    }
}
