using System.ComponentModel;

namespace MemberSite.Service.Model.enums
{
    public enum DepositType
    {
        [Description("OnlineBanking")]
        OnlineBanking = 1,
        [Description("EWallet")]
        MobilePayment = 2,
        [Description("BankTransfer")]
        LocalBank = 3,
        [Description("Crypto")]
        CryptoPayment = 4,
        [Description("TopUpCard")]
        LoadCardPayment = 5,
        [Description("QRPay")]
        QrPayPayment = 6,
        [Description("AutoChannel")]
        BankAutoChannel = 7,
        CryptoWallet = 8,
        [Description("QuickTransfer")]
        QuickTransfer = 9,
        [Description("ChatTransfer")]
        ChatTransfer = 10,
        [Description("DigitalWallet")]
        DigitalWallet = 11
    }
}