using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemberSite.Service.Model
{
    public class MailConfig
    {
        public string MailSender { get; set; }
        public string MailHost { get; set; }
        public string MailID { get; set; }
        public string MailPwd { get; set; }
        public int Port { get; set; }
    }
}
