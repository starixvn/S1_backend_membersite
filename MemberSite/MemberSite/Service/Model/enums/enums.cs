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
    public enum DepositType
    {
        OnlineBanking = 1,
        MobilePayment = 2,
        LocalBank = 3,
        CryptoPayment = 4,
        LoadCardPayment = 5,
        QRPayPayment = 6,
        BankAutoChannel = 7,
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
        Lottery = 5
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
        Poker = 8
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
        MP = 35
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
    public enum SpecialCodeErrorcode : int
    {
        Success = 0,
        SpecialCodeError = 99
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
        BTC
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
        RMB = 13,
        IDR = 15,
        VND = 51,
        INR = 61,
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
        BTC = 99
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
}
