using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemberSite.Service.Model.enums
{
    public enum WalletStatus
    {
        Normal = 1,
        UM = 2,
        Error = 3,
    }
    public enum Direction
    {
        Add = 1,
        Deduct = 2
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
    }
    public enum DepositType
    {
        OnlineBanking = 1,
        MobilePayment = 2,
        WireTransfer = 3,
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

    public enum PromotionType
    {
        FstDeposit = 1,
        ReDeposit = 2,
        Rebate = 3,
        Special = 4
    }

    public enum BonusType
    {
        Percentage = 1,
        Amount = 2
    }
    public enum BonusStatus
    {
        Running = 1,
        Canceled = 2,
        Expiry = 3,
        Fulfill = 4,
    }
    public enum RollOverStatus
    {
        Running = 1,
        Fulfill = 2
    }
    public enum Tp
    {
        Saba = 1,
        SA = 2,
        AG = 3,
        DG = 4,
        GPI = 6,
        YL = 7,
        YGG = 8,
        Joker = 9
    }
    public enum GameType
    {
        SlotsLobby = 1,
        LiveCasinoLobby = 2,
        LiveCasino = 3,
        Slots = 4,
        Fishing = 5
    }
}
