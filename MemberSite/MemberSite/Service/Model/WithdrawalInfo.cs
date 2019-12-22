using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MemberSite.Service.Model
{
    public class WithdrawalInfo
    {
        public int SysId { get; set; }
        public string AccountName { get; set; }
        public string AccountNo { get; set; }
    }

    public class WithdrawalHistory
    {
        public DateTime CreateTime { get; set; }
        public string TransId { get; set; }
        public decimal Amount { get; set; }
        public decimal ChargeFee { get; set; }
        public int Status { get; set; }
        [JsonIgnore]
        public string WithdrawalInfo { get; set; }
    }
}
