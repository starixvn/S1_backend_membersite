using MemberSite.Service.Library;
using MemberSite.Service.Model.Store.Redemption;

namespace MemberSite.Service.BLL;

public class RedemptionBLL
{
    private readonly ILogger<RedemptionBLL> _logger;
    private readonly CourierAdapter _courierAdapter;
    private readonly string _storeSiteUrl;
        

    public RedemptionBLL(IServiceProvider provider)
    {
        var configuration = provider.GetRequiredService<IConfiguration>();
        _logger = provider.GetRequiredService<ILogger<RedemptionBLL>>();
        _storeSiteUrl = configuration.GetValue<string>("StoreService");
        _courierAdapter = provider.GetRequiredService<CourierAdapter>();
    }
    
    public async Task<object> CreateRedemptionAsync(CreateRedemptionRequest req)
    {
        var url = _storeSiteUrl + "/api/Redemption/CreateRedemption";
        var result = await _courierAdapter.MyPostAsync<object,CreateRedemptionRequest>(url, req);
        
        return result;
    }
}