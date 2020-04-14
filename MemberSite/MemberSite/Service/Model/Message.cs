using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemberSite.Service.Model
{
    public class Msg
    {
        public int Id { get; set; }
        public int Type { get; set; }
        public int SenderCustId { get; set; }
        public string Sender { get; set; }
        public string Title { get; set; }
        public DateTime CreateTime { get; set; }
        public bool Flag { get; set; }
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
}
