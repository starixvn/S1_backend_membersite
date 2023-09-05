namespace MemberSite.Service.Model.enums
{
    public enum DepositMessageStatus
    {
        DepositorsNameUnmatch = 0,
        DepositSlipRequired = 1,
        PaymentNotReceived = 2,
        DepositRefunded = 3,
        WrongAmountSubmitted = 4,
        WrongBankSubmitted = 5,
        InactiveBankAccountSubmission = 6,
        SystemUnstable = 7,
        BankUnderMaintenance = 8
    }
}