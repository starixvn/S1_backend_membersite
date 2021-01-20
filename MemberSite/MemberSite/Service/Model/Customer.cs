using MemberSite.Service.Model.enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MemberSite.Service.Model
{
    public class Customer
    {
        public int CustId { get; set; }
        public int SiteId { get; set; }
        public string UserName { get; set; }
        public string Userpwd { get; set; }
        public int CurrencyId { get; set; }
        public string UserLang { get; set; }
        public int ActStatus { get; set; }
        public int AgentId { get; set; }
        public string AllAgentId { get; set; }
        public int UserLevel { get; set; }
        public bool IsTest { get; set; }
        public DateTime LastLoginTime { get; set; }
        public DateTime RegTime { get; set; }
        public int Status { get; set; }
        public bool IsResetPwd { get; set; }
        public string Aff { get; set; }
        public string AffCode { get; set; }
    }

    public class CustProfile
    {
        public string RealName { get; set; }
        public string EMail { get; set; }
    }
    //{"Ver": 1, "SysId": 8, "AccountNo": "9876543210", "AccountName": "Eddie Wu"}
    public class CustBankInfo
    {
        public string AccountName { set; get; }
        [JsonIgnore]
        public string BankInfo { set; get; }
        public IEnumerable<BankInfoDetail> bankInfoDetail { set; get; }
    }

    public class BankInfoDetail
    {
        public int SysId { set; get; }
        public string AccountNo { set; get; }
    }

    public class BankInfoDetailRep : BankInfoDetail
    {
        public string BankName { set; get; }
        public string BankLogo { set; get; }
        public string BankSite { set; get; }
    }

    public class CustBankInfoRep
    {
        public string AccountName { set; get; }
        public IEnumerable<BankInfoDetailRep> bankInfoDetail { set; get; }
    }

    public class WalletInfo
    {
        public decimal Balance { set; get; }
        public string LastLoginTime { set; get; }
        public string LastTransaction { set; get; }
        public bool IsHideBalance { set; get; }
        public IEnumerable<TpWallet> tpWallets { set; get; }
        public IEnumerable<BonusWallet> bonusWallets { set; get; }
    }

    public class CustInfoProfile
    {
        public string UserName { get; set; }
        public int CurrencyId { get; set; }
        public int UserLevel { get; set; }
        public string Agent { get; set; }
        public string UserLang { get; set; }
        public int ActStatus { get; set; }
        public bool IsTest { get; set; }
        public DateTime LastLoginTime { get; set; }
        public DateTime RegTime { get; set; }
        public string Email { get; set; }
        public string AccountName { get; set; }
        public string CountryCode { get; set; }
        public string AreaCode { get; set; }
        public string PhoneNo { get; set; }
        public string Birth_d { get; set; }
        public string Birth_m { get; set; }
        public string Birth_y { get; set; }
        public string RealName { get; set; }
        public bool PhoneVerified { get; set; }
    }
    public class CustRollOver
    {
        public decimal RollOver { get; set; }
        public decimal TurnOver { get; set; }
    }

    public class Rollover
    {
        public bool ReachedRollover { get; set; }
        public decimal Lack { get; set; }
    }
    public class TpWallet
    {
        public int TpId { set; get; }
        public string displayKey { get; set; }
        public string TpName { set; get; }
        public string Description { set; get; }
        public decimal Balance { set; get; }
        public decimal Bonus { set; get; }
        public bool Transfer { set; get; }
        public WalletStatus walletStatus { set; get; }
    }

    public class RrofileReq
    {
        public string AreaCode { get; set; }
        public string PhoneNo { get; set; }
    }

    public class RegCust
    {
        public int CustId { get; set; }
        public int ErrorCode { get; set; }
    }
    public class TpWalletList
    {
        public int TpId { set; get; }
        public string TpName { set; get; }
        public string displayKey { get; set; }
        public string Description { set; get; }
    }
    public class TpWalletData
    {
        public int TpId { set; get; }
        public decimal Balance { set; get; }
        public decimal Bonus { set; get; }
        public WalletStatus walletStatus { set; get; }
    }
    public class WalletBalance
    {
        public decimal Balance { set; get; }
        public string LastLoginTime { set; get; }
        public string LastTransaction { set; get; }
        public bool IsHideBalance { set; get; }
    }
    public class ReferralSetting
    {
        public int PeriodDays { get; set; }
        public decimal BonusAmount { get; set; }
        public decimal VaildBet { get; set; }
        public decimal Deposit { get; set; }
    }
    public class ReferralSummary
    {
        public int ReferralCount { get; set; }
        public int ConditionCount { get; set; }
        public int AmountCount { get; set; }
        public int AmountSum { get; set; }

    }
    public class ReferralFriend
    {
        public int CustId { get; set; }
        public string UserName { get; set; }
        public DateTime RegisterDate { get; set; }
        public int CurrencyId { get; set; }
        public decimal TotalDeposit { get; set; }
        public decimal DepositC { get; set; }
        public decimal TotalBet { get; set; }
        public decimal VaildBetC { get; set; }
        public decimal BonusAmount { get; set; }
        public int Status { get; set; }

    }
    public class CustTransSummary
    {
        public decimal DepositSum { get; set; }
        public int DepositCnt { get; set; }
        public decimal WithdrawlSum { get; set; }
        public int WithdrawlCnt { get; set; }
        public decimal EffectiveBetSum { get; set; }
        public decimal BonusSum { get; set; }
        public int BonusCnt { get; set; }
    }
}
