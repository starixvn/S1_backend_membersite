namespace MemberSite.Service.Model
{
    public class Telegram
    {
        public int error_code { get; set; }
        public string message { get; set; }
        public TelegramData Data { get; set; }
    }
    public class TelegramData
    {
        public string url { get; set; }
        public bool is_binding { get; set; }
    }
}
