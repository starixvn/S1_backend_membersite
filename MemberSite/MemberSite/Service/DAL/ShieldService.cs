using System.Security.Cryptography;
using System.Text;
using Dapper;
using MemberSite.Service.BLL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
namespace MemberSite.Service.DAL;

public class ShieldService
{
    private readonly CourierAdapter _courierAdapter;
    private readonly Dictionary<string, ShieldConfig> _shieldConfig;
    private readonly string _shieldApiUrl;
    private readonly MySqlClient _mainDbClient;
    private readonly MySqlClient _reportDbClient;


    public ShieldService(IServiceProvider provider)
    {
        _courierAdapter = provider.GetRequiredService<CourierAdapter>();

        var configBll = provider.GetRequiredService<ConfigBLL>();
        _shieldConfig = configBll.GetConfigValuesBySiteName<ShieldConfig>("ShieldConfig");
        _shieldApiUrl = configBll.GetConfigValue("ShieldApiUrl");
        _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
    }

    public CustShieldRiskInfoData GetCustShieldRiskInfoList(int custId)
    {
        var p = new DynamicParameters();
        p.Add("_CustId", custId);
        return _reportDbClient.QueryFirst<CustShieldRiskInfoData>("Member_CustShieldRiskInfo_Get", p);
    }

    public int InsertCustShieldInfo(ShieldRiskRequest input, Customer custInfo)
    {
        var p = new DynamicParameters(new
        {
            CustId = custInfo.CustId,
            SiteId = custInfo.SiteId,
            CurrencyId = custInfo.CurrencyId,
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

    public async Task<GetEndpointResult> GetEndpoint(int siteId)
    {
        var siteName = Enum.GetName(typeof(SiteId), siteId);
        var siteGroup = _shieldConfig[siteName ?? string.Empty];
        var apiUrl = $"{_shieldApiUrl}?sid={siteGroup.SiteId}";

        var timestamp = DateTimeOffset.Now.ToUnixTimeSeconds().ToString();
        var sign = GetShieldSignature(timestamp, siteId);
        var header = new Dictionary<string, string>
        {
            {"Site-Id", siteGroup.SiteId},
            {"Timestamp", timestamp},
            {"Shield-Signature", sign}
        };

        var result = await _courierAdapter.ShieldGetAsync<GetEndpointResult>(apiUrl, header);

        return result;
    }

    public async Task<SendAttributesResult> SendShieldAttributes(GetEndpointResult endpoint, SendShieldAttributesRequest request, string timestamp, string sign)
    {
        var apiUrl = $" {endpoint.Endpoint}/shield-fp/v1/api/attributes/{request.SessionId}";

        var req = new
        {
            user_id = request.UserId,
            platform = request.Platform.ToUpper()
        };

        var siteName = Enum.GetName(typeof(SiteId), request.SiteId);
        var siteGroup = _shieldConfig[siteName ?? string.Empty];

        var header = new Dictionary<string, string>
        {
            {"Site-Id", siteGroup.SiteId},
            {"Timestamp", timestamp},
            {"Shield-Signature", sign},
            {"X-Endpoint-Version", endpoint.Version.ToString()}
        };

        var result = await _courierAdapter.ShieldPostAsync<SendAttributesResult, object>(apiUrl, req, header);

        return result;
    }

    public string GetShieldSignature(string timestamp, int siteId)
    {
        var siteName = Enum.GetName(typeof(SiteId), siteId);
        var siteGroup = _shieldConfig[siteName ?? string.Empty];

        var timeHash = HmacSha256(timestamp, siteGroup.SecretKey);
        var shieldSignature = HmacSha256(siteGroup.SiteId, timeHash);

        return shieldSignature;
    }

    private static string HmacSha256(string toHash, string secretKey)
    {
        using var mac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
        var hash = mac.ComputeHash(Encoding.UTF8.GetBytes(toHash));
        var sign = BitConverter.ToString(hash).Replace("-", "").ToLower();

        return sign;
    }
}