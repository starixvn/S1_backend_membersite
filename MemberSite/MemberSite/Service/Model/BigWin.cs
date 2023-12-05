namespace MemberSite.Service.Model
{
    public class BigWin
    {
        public int TpId { get; set; }
        public int TpGameType { get; set; }
        public int TpCategory { get; set; }
        public string GameID { get; set; }
        public decimal WinlostAmount { get; set; }
        public string MemberID { get; set; }
        public DateTime Time { get; set; }
    }
    public class BigWinPopUp
    {
        public int TpId { get; set; }
        public string GameId { get; set; }
        public int TpGameType { get; set; }
        public int TpCategory { get; set; }
        public string GameName { get; set; }
        public string UserName { get; set; }
        public decimal Amount { get; set; }
    }
}
