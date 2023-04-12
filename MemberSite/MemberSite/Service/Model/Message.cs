namespace MemberSite.Service.Model
{
    public class Msg
    {
        public int Id { get; set; }
        public int Type { get; set; }
        public int SenderCustId { get; set; }
        public string Sender { get; set; }
        public string Title { get; set; }
        public DateTimeOffset CreateTime { get; set; }
        public bool Flag { get; set; }
        public bool IsBatch { get; set; }
    }

    public class MsgContent
    {
        public string Content { get; set; }
    }

    public class Announcement
    {
        public int Id { get; set; }
        public string Subject { get; set; }
        public string Content { get; set; }
        public DateTime StartTime { get; set; }
    }
    
    public class Message
    {
        public Message(string sender, int custId, string langId, string title, string content)
        {
            Sender = sender;
            Title = title;
            Content = content;
            CustId = custId;
            LangId = langId;
        }
        public int CustId { get; }
        public string LangId { get; }
        public string Title { get; }
        public string Content { get; }
        public string Sender { get; }
    }

    public class PopupMsg
    {
        public int Id { get; set; }
        public string PopupMessage { get; set; }
        public int PageNumber { get; set; }
    }

    public class DeleteMsg
    {
        public int MsgId { get; set; }
        public bool IsBatch { get; set; }
    }
}
