using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MemberSite.Service.Model
{

    public interface IDepositInfo
    {
        int Ver { get; set; }
        int CurrencyId { get; set; }
    }
    public class DepositDetailInfo
    {
        public int DepositId { get; set; }
        public string TransId { get; set; }
        public int DepositType { get; set; }
        public int CustId { get; set; }
        public string UserName { get; set; }
        public int CurrencyId { get; set; }
        public int SiteId { get; set; }
        public string DepositInfo { get; set; }
        public decimal Amount { get; set; }
        public decimal ActualDeposit { get; set; }
        public string Memo { get; set; }
        public string PostScript { get; set; }
        public int Status { get; set; }
    }
    public class DepositInfo1_1 : IDepositInfo
    {
        public int Ver { get; set; }
        public int CurrencyId { get; set; }
    }
    public class DepositInfo2_1 : IDepositInfo
    {
        public int Ver { get; set; }
        public int PayId { get; set; }
        public int ChannelId { get; set; }
        public int CurrencyId { get; set; }
    }
    public class DepositInfo3_1 : IDepositInfo
    {
        public int Ver { get; set; }
        public int SysId { get; set; }
        public string AccountName { get; set; }
        public string AccountNo { get; set; }
        public int CurrencyId { get; set; }
    }

    public class DepositRep3
    {
        public string BankName { set; get; }
        public string BankLogo { set; get; }
        public string BankSite { set; get; }
        public string AccountNo { get; set; }
        public string AccountName { get; set; }
        public string TransId { get; set; }
        public string PostScript { get; set; }
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
    }

    public class DepositInfoHistory
    {
        public DateTime CreateTime { get; set; }
        public int DepositType { get; set; }
        public string TransId { get; set; }
        public decimal Amount { get; set; }
        //public decimal ActualDeposit { get; set; }
        public int Status { get; set; }
        //public string PostScript { get; set; }
        [JsonIgnore]
        public string DepositInfo { get; set; }
        public dynamic DepositRep { get; set; }
        public BankInfoSys BankInfo { get; set; }
        public string RefId { get; set; }
        public DateTime? DepositTime { get; set; }
        public string PicFileName { get; set; }
    }

    public class DepositReceipt
    {
        public int DepositId { get; set; }
        public string PicFileName { get; set; }
        public byte[] DepositPic { get; set; }
    }

    public class DepositLimitInfo
    {
        public int DepositType { get; set; }
        public int SiteId { get; set; }
        public int CurrencyId { get; set; }
        public int VipLevel { get; set; }
        public decimal DepositMin { get; set; }
        public decimal DepositMax { get; set; }
        public int DepositStatus { get; set; }
    }
}
