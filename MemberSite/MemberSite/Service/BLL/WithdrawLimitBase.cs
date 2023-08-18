using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.BLL
{
    public static class WithdrawLimitFactory
    {
        public static WithdrawLimitBase Create(WithdrawalTypeStatus withdrawalType)
        {
            return withdrawalType switch
            {
                WithdrawalTypeStatus.EWallet => new EWalletWithdrawLimit(),
                _ => throw new ArgumentOutOfRangeException(nameof(withdrawalType), withdrawalType, @"withdrawal type error")
            };
        }
    }

    public abstract class WithdrawLimitBase
    {
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
        public decimal DailyTotalWithdrawalMax { get; set; }
        public int DailyWithdrawCount { get; set; }

        public abstract int CheckWithdrawLimit(EWalletWithdrawalLimitInfoOutput eWalletWithdrawalLimitInfo, WithdrawalLimitInfoOutput withdrawalLimitInfo, DailyCustomerWithdrawalInfoOutput dailyCustomerWithdrawalInfo, decimal amount);
    }

    public class EWalletWithdrawLimit : WithdrawLimitBase
    {
        public override int CheckWithdrawLimit(EWalletWithdrawalLimitInfoOutput eWalletWithdrawalLimitInfo, WithdrawalLimitInfoOutput withdrawalLimitInfo, DailyCustomerWithdrawalInfoOutput dailyCustomerWithdrawalInfo, decimal amount)
        {
            if (eWalletWithdrawalLimitInfo == null || withdrawalLimitInfo == null)
                return -8;

            if (eWalletWithdrawalLimitInfo.Status == (int)EWalletStatus.Disable)
                return -8;

            MinAmount = Math.Max(eWalletWithdrawalLimitInfo.WithdrawalMin, withdrawalLimitInfo.WithdrawalMin);
            if (MinAmount > amount)
                return -5;

            MaxAmount = Math.Min(eWalletWithdrawalLimitInfo.WithdrawalMax, withdrawalLimitInfo.TotalWithdrawalMax);
            if (MaxAmount < amount)
                return -9;

            DailyTotalWithdrawalMax = withdrawalLimitInfo.TotalWithdrawalMax;
            if (DailyTotalWithdrawalMax != 0 && dailyCustomerWithdrawalInfo.Amount + amount > DailyTotalWithdrawalMax)
                return -6;

            DailyWithdrawCount = withdrawalLimitInfo.Times;
            if (DailyWithdrawCount != 0 && dailyCustomerWithdrawalInfo.Count >= DailyWithdrawCount)
                return -7;

            return 0;
        }
    }
}