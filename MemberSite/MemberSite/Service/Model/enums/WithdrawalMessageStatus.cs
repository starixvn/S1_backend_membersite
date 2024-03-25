namespace MemberSite.Service.Model.enums
{
    public enum WithdrawalMessageStatus
    {
        WithdrawalAccountsNameUnmatch = 0,
        WithdrawalAccountNumberIncorrect = 1,
        WithdrawalAccountAannotReceiveMoney = 2,
        BeneficiarysAccountMaintenance = 3,
        BankSystemUnstable = 4,
        WithdrawalLimitExceededINR = 5
    }
}