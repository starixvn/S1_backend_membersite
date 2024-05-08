using MemberSite.Service.Model.enums;

namespace MemberSite.Service.Model
{
    public class BankInfoPay : IPaymentLimit
    {
        public int SysId { get; set; }
        public int CurrencyId { get; set; }
        public string SysBankCode { get; set; }
        public string SysBankName { get; set; }
        public string BankLogo { get; set; }
        public int PayId { get; set; }
        public string BankCode { get; set; }
        public string BankPayoutCode { get; set; }
        public string BankName { get; set; }
        public int ChannelId { get; set; }
        public string ChannelName { get; set; }
        public int Mode { get; set; }
        public int PointLevel { get; set; }
        public string PayName { get; set; }
        public string PayCode { get; set; }
        public ExtraDataTypeEnum ExtraDataType { get; set; }
        public int MSort { get; set; }
        public int SSort { get; set; }
        public bool SelBank { get; set; }
        public string PayCurrencyId { get; set; }
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
    }

    public class BankInfoPayFront : IPaymentLimit
    {
        public int SysId { get; set; }
        public int PayId { get; set; }
        public string BankLogo { get; set; }
        public string BankCode { get; set; }
        public string BankName { get; set; }
        public int ChannelId { get; set; }
        public string PayCode { get; set; }
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
        public int Mode { get; set; }
    }
}
