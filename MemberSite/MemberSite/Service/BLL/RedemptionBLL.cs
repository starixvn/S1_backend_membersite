using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.Store.Redemption;

namespace MemberSite.Service.BLL;

public class RedemptionBLL
{
    private readonly ILogger<RedemptionBLL> _logger;
    private readonly CourierAdapter _courierAdapter;
    private readonly string _storeSiteUrl;


    public RedemptionBLL(IServiceProvider provider)
    {
        _logger = provider.GetRequiredService<ILogger<RedemptionBLL>>();
        var configBll = provider.GetRequiredService<ConfigBLL>();
        _storeSiteUrl = configBll.GetConfigValueByEnvironment<ServiceHostConfig>("ServiceHost").StoreService;
        _courierAdapter = provider.GetRequiredService<CourierAdapter>();
    }

    public async Task<ApiResult> CreateRedemptionAsync(CreateRedemptionRequest req)
    {
        var url = _storeSiteUrl + "Redemption/CreateRedemption";
        var result = await _courierAdapter.MyPostAsync<object, CreateRedemptionRequest>(url, req);

        return result;
    }

    public async Task<object> SetShoppingCartAsync(SetShoppingCartRequest req)
    {
        var url = _storeSiteUrl + "GiftManagement/SetShoppingCart";
        var result = await _courierAdapter.PostAsync(url, req);

        return result;
    }
}