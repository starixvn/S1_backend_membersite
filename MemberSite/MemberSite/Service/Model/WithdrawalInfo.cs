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

    public class WithdrawalLimitInfo
    {
        public decimal WithdrawalLimit { get; set; }
        public decimal TurnOver { get; set; }
        public decimal Balance { get; set; }
    }

    public class WithdrawalKyc
    {
        public int CurrencyId { get; set; }
        public int SiteId { get; set; }
        public decimal Amount { get; set; }
    }
    public class WithdrawalLimitInfo1
    {
        public int SiteId { get; set; }
        public int CurrencyId { get; set; }
        public int VipLevel { get; set; }
        public decimal WithdrawalMin { get; set; }
        public decimal WithdrawalMax { get; set; }
    }
    public class WithdrawalMinInfo
    {
        public int WithdrawalMin { get; set; }
    }
}
