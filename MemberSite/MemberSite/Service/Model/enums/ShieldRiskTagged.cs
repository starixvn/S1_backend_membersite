namespace MemberSite.Service.Model.enums
{
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
}