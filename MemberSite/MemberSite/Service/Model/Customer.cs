using System.Text.Json.Serialization;

using MemberSite.Extensions;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.Model
{
    public class Customer
    {
        public string SiteName { get; set; }
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
        public bool PhoneVerified { get; set; }
        public bool IsResetPwd { get; set; }
        public string PhoneNo { get; set; }
        public string AreaCode { get; set; }
        public string AccountName { get; set; }
        public string Aff { get; set; }
        public string AffCode { get; set; }
        public string Token { get; set; }
        public string TagList { get; set; }
    }

    //{"Ver": 1, "SysId": 8, "AccountNo": "9876543210", "AccountName": "Eddie Wu"}
    public class CustBankInfo
    {
        public string AccountName { set; get; }
        public IEnumerable<BankInfoDetail> bankInfoDetail { set; get; }
        public IEnumerable<CryptoWalletDetail> usdtDetail { set; get; }
    }

    public class BankInfoDetail
    {
        public int SysId { set; get; }
        public string AccountNo { set; get; }
        public string IFSCCode { set; get; }
    }
    public class BankInfoDetailData
    {
        public int CustId { get; set; }
        public int SysId { set; get; }
        public string AccountNo { set; get; }
        public string IFSCCode { set; get; }
        public int Status { get; set; }
    }
    public class CryptoWalletDetail
    {
        public string NickName { set; get; }
        public string USDTAddress { set; get; }
        public string Network { set; get; }
        public int CurrencyId { set; get; }
        public string CurrencyName { set; get; }
        public bool Disabled { set; get; }
    }

    public class CryptoWalletDetailLog : CryptoWalletDetail
    {
        public int Status { set; get; }
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
        public string DefAccountNo { set; get; }
        public IEnumerable<BankInfoDetailRep> bankInfoDetail { set; get; }
        public IEnumerable<CryptoWalletDetail> uSDTDetail { set; get; }
    }

    public class EWalletInfoDetail
    {
        public int SysId { set; get; }
        public string AccountNo { set; get; }
    }
    public class EWalletInfoDetailData : EWalletInfoDetail
    {
        public int CustId { set; get; }
    }

    public class EWalletInfoDetailRep : EWalletInfoDetail
    {
        public string EWalletName { set; get; }
        public string EWalletLogo { set; get; }
        public string EWalletSite { set; get; }
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
        public decimal PhoneReward { get; set; }
        public bool EmailVerified { get; set; }
        public decimal EmailReward { get; set; }
        public int EnablePromotion { get; set; }
        public string AddressCountry { get; set; }
        public string AddressState { get; set; }
        public string AddressPostCode { get; set; }
        public string AddressCity { get; set; }
        public string AddressLineFirst { get; set; }
        public string AddressLineSecond { get; set; }
        public bool IsResetPwd { get; set; }
        public MemberTypeEnum MemberType { get; set; }
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

    public class TpWalletList
    {
        public int TpId { set; get; }
        public string TpName { set; get; }
        public string displayKey { get; set; }
        public string Description { set; get; }
        public int WalletMode { set; get; } //0.Semless Wallet 1.Restore All 2.Manual
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
        public bool Status { get; set; }
    }

    public class ReferralSettingDetailData
    {
        public decimal BonusAmount { get; set; }
        public int InvitationLimit { get; set; }
        public decimal AgentValidBet { get; set; }
        public decimal AgentDeposit { get; set; }
        public decimal ReferralFreeCredit { get; set; }
        public decimal VaildBet { get; set; }
        public decimal Deposit { get; set; }
        public int PeriodDays { get; set; }
        public decimal DepositRewardPercentage { get; set; }
        public decimal MinPayout { get; set; }
        public decimal MaxPayout { get; set; }
        public string Details { get; set; }
        public ReferralStatusEnum Status { get; set; }
    }

    public class ReferralSettingDetail
    {
        public decimal ReferralReward { get; set; }
        public int InvitationLimit { get; set; }
        public decimal AgentValidBet { get; set; }
        public decimal AgentDeposit { get; set; }
        public decimal MemberReward { get; set; }
        public decimal VaildBet { get; set; }
        public decimal Deposit { get; set; }
        public int PeriodDays { get; set; }
        public decimal DepositRewardPercentage { get; set; }
        public decimal MinPayout { get; set; }
        public decimal MaxPayout { get; set; }
        public ReferralContent ReferralContent { get; set; }
    }

    public class ReferralContent
    {
        public string LangId { get; set; }
        public string BannerUrl { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Detail { get; set; }
        public ReferralTypeEnum Type { get; set; }
    }

    public class ReferralSummary
    {
        public int ReferralCount { get; set; }
        public int ConditionCount { get; set; }
        public int AmountCount { get; set; }
        public int AmountSum { get; set; }
        public bool HasReferral { get; set; }

    }
    public class ReferralFriend
    {
        public int CustId { get; set; }
        public string UserName { get; set; }
        public DateTimeOffset RegisterDate
        {
            get
            {
                return _registerDate.ToDateTimeOffset(CustTimeZone);
            }
            set
            {
                _registerDate = value.DateTime;
            }
        }
        private DateTime _registerDate { get; set; }

        public int CurrencyId { get; set; }
        public decimal TotalDeposit { get; set; }
        public decimal DepositC { get; set; }
        public decimal TotalBet { get; set; }
        public decimal VaildBetC { get; set; }
        public decimal BonusAmount { get; set; }
        public int Status { get; set; }

        [JsonIgnore]
        public string CustTimeZone { get; set; }

    }
    public class DepositReferralDao
    {
        public DateTime SettledDate { get; set; }
        public string UserName { get; set; }
        public string CurrencyName { get; set; }
        public decimal TotalDeposit { get; set; }
        public decimal RewardAmount { get; set; }

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

    public class OnUser
    {
        public bool Exists { get; set; }
    }

    public class CustTagList
    {
        public int CustId { get; set; }

        public string StrTagList { get; set; }

        public IEnumerable<int> TagList { get; set; }
    }

    public class CustAccountNameInfo : CustTagList
    {
        public string AccountName { get; set; }
    }

    public class CryptoWalletDetailData : CryptoWalletDetail
    {
        public int CustId { set; get; }
    }

    public class CustTpGameTypeSettingDto
    {
        public int CategoryId { set; get; }
        public int TpId { set; get; }
        public int GameType { set; get; }
        public CustTpGameTypeSettingType Type { set; get; }
    }

    public class CustTpGameTypeSetting
    {
        public int CategoryId { get; set; }
        public int TpId { set; get; }
        public int GameType { set; get; }
        public bool NoShow { set; get; }
        public bool NoPlay { set; get; }

        public CustTpGameTypeSetting(int categoryId, int tpId, int gameType, bool noShow, bool noPlay)
        {
            CategoryId = categoryId;
            TpId = tpId;
            GameType = gameType;
            NoShow = noShow;
            NoPlay = noPlay;
        }
    }

    public class CustRecentlyViewedInfo
    {
        public int CustId { set; get; }
        public string RecentlyViewed { set; get; }
        public DateTime UpdateTime { set; get; }
    }
    public class CustVIPProgress
    {
        public int CurrentVipLevel { get; set; }
        public decimal? UpDeposit { get; set; }
        public decimal ActualDeposit { get; set; }
        public decimal? DownDeposit { get; set; }
        public decimal? DownTurnover { get; set; }
    }
}
