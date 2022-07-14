using MemberSite.Service.Library;
using MemberSite.Service.Model.Store.Frontend;

namespace MemberSite.Service.BLL;

public class StoreBLL
{
    private readonly ILogger<StoreBLL> _logger;
    private readonly CourierAdapter _courierAdapter;
    private readonly string _storeSiteUrl;


    public StoreBLL(IServiceProvider provider)
    {
        var configuration = provider.GetRequiredService<IConfiguration>();
        _logger = provider.GetRequiredService<ILogger<StoreBLL>>();
        _storeSiteUrl = configuration.GetValue<string>("StoreService");
        _courierAdapter = provider.GetRequiredService<CourierAdapter>();
    }

    public async Task<object> GetGiftListAsync(StoreGetGiftListRequest request)
    {
        var url = _storeSiteUrl + "Frontend/GetGiftList";
        var result = await _courierAdapter.MyPostAsync<List<GiftListModel>, StoreGetGiftListRequest>(url, request);

        return result;
    }
}
