using System.Text.Json.Serialization;

using MemberSite.Extensions;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.Model
{
    public class WithdrawalInfo
    {
        public int SysId { get; set; }
        public string AccountName { get; set; }
        public string AccountNo { get; set; }
        public string IFSCCode { get; set; }
    }
    public class WithdrawalCryptoInfo
    {
        public string AccountType { get; set; }
        public string AccountName { get; set; }
        public string NickName { get; set; }
        public string AccountNo { get; set; }
        public string Network { set; get; }
        public int CurrencyId { set; get; }
        /// <summary>
        /// 參考匯率
        /// </summary>
        public decimal TempExchangeRate { set; get; }
        /// <summary>
        /// 參考金額
        /// </summary>
        public decimal TempExchangeAmount { set; get; }
    }
    public class WithdrawalEWalletInfo
    {
        public int EWalletSysId { get; set; }
        public string EWalletName { get; set; }
        public string AccountNo { get; set; }
        public string AccountType { get; set; }
        public string AccountName { get; set; }
    }
    public class WithdrawalHistory
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
        public string TransId { get; set; }
        public decimal Amount { get; set; }
        public decimal ChargeFee { get; set; }
        public int Status { get; set; }
        [JsonIgnore]
        public string WithdrawalInfo { get; set; }
        [JsonIgnore]
        public string CustTimeZone { get; set; }
        public string Memo { get; set; }
        public bool CanCancel => this.Memo == "WithdrawalSubmission" ? false : true;
        public string WithdrawalMessage { get; set; }
        public IEnumerable<WithdrawalMessageDetail> WithdrawalMessageDetail { get; set; }
    }


    public class WithdrawalLimitInfo
    {
        public decimal WithdrawalLimit { get; set; }
        public decimal TurnOver { get; set; }
        public decimal Balance { get; set; }
    }

    public class WithdrawalLimitInfoInput
    {
        public int SiteId { get; set; }
        public int CurrencyId { get; set; }
        public int VipLevel { get; set; }
    }
    public class USDTWithdrawalLimitInfoInput : WithdrawalLimitInfoInput
    {
        public int CryptoCurrencyId { get; set; }

        public string Network { get; set; }
    }
    public class WithdrawalLimitInfoOutput
    {
        public decimal WithdrawalMin { get; set; }
        public decimal WithdrawalMax { get; set; }
        public decimal TotalWithdrawalMax { get; set; }
        public int Times { get; set; }

    }
    public class USDTWithdrawalLimitInfoOutput : WithdrawalLimitInfoOutput
    {
        public string Network { get; set; }
        public int CurrencyId { get; set; }
        public string CurrencyName { get; set; }
    }
    public class EWalletWithdrawalLimitInfoOutput
    {
        public decimal WithdrawalMin { get; set; }
        public decimal WithdrawalMax { get; set; }
        public EWalletStatus Status { get; set; }
    }

    public class WithdrawalLimitOutput
    {
        public decimal WithdrawalMin { get; set; }
        public bool Unlimited { get; set; }
        public decimal WithdrawalAvailableAmount { get; set; }
        public int MaxTimes { get; set; }
        public int WithdrawalTimes { get; set; }
        public List<CryptoWithdrawalLimitOutput> USDTList { get; set; }
        public decimal EWalletWithdrawalMin { get; set; }
        public bool EWalletUnlimited { get; set; }
        public decimal EWalletWithdrawalAvailableAmount { get; set; }
        public EWalletStatus EWalletStatus { get; set; }
        public decimal TotalWithdrawalMax { get; set; }
    }

    public class DailyCustomerWithdrawalInfoInput
    {
        public int CustId { get; set; }
    }

    public class DailyCustomerWithdrawalInfoOutput
    {
        public decimal Amount { get; set; }
        public int Count { get; set; }
    }

    public class CustomerWithdrawalInfoInput
    {
        public int CustId { get; set; }
    }

    public class CustomerWithdrawalInfoOutput
    {
        public bool WithdrawalExists { get; set; }
    }
    public class CryptoWithdrawalLimitOutput
    {
        public int CurrencyId { get; set; }
        public string CurrencyName { get; set; }
        public string Network { get; set; }
        public decimal WithdrawalMin { get; set; }
        public bool Unlimited { get; set; }
        public decimal WithdrawalAvailableAmount { get; set; }
        public int MaxTimes { get; set; }
        public int WithdrawalTimes { get; set; }
    }
    public class WithdrawalMessageDetail
    {
        public string ErrorTypeName { get; set; }
        public string Message { get; set; }
        public DateTimeOffset CreateTime { get; set; }
    }
    public class WithdrawalMessage
    {
        public WithdrawalMessageStatus Status { get; set; }
        private DateTimeOffset _createTime { get; set; }
        public DateTimeOffset CreateTime
        {
            get => _createTime;
            set => _createTime = new DateTimeOffset(value.DateTime, TimeSpan.Zero);
        }
    }

    public class AutoPaymentRequest
    {
        public string TransId { get; set; }
        public string Token { get; set; }
        public string Comment { get; set; }
    }
}
