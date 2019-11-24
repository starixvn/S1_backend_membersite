using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemberSite.Service.Model
{
    public class AuthData
    {
        public int CustId { set; get; }
        public string UserName { set; get; }
        public string Token { set; get; }
        public string LangId { set; get; }
    }
}
