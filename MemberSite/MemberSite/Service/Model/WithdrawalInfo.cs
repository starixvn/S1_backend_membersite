using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MemberSite.Service.Model
{
    public class WithdrawalInfo
    {
        public int SysId { get; set; }
        public string AccountName { get; set; }
        public string AccountNo { get; set; }
        public string IFSCCode { get; set; }
    }
    public class WithdrawalUSDTInfo
    {
        public string AccountType { get; set; }
        public string AccountName { get; set; }
        public string NickName { get; set; }
        public string AccountNo { get; set; }
        public string Network { set; get; }
        public int CurrencyId { set; get; }
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
    public class WithdrawalLimitInfoInput
    {
        public int SiteId { get; set; }
        public int CurrencyId { get; set; }
        public int VipLevel { get; set; }
    }
    public class USDTWithdrawalLimitInfoInput: WithdrawalLimitInfoInput
    {
        public int CryptoCurrencyId { get; set; }

        public string Network { get; set; }
    }
    public class WithdrawalLimitInfoOutput
    {
        public decimal WithdrawalMin { get; set; }
        public decimal WithdrawalMax { get; set; }
        
    }
    public class USDTWithdrawalLimitInfoOutput: WithdrawalLimitInfoOutput
    {
        public string Network { get; set; }
        public int CurrencyId { get; set; }
        public string CurrencyName { get; set; }
    }

    public class WithdrawalLimitOutput
    {
        public decimal WithdrawalMin { get; set; }
        public bool Unlimited { get; set; }
        public decimal WithdrawalAvailableAmount { get; set; }
        public List<CryptoWithdrawalLimitOutput> CryptoList { get; set; }
    }

    public class DailyCustomerWithdrawalInfoInput
    {
        public int CustId { get; set; }
    }

    public class DailyCustomerWithdrawalInfoOutput
    {
        public decimal Amount { get; set; }
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
    }
}
