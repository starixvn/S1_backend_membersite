using System.ComponentModel;

namespace MemberSite.Service.Model.enums
{
    public enum WalletStatus
    {
        Normal = 1,
        UM = 2,
        Error = 3,
    }

    public enum CustActStatus
    {
        NonDepoist = 1,
        Depoisted = 2,
    }
    public enum CustStatus
    {
        Running = 1,
        Suspend = 2,
        Closed = 3,
        Lock = 4
    }

    public enum PaymentView
    {
        DepositReqOnlineBanking = 1,
        DepositReqMobilePay = 2,
        DepositReqCryptoPay = 3,
        DepositReqLoadCardPay = 4
    }

    public enum PayChannelMode
    {
        OpenView = 1,
        Redirect = 2,
        ShowQrCode = 3
    }

    public enum DepositType
    {
        OnlineBanking = 1,
        MobilePayment = 2,
        LocalBank = 3,
        CryptoPayment = 4,
        LoadCardPayment = 5,
        QRPayPayment = 6,
        BankAutoChannel = 7,
        CryptoWallet = 8
    }

    /// <summary>
    /// 
    /// </summary>
    public enum DepositStatus
    {
        Pending = 1,
        Paid = 2,/**/
        Approved = 3,
        Rejected = 4,
        Cancelled = 5,
        Expired = 6,
        OnHold = 7
    }

    /// <summary>
    /// 
    /// </summary>
    public enum WithdrawalStatus
    {
        Pending = 1,
        OnHold = 2,
        Verify = 3,
        Process = 4,
        Cancelled = 5,
        Approved = 6,
        Rejected = 7,
        ToKyc = 8,
        Error = 99
    }
    public enum WithdrawalType
    {
        Undefine = 0,
        OnlinePayment = 1,
        LocalBank = 2,
    }
    /// <summary>
    ///  1=已入账  2=支付中  3=等待出款  4=已出款
    /// </summary>
    public enum DepositState
    {

        /// <summary>
        /// 已入账
        /// </summary>
        AddedMoney = 1,

        /// <summary>
        /// 支付中
        /// </summary>
        Paying = 2,

        /// <summary>
        /// 等待审核
        /// </summary>
        WaitingOut = 3,

        /// <summary>
        /// 已出款
        /// </summary>
        PayedMoney = 4,

        /// <summary>
        /// 提现撤单
        /// </summary>
        DrawBack = 5,

        /// <summary>
        /// 支付订单已过期
        /// </summary>
        PayTimeOut = 6,

        /// <summary>
        /// 等待出款【审核通过】
        /// </summary>
        WaitingTransferMoney = 7,

        /// <summary>
        /// 支付中
        /// </summary>
        CasherPaying = 8,

        /// <summary>
        /// 第三方自动支付失败
        /// </summary>
        AutoPayError = 9,
    }

    public enum PromotionCategoryId
    {
        SlotsGame = 1,
        LiveCasino = 2,
        SabaSports = 4,
        Lottery = 5,
        ESports = 11
    }

    public enum TpCategoryId
    {
        Sports = 1,
        Casino = 2,
        LiveCasino = 3,
        Fishing = 4,
        Cockfighting = 5,
        Lottery = 6,
        VirtualSports = 7,
        P2P = 8,
        ESports = 11
    }

    public enum PromotionType
    {
        FstDeposit = 1,
        ReDeposit = 2,
        FreeBet = 3,
        WalletBonus = 5,
        Rebate = 6,
        Cashback = 7,
        StaticPage = 8
    }

    public enum BonusType
    {
        Percentage = 1,
        Amount = 2
    }
    public enum LockedType
    {
        FromBonus = 1,
        FromRequireAmount = 2
    }
    public enum BonusStatus
    {
        Running = 1,
        Canceled = 2,
        Expiry = 3,
        Fulfill = 4,
        transferOut = 5
    }
    public enum RollOverStatus
    {
        Running = 1,
        Fulfill = 2
    }
    public enum Tp
    {
        None = 0,
        Saba = 1,
        SA = 2,
        AG = 3,
        DG = 4,
        PP = 5,
        GPI = 6,
        YL = 7,
        YGG = 8,
        Joker = 9,
        SG = 10,
        KM = 11,
        Allbet = 12,
        OneGaming = 13,
        PG = 14,
        Sexy = 15,
        CQ9 = 16,
        CF = 17,
        EBet = 18,
        AWC = 19,
        WM = 20,
        Kiss918 = 21,
        Mega = 22,
        One789 = 23,
        Iconic = 24,
        V8 = 25,
        PT = 27,
        N2Live = 28,
        BTI = 29,
        AMB = 30,
        NineW = 31,
        JILI = 32,
        TCG = 33,
        MG = 34,
        MP = 35,
        SBSW = 41
    }
    public enum GameType
    {
        SlotsLobby = 1,
        LiveCasinoLobby = 2,
        LiveCasino = 3,
        Slots = 4,
        Fishing = 5
    }
    public enum IovationType
    {
        registration = 1,
        withdrawal = 2,
    }
    public enum IovationErrorcode : int
    {
        Success = 0,
        IovationNotSupport = 1301
    }

