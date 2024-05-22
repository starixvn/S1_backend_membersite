using System.Text.Json.Serialization;

using MemberSite.Service.Model.enums;

namespace MemberSite.Service.Model
{
    public class LinePay
    {
        public LinePay(string link)
        {
            Link = link;
        }

        public string Link { get; set; }
    }

    public class BankInfo : IBankNameLocalization
    {
        public int SysId { get; set; }
        public string BankName { get; set; }
        public string BankCode { get; set; }
        public string BankLogo { get; set; }
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
    }

    public class AutoChannelBankInfo : BankInfo
    {
        public DepositType DepositType { get; set; }
    }

    public class OfflineBankInfo : BankInfo
    {
        public bool CanSubmit { get; set; }
        public DepositType DepositType { get; set; }
        public OfflineBankAccount BankAccountInfo { get; set; }
    }

    public class OfflineBankAccount
    {
        public int BankId { get; set; }
        public string AccountName { get; set; }
        public string AccountNo { get; set; }
        public string QrCodeUrl { get; set; }
        public string Code { get; set; }
    }

    public class OnlineBankChannel : GenericPayChannel
    {
        public bool SelectBank { get; set; }
        public IEnumerable<BankInfo> SupportBanks { get; set; }
    }

    public class GenericPayGroup : IPaymentLimit
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; }
        public string DisplayGroupName { get; set; }
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
        [JsonIgnore]
        public int Sort { get; set; }
        public IEnumerable<GenericPayChannel> SupportChannels { get; set; }
    }

    public class GenericPayChannel : IPayInfo
    {
        public int PayId { get; set; }
        public string PayName { get; set; }
        public int GroupId { get; set; }
        public string GroupName { get; set; }
        public string DisplayGroupName { get; set; }
        public int ChannelId { get; set; }
        public string ChannelName { get; set; }
        public string DisplayChannelName { get; set; }
        public int Mode { get; set; }
        public int ExtraDataType { get; set; }
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
        public int AmountRule { get; set; }
        public IEnumerable<decimal> AmountList { get; set; }
        [JsonIgnore]
        public int GroupSort { get; set; }
        [JsonIgnore]
        public int ChannelSort { get; set; }
        public DepositType DepositType { get; set; }
    }

    public class PayMethod
    {
        public PayMethod(object data)
        {
            Data = data;
        }

        public object Data { get; set; }
    }
}