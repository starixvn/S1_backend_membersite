using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemberSite.Service.Model
{
    public class Evnets
    {
       
    }
    public class Leaderboard
    {
        public DateTime Date { get; set; }
        public int LBCategoryId { get; set; }
        public string LBCategoryName { get; set; }
        public int CustId { get; set; }
        public string UserName { get; set; }
        public decimal WTurnover { get; set; }
    }
    public class LeaderboardCustRank
    {
        public int CustRank { get; set; }
        public decimal point { get; set; }
    }
}