    public enum ShieldErrorCode
    {
        Success = 0,
        InvalidRequest = -1,
        Failed = 99,
    }
    public enum SpecialCode
    {
        Success = 0,
        SpecialCodeError = 99,
        SpecialCodeErrorForCheck = 1000,
        SpecialCodeVipLevelsOrBlockTagInvalid = 1001, // VipLevel 不符合 或是有 BlockTag
        PromoCodeHasBeenUsed = 3000
    }

    public enum SiteId
    {
        Nova88 = 1,
        Goal123 = 2,
        Bwing = 3
    }

    public enum MaintainType
    {
        Date,
        Day,
        Week,
        Month
    }

    public enum DataBase
    {
        Main, //ConnectionString
        Report,//ConnectionStringReport
        Wager,//ConnectionStringWager
        Log,//ConnectionStringLog
    }

    public enum RedisDataBase
    {
        Member = 0,
        Shared = 1,
        Tp = 2,
        Bonus = 3,
        Admin = 4,
        Payment = 5,
        Token = 6
    }

    public enum AppLinkAppType
    {
        Andriod = 1,
        IOS = 2
    }
    public enum AppLinkProtocolType
    {
        http = 1,
        https = 2
    }
    public enum NetworkType
    {
        ERC20,
        TRC20,
        ETH,
        BTC,
        BSC,
        BEP20,
        XRP,
        ADA
    }

    public enum ActStatus
    {
        NonDeposit = 1,
        HasDeposit = 2
    }
    public enum BonusInfoAtPage
    {
        Promotion = 0,
        Deposit = 1
    }
    public enum OTPType
    {
        SMS = 0,
        Email = 1
    }
    public enum CurrencyId
    {
        MYR = 2,
        USD = 3,
        THB = 4,
        CNY = 13,
        IDR = 15,
        VND = 51,
        INR = 61,
        KHUSD = 72,
        BDT = 74,
        USDT = 96
    }
    public enum DeviceId
    {
        Old = 0,
        Desktop = 1,
        H5 = 2,
        APP = 3
    }

    public enum CryptoCurrencyId
    {
        USDT = 96,
        USDC = 97,
        ETH = 98,
        BTC = 99,
        BNB = 100,
        BUSD = 101,
        DAI = 102,
        DOGE = 103,
        XRP = 104,
        EOS = 105,
        SHIB = 106,
        ADA = 107
    }
    public enum DeepLinkType
    {
        Android = 1,
        IOS = 2,
        Desktop = 3
    }
    public enum DeepLinkProtocolType
    {
        http = 1,
        https = 2
    }

    public enum TransferAction
    {
        Unknow = 0,
        Manual = 1,
        RestoreAll = 2
    }
    public enum WalletMode
    {
        Seamless = 0,
        Restore = 1,
        Manual = 2,
    }

    public enum PromotionDeviceType
    {
        All = 1,
        Web = 2,
        App = 3
    }

    public enum LineLoginErrorCode
    {
        Success = 0,

        MemberAccountAlreadyBinding = 1001,
        LineCallbackError = 1002,
        CodeInvaild = 1003,
        StateInvaid = 1004,

        NotFoundToken = 1005,
        FlowTypeInvaild = 1006,
        TokenExpired = 1007,

        IssueLineTokenFail = 2001,
        //GetLineTokenFail = 2002, //HttpCode!=200
        LineTokenInvaild = 2003,
        LineTokenNotFound = 2004,
        //GetLineUserProfileFail = 2005, //HttpCode!=200
        LineUserProfileInvaild = 2006,
        LineUserIdNotFound = 2007,
        LineGetUserIdException = 2009,

        RegisterAuthInvaild = 3001,
        RegisterLineProfileInvaild = 3002,
        LoginLineProfileInvaild = 3003,
        BindingAuthInvaild = 3004,
        BindingLineProfileInvaild = 3005,
        UndefineFlow = 3009,

