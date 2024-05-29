using MemberSite.Service.Model.enums;

using Newtonsoft.Json;

namespace MemberSite.Service.Model
{
    public class BankInfoSys
    {
        public int SysId { set; get; }
        public int CurrencyId { set; get; }
        public string BankCode { set; get; }
        public string BankName { set; get; }
        public string BankLogo { set; get; }
        public string BankSite { set; get; }
        public GeneralStatusEnum Status { set; get; }
    }

    public class PaymentFront : IPaymentLimit
    {
        public int PayId { get; set; }
        public int ChannelId { get; set; }
        public string PayName { get; set; }
        public bool SelBank { get; set; }
        public int ExtraDataType { get; set; }
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
        public int Mode { get; set; }
        [JsonIgnore]
        public int AmountRule { get; set; }
        [JsonIgnore]
        public IEnumerable<BankInfoSys> BankList { get; set; }
    }

    public class SysBankInfoFront : IPaymentLimit
    {
        public int BankId { get; set; }
        public int SysId { get; set; }
        public string AccountNo { get; set; }
        public string AccountName { get; set; }
        public string BankCode { get; set; }
        public string BankName { get; set; }
        public string IFSCCode { get; set; }
        public string BankLogo { get; set; }
        public string BankSite { get; set; }
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
        public int Status { get; set; }

        public string QRCodeUrl { get; set; }

        public IEnumerable<OfflineBankAccountSupportChannelEnum> SupportChannel { get; set; }
        public IEnumerable<int> GroupIds { get; set; }
    }
    public class OfflineBankInfoFront : SysBankInfoFront
    {
        public OfflineBankInfoFront() { }

        public OfflineBankInfoFront(SysBankInfoFront bankInfo, bool canSubmit)
        {
            base.BankId = bankInfo.BankId;
            base.SysId = bankInfo.SysId;
            base.BankCode = bankInfo.BankCode;
            base.AccountNo = bankInfo.AccountNo;
            base.AccountName = bankInfo.AccountName;
            base.BankName = bankInfo.BankName;
            base.IFSCCode = bankInfo.IFSCCode;
            base.BankLogo = bankInfo.BankLogo;
            base.BankSite = bankInfo.BankSite;
            base.MinAmount = bankInfo.MinAmount;
            base.MaxAmount = bankInfo.MaxAmount;
            base.Status = bankInfo.Status;
            base.QRCodeUrl = bankInfo.QRCodeUrl;
            base.SupportChannel = bankInfo.SupportChannel;
            this.CanSubmit = canSubmit;
        }
        public bool CanSubmit { get; set; }
    }

    public class BankInfoName
    {
        public int SysId { set; get; }
        public string Lang { set; get; }
        public string BankName { set; get; }
    }

    public class BlockAccount
    {
        public int SysId { set; get; }
        public string AccountNum { get; set; }
        public int Status { set; get; }
        public string Memo { set; get; }
        public string UpdateOperator { set; get; }
        public DateTime UpdateTime { set; get; }

    }

    public class BlockCryptoWallet
    {
        public string Address { get; set; }
        public int Status { set; get; }
        public string Memo { set; get; }
        public string UpdateOperator { set; get; }
        public DateTime UpdateTime { set; get; }

    }
}
