using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.Store.Redemption;

namespace MemberSite.Service.BLL;

public class RedemptionBLL
{
    private readonly ILogger<RedemptionBLL> _logger;
    private readonly CourierAdapter _courierAdapter;
    private readonly string _storeSiteUrl;
    private readonly string _tpServiceUrl;
    private readonly RedemptionService _redemptionService;
    private readonly ServerInfoBll _serverInfoBll;

    public RedemptionBLL(IServiceProvider provider)
    {
        _logger = provider.GetRequiredService<ILogger<RedemptionBLL>>();
        var configBll = provider.GetRequiredService<ConfigBLL>();
        _storeSiteUrl = configBll.GetConfigValueByEnvironment<ServiceHostConfig>("ServiceHost").StoreService;
        _tpServiceUrl = configBll.GetConfigValueByEnvironment<ServiceHostConfig>("ServiceHost").TpService;
        _courierAdapter = provider.GetRequiredService<CourierAdapter>();
        _redemptionService = provider.GetRequiredService<RedemptionService>();
        _serverInfoBll = provider.GetRequiredService<ServerInfoBll>();
    }

    public async Task<ApiResult> CreateRedemptionAsync(CreateRedemptionRequest req)
    {
        var giftInfo = _redemptionService.GetGiftInfo(req);
        var url = _storeSiteUrl + "Redemption/CreateRedemption";
        var result = await _courierAdapter.MyPostAsync<object, CreateRedemptionRequest>(url, req);

        //申請完後自動發放
        if (result.ErrorCode == 0 && giftInfo.GiftType == 2)
        {
            //await AutoApportion(req, giftInfo);
        }
        return result;
    }

    public async Task<object> SetShoppingCartAsync(SetShoppingCartRequest req)
    {
        var url = _storeSiteUrl + "GiftManagement/SetShoppingCart";
        var result = await _courierAdapter.PostAsync(url, req);

        return result;
    }

    private async Task AutoApportion(CreateRedemptionRequest req, GiftInfo giftInfo)
    {
        var serverInfo = _serverInfoBll.GetServerInfo();
        var transId = _redemptionService.GetRedemptionId(req);
        var bonusCode = BonusCodeRule(giftInfo, serverInfo.SiteName);
        _logger.LogInformation($"FreeSpinAutoProcess，TransId:{transId}");
        var inputs = new Dictionary<string, string>
        {
            { "bonusCode", bonusCode }
        };
        var request = new AddPrize()
        {
            TpId = 5,
            CustId = req.CustId,
            SiteId = req.SiteId,
            inputs = inputs
        };
        var tpUrl = _tpServiceUrl + "AddPrizeToPlayer";
        var autoResult = await _courierAdapter.MyPostAsync<object, AddPrize>(tpUrl, request);

        //發放成功後更改status與紀錄，與後台approve動作相同
        if (autoResult.ErrorCode == 0)
        {
            var setRedemptionInfoRequest = new SetRedemptionInfoRequest
            {
                SiteId = giftInfo.SiteId,
                TransId = transId,
                Action = 2,//approve
                Comment = "auto",
                ActualCost = giftInfo.PriceWorth,
                ReceiptUrls = Enumerable.Empty<string>(),
                Operator = "System",
                BonusCode = bonusCode,
            };
            var storeUrl = _storeSiteUrl + "Redemption/SetRedemptionInfo";
            var storeResult = await _courierAdapter.MyPostAsync<object, SetRedemptionInfoRequest>(storeUrl, setRedemptionInfoRequest);

            _logger.LogInformation(storeResult.ErrorCode == 0 ? "AutoProcessSuccess" : $"AutoProcessFail:{storeResult.RawMessage}");
        }
    }

    private static string BonusCodeRule(GiftInfo giftInfo, string siteName)
    {
        var bonusCode = $"PPFREESPIN{giftInfo.Round}FOR{siteName.ToUpper()}{DateTime.UtcNow.ToString("yyyyMMdd")}";
        return bonusCode;
    }
}