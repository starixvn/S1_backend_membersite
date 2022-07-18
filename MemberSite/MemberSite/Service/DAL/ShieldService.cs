using System.Data;
using Dapper;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using MySql.Data.MySqlClient;

namespace MemberSite.Service.DAL;

public class ShieldService
{
    private readonly IConfiguration configuration;
    private readonly Tools tools;
    private readonly MySqlClient _mainDbClient;
    private readonly MySqlClient _reportDbClient;

    public ShieldService(IServiceProvider provider)
    {
        configuration = provider.GetRequiredService<IConfiguration>();
        tools = provider.GetRequiredService<Tools>();
        _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
    }
    
    public int InsertCustShieldInfo(ShieldRiskRequest input , Customer custInfo)
    {
        var p = new DynamicParameters(new
        {
            CustId = custInfo.CustId,
            SiteId = custInfo.SiteId,
            CurrencyId =custInfo.CurrencyId,
            Version = input.Version,
            Timestamp = input.Timestamp,
            SessionId = input.SessionId,
            Platform = input.Platform,
            ShieldId = input.DeviceIntelligence.ShieldId,
            DeviceScore = input.DeviceIntelligence.DeviceScore,
            IsBot = input.DeviceIntelligence.IsBot,
            IsIncognito = input.DeviceIntelligence.IsIncognito,
            IsEmulated = input.DeviceIntelligence.IsEmulated,
            IsTor = input.DeviceIntelligence.IsTor,
            IsAntiFingerprinting = input.DeviceIntelligence.IsAntiFingerprinting,
            IsProxy = input.DeviceIntelligence.IsProxy,
            RunningCloneApps = input.DeviceIntelligence.RunningCloneApps,
            RunningGpsSpoofers = input.DeviceIntelligence.RunningGpsSpoofers,
            RunningVpnSpoofers = input.DeviceIntelligence.RunningVpnSpoofers,
            AutoClickerEnabled = input.DeviceIntelligence.AutoClickerEnabled,
            IsDeviceMasked = input.DeviceIntelligence.IsDeviceMasked,
            IsJailBroken = input.DeviceIntelligence.IsJailBroken,
            AppTampering = input.DeviceIntelligence.AppTampering,
            SuspiciousFactoryReset = input.DeviceIntelligence.SuspiciousFactoryReset,
            VirtualOs = input.DeviceIntelligence.VirtualOs,
            Hooking = input.DeviceIntelligence.Hooking
        });
        
        var result = _mainDbClient.Execute("Member_CustShieldRiskInfo_Ins", p);
      
        return result;
    }
    
    public int HighShieldRiskLog(HighShieldRiskLogRequest input)
    {
        var p = new DynamicParameters(new
        {
            SiteId = input.SiteId,
            CurrencyId = input.CurrencyId,
            CustId = input.CustId,
            ShieldId = input.ShieldId,
            DeviceScore = input.DeviceScore,
            TaggedIssue = input.TaggedIssue,
            TriggeredPoint = input.TriggeredPoint
        });
        
        var result = _mainDbClient.Execute("Member_CustShieldTriggeredLog_Ins", p);
      
        return result;
    }
}