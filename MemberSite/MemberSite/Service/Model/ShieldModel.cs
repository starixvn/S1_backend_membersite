using System.Text.Json.Serialization;

namespace MemberSite.Service.Model;


public class ShieldRiskRequest
{
    public string Version { get; set; }
    public long Timestamp { get; set; }
    [JsonPropertyName("session_id")]
    public string SessionId { get; set; }
    public string Platform { get; set; }
    [JsonPropertyName("device_intelligence")]
    public DeviceIntelligence DeviceIntelligence { get; set; }
}

public class SendShieldAttributesRequest
{
    public int SiteId { get; set; }
    public string UserId { get; set; }
    public string Platform { get; set; }
    public string SessionId { get; set; }
}

public class DeviceIntelligence
{
    //web
    [JsonPropertyName("shield_id")]
    public string ShieldId { get; set; }
    
    [JsonPropertyName("device_score")]
    public int DeviceScore { get; set; }
    
    [JsonPropertyName("is_bot")]
    public bool? IsBot { get; set; }
    
    [JsonPropertyName("is_incognito")]
    public bool? IsIncognito { get; set; }
    
    [JsonPropertyName("is_emulated")]
    public bool? IsEmulated { get; set; }
    
    [JsonPropertyName("is_tor")]
    public bool? IsTor { get; set; }
    
    [JsonPropertyName("is_anti_fingerprinting")]
    public bool? IsAntiFingerprinting { get; set; }
    
    [JsonPropertyName("is_proxy")]
    public bool? IsProxy { get; set; }
    
    //app
    
    [JsonPropertyName("running_clone_apps")]
    public bool? RunningCloneApps { get; set; }
    
    [JsonPropertyName("running_gps_spoofers")]
    public bool? RunningGpsSpoofers { get; set; }
    
    [JsonPropertyName("running_vpn_spoofers")]
    public bool? RunningVpnSpoofers { get; set; }
    
    [JsonPropertyName("auto_clicker_enabled")]
    public bool? AutoClickerEnabled { get; set; }
    
    [JsonPropertyName("is_device_masked")]
    public bool? IsDeviceMasked { get; set; }
    
    [JsonPropertyName("is_jailbroken")]
    public bool? IsJailBroken { get; set; }
    
    [JsonPropertyName("app_tampering")]
    public bool? AppTampering { get; set; }
    
    [JsonPropertyName("suspicious_factory_reset")]
    public bool? SuspiciousFactoryReset { get; set; }
    
    [JsonPropertyName("virtual_os")]
    public bool? VirtualOs { get; set; }
    
    public bool? Hooking { get; set; }
}

public class HighShieldRiskLogRequest
{
    public int SiteId { get; set; }
    public int CurrencyId { get; set; }
    public int CustId { get; set; }
    public string ShieldId { get; set; }
    public int DeviceScore { get; set; }
    public int TriggeredPoint { get; set; }
    public int TaggedIssue { get; set; }
}

public class GetEndpointRequest
{
    public int SiteId { get; set; }
}

public class GetEndpointResult
{
    [JsonPropertyName("endpoint")]
    public string Endpoint { get; set; }
    [JsonPropertyName("version")]
    public int Version { get; set; }
    [JsonPropertyName("mode")]
    public int Mode { get; set; }
    [JsonPropertyName("fp_setting")]
    public bool FpSetting { get; set; }
}

public class SendAttributesResult
{
    public string Status { get; set; }
    public string Code { get; set; }
    public string Message { get; set; }
}
