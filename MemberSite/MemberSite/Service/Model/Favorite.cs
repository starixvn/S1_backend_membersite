using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemberSite.Service.Model
{
    public class MemberFavorite
    {
        public int CustId { get; set; }
        public int ProviderId { get; set; }
        public string GameId { get; set; }
        public bool IsRemove { get; set; }
    }
}
