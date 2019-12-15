using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemberSite.Service.Model.enums
{
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
        Paid = 2,
        Approved = 3,
        Rejected = 4,
        Canceled = 5,
        Expired = 6,
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
}