        InsertLineProfileFail = 4001,
        InsertLineProfileException = 4009,

        IssueLineTokenException = 9001,
        CallBackException = 9002
    }
    public enum CustBankInfoLogStatus
    {
        Create = 1,
        Delete = 2,
        Lock = 3,
        Unlock = 4
    }
    public enum CustBankDetailStatus
    {
        Disable = 0,
        Able = 1,
    }

    public enum ShieldRiskTagged
    {
        IsBot = 1,
        IsIncognito = 2,
        IsEmulated = 3,
        IsTor = 4,
        IsAntiFingerprinting = 5,
        IsProxy = 6,
        RunningCloneApps = 7,
        RunningGpsSpoofers = 8,
        RunningVpnSpoofers = 9,
        AutoClickerEnabled = 10,
        IsDeviceMasked = 11,
        IsJailBroken = 12,
        AppTampering = 13,
        SuspiciousFactoryReset = 14,
        VirtualOs = 15,
        Hooking = 16
    }

    public enum ShieldRiskTriggeredPoint
    {
        Registration = 1,
        Login = 2,
        Deposit = 3,
        Withdrawal = 4,
        ClaimBonus = 5,
    }

    public enum ShieldRiskTaggedIssue
    {
        HighRisk = 1
    }


    public enum WorldCupUserTicketStatus
    {
        Active = 1,
        Finished = 2,
        UnStarted = 3
    }
    public enum CustEWalletDetailLogStatus
    {
        Create = 1,
        Delete = 2,
        Lock = 3,
        Unlock = 4
    }
    public enum CustEWalletDetailStatus
    {
        Disable = 0,
        Able = 1,
    }

    public enum WithdrawalTypeStatus
    {
        LocalBank = 0,
        Crypto = 1,
        EWallet = 2
    }

    public enum EWalletStatus
    {
        Disable = 0,
        Able = 1,
    }

    public enum RedemptionErrorCode
    {
        InsufficientPoint = -1001,
        RedemptionLimitOverLimit = -1002
    }


    public enum PointTypeEnum
    {
        Deposit = 1,
        Turnover = 2,
        /// <summary>
        /// 兌換物品
        /// </summary>
        Redemption = 3,
        /// <summary>
        /// 手動
        /// </summary>
        Manual = 4,
        System = 5
    }

    public enum OfflineBankAccountPurposeEnum
    {
        Deposit = 1,
        Withdrawal = 2,
        Transfer = 3
    }

    public enum OfflineBankAccountSupportChannelEnum
    {
        LocalBank = 0,
        EWallet = 1,
        BankQR = 2
    }

    public enum PayModeEnum
    {
        //開新視窗，將html內容置入
        Html = 1,
        //開新視窗，導頁至該Url
        Redirect = 2,
        //將 url 以QrCode 呈現
        QrCode = 3
    }

    public enum HotGameTopCategory
    {
        HotGame = 1,
        NewGame = 2,
        Progressive = 3,
        Featured = 4,
        TopCasino = 5
    }

    public enum HotGameDeviceType
    {
        Pc = 1,
        Mobile = 2
    }

    public enum DepositMessageStatus
    {
        DepositorsNameUnmatch = 0,
        DepositSlipRequired = 1,
        PaymentNotReceived = 2,
        DepositRefunded = 3,
        WrongAmountSubmitted = 4,
        WrongBankSubmitted = 5,
        InactiveBankAccountSubmission = 6
    }
    public enum WithdrawalMessageStatus
    {
        WithdrawalAccountsNameUnmatch = 0,
        WithdrawalAccountNumberIncorrect = 1,
        WithdrawalAccountAannotReceiveMoney = 2,
        BeneficiarysAccountMaintenance = 3
    }

    public enum CustTagEnum
    {
        Duplicate = 6
    }

    public enum CustCryptoWalletDetailStatus
    {
        Disable = 0,
        Able = 1,
    }

    public enum CustCryptoWalletDetailLogStatus
    {
        Create = 1,
        Delete = 2,
        Lock = 3,
        Unlock = 4
    }
    
    public enum ExchangeTypeEnum
    {
        /// <summary>
        /// 我方賣出(玩家提款)
        /// </summary>
        Sell = 0,
        /// <summary>
        /// 我方買入(玩家存款)
        /// </summary>
        Buy = 1
    }
    
    public enum WithdrawalLimitLogCreateFromEnum 
    {
        Default = 0,
        DrawPrize = 7
    }
}
