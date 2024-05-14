using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

using Microsoft.Extensions.Caching.Memory;

namespace MemberSite.Service.BLL;

public class ShieldBLL
{
    private readonly ShieldService _shieldService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly CacheHelper _cacheHelper;

    public ShieldBLL(IServiceProvider provider)
    {
        _shieldService = provider.GetRequiredService<ShieldService>();
        _httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
        _cacheHelper = provider.GetRequiredService<CacheHelper>();
    }

    public int InsertCustShieldInfo(ShieldRiskRequest input, Customer custInfo)
    {
        var result = _shieldService.InsertCustShieldInfo(input, custInfo);

        return result;
    }

    public async Task<SendAttributesResult> SendShieldAttributes(SendShieldAttributesRequest request)
    {
        var result = new SendAttributesResult();
        var context = _httpContextAccessor.HttpContext;
        request.SessionId = context.Request.Cookies["session_id"];
        request.Platform = context.Request.Cookies["platform"];

        var timestamp = DateTimeOffset.Now.ToUnixTimeSeconds().ToString();
        var sign = _shieldService.GetShieldSignature(timestamp);
        var endpoint = await GetEndpoint(request.SiteId);

        if (!string.IsNullOrEmpty(endpoint.Endpoint) &&
            !string.IsNullOrEmpty(request.SessionId) &&
            !string.IsNullOrEmpty(request.Platform))
        {
            result = await _shieldService.SendShieldAttributes(endpoint, request, timestamp, sign);
        }

        return result;
    }

    public Task<GetEndpointResult> GetEndpoint(int siteId)
    {
        var key = $"ShieldEndpoint:{siteId}";

        var result = _cacheHelper.TryGetOrCreate(key,
            new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(5)),
            i => _shieldService.GetEndpoint());

        return result;
    }

    private int InsertHighShieldRiskLog(HighShieldRiskLogRequest input)
    {
        var result = _shieldService.HighShieldRiskLog(input);

        return result;
    }

    public int CheckCustShieldRisk(Customer custInfo, ShieldRiskTriggeredPoint trigger, ShieldRiskTaggedIssue issue)
    {
        var context = _httpContextAccessor.HttpContext;
        var shieldId = context.Request.Cookies["shield_id"];
        var strDeviceScore = context.Request.Cookies["device_score"];
        var deviceScore = strDeviceScore != null ? Convert.ToInt32(strDeviceScore) : -1;
        var strIsBot = context.Request.Cookies["is_bot"];
        var isBot = strIsBot != null && Convert.ToBoolean(strIsBot);

        if (!string.IsNullOrEmpty(shieldId) &&
            ((deviceScore > 0 && Enumerable.Range(0, 75).Contains(deviceScore)) || isBot))
        {
            var req = new HighShieldRiskLogRequest
            {
                SiteId = custInfo.SiteId,
                CurrencyId = custInfo.CurrencyId,
                CustId = custInfo.CustId,
                ShieldId = shieldId,
                DeviceScore = deviceScore,
                TriggeredPoint = (int)trigger,
                TaggedIssue = (int)issue
            };

            if (InsertHighShieldRiskLog(req) >= 0)
            {
                return (int)ShieldErrorCode.Success;
            }
            return (int)ShieldErrorCode.Failed;
        }

        return (int)ShieldErrorCode.Success;
    }
}