using MemberSite.Extensions;
using MemberSite.Service.Model.enums;
using System.Text.Json.Serialization;

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
        public string Code { get; set; }
        public string Serial { get; set; }
        public decimal Amount { get; set; }
        public decimal ActualDeposit { get; set; }
        public string Memo { get; set; }
        public string PostScript { get; set; }
        public int Status { get; set; }
    }

    public class DepositInfo3_1 : IDepositInfo
    {
        public int Ver { get; set; }
        public int SysId { get; set; }
        public string AccountName { get; set; }
        public string AccountNo { get; set; }
        public string IFSCCode { get; set; }
        public int CurrencyId { get; set; }
        public bool IsLocalPay { get; set; }
        public int GroupId { get; set; }
        public string Code { get; set; }
    }

    public class DepositRep3
    {
        public string BankName { set; get; }
        public string BankLogo { set; get; }
        public string BankSite { set; get; }
        public string AccountNo { get; set; }
        public string AccountName { get; set; }
        public string IFSCCode { get; set; }
        public string TransId { get; set; }
        public string PostScript { get; set; }
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
    }

    public class LocalPayResp
    {

        public string TransId { get; set; }
        public decimal Amount { get; set; }
        public string ReturnUrl { set; get; }
        public string Code { set; get; }
        public string BankLogo { set; get; }
    }

    public class DepositInfoHistory
    {
        public DateTimeOffset CreateTime
        {
            get
            {
                return _createTime.ToDateTimeOffset(CustTimeZone);
            }
            set
            {
                _createTime = value.DateTime;
            }
        }
        private DateTime _createTime { get; set; }
        public int DepositType { get; set; }
        public string TransId { get; set; }
        public decimal Amount { get; set; }
        //public decimal ActualDeposit { get; set; }
        public int Status { get; set; }
        //public string PostScript { get; set; }
        [JsonIgnore]
        public string DepositInfo { get; set; }
        [JsonIgnore]
        public string Memo { get; set; }
        public dynamic DepositRep { get; set; }
        public BankInfoSys BankInfo { get; set; }
        public string ThirdId { get; set; }
        public string RefId { get; set; }
        public DateTimeOffset? DepositTime
        {
            get
            {
                return _depositTime != null ? _depositTime.Value.ToDateTimeOffset(CustTimeZone) : null;
            }
            set
            {
                _depositTime = value?.DateTime;
            }
        }
        private DateTime? _depositTime { get; set; }

        public string PicFileName { get; set; }

        [JsonIgnore]
        public string CustTimeZone { get; set; }
        public string DepositMessage { get; set; }
        public IEnumerable<DepositMessageDetail> DepositMessageDetail { get; set; }
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
    public class DepositFromInfo
    {
        public int Channel { get; set; }
        public int SysId { get; set; }
        public string AccountNo { get; set; }
        public string IFSCCode { get; set; }
    }

    public class ExchangeInfo
    {
        public decimal Rate { get; set; }
        public decimal To_Amount { get; set; }
        public long Updated_timestamp { get; set; }
    }

    public class PaymentReq
    {
        public ExchangeInfo formData { get; set; }
    }

    public class DepositMessage
    {
        public DepositMessageStatus Status { get; set; }
        private DateTimeOffset _createTime { get; set; }
        public DateTimeOffset CreateTime
        {
            get => _createTime;
            set => _createTime = new DateTimeOffset(value.DateTime, TimeSpan.Zero);
        }
    }
    public class DepositMessageDetail
    {
        public string ErrorTypeName { get; set; }
        public string Message { get; set; }
        public DateTimeOffset CreateTime { get; set; }
    }

    public class DepositReqLocalPayModel
    {
        public int CustId { get; set; }
        public decimal Amount { get; set; }
        public int BankId { get; set; }
        public int GroupId { get; set; }
        public DepositType Type { get; set; }
    }
}
