using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.Store.Frontend;
using Newtonsoft.Json.Linq;

namespace MemberSite.Service.BLL;

public class StoreBLL
{
    private readonly ILogger<StoreBLL> _logger;
    private readonly CourierAdapter _courierAdapter;
    private readonly string _storeSiteUrl;
    private readonly Dictionary<int, string> _siteStoreImageUrl;
    public StoreBLL(IServiceProvider provider)
    {
        _logger = provider.GetRequiredService<ILogger<StoreBLL>>();
        var configBll = provider.GetRequiredService<ConfigBLL>();
        _storeSiteUrl = configBll.GetConfigValueByEnvironment<ServiceHostConfig>("ServiceHost").StoreService;
        _courierAdapter = provider.GetRequiredService<CourierAdapter>();
        _siteStoreImageUrl = configBll.GetConfigValuesBySiteId<string>("StoreImageUrl");
    }

    public async Task<object> GetGiftListAsync(StoreGetGiftListRequest request)
    {
        var url = _storeSiteUrl + "Frontend/GetGiftList";
        var result = await _courierAdapter.PostAsync(url, request);

        var jsonObject = JObject.FromObject(result);
        var dataArray = jsonObject["data"] as JArray;
        if (dataArray != null)
        {
            foreach (var item in dataArray)
            {
                var siteId = item["SiteId"].ToObject<int>();
                var giftPictureUrl = item["GiftPictureUrl"]?.ToString();
                if (!string.IsNullOrEmpty(giftPictureUrl))
                {
                    item["GiftPictureUrl"] = $"{_siteStoreImageUrl[siteId]}{item["GiftPictureUrl"]}";
                }
            }
        }

        return result;
    }

    public async Task<object> GetMyCoinAsync(StoreGetMyCoinRequest request)
    {
        var url = _storeSiteUrl + "Frontend/GetMyCoin";

        var result = await _courierAdapter.PostAsync(url, request);
        return result;
    }
    public async Task<object> SetFavoriteAsync(StoreSetFavoriteRequest request)
    {
        var url = _storeSiteUrl + "Frontend/SetFavorite";
        var result = await _courierAdapter.PostAsync(url, request);

        return result;
    }

    public async Task<object> GetBalanceAsync(StoreGetBalanceRequest request)
    {
        var url = _storeSiteUrl + "Member/GetBalance";
        var result = await _courierAdapter.PostAsync(url, request);

        return result;
    }

    public async Task<object> GetGiftTagsAsync(StoreGetTagsRequest request)
    {
        var url = _storeSiteUrl + "Frontend/GetTags";
        var result = await _courierAdapter.PostAsync(url, request);

        return result;
    }

    public async Task<object> GetMyPurchesAsync(StoreGetMyPurchesRequest request)
    {
        var url = _storeSiteUrl + "Frontend/GetMyPurches";
        var result = await _courierAdapter.PostAsync(url, request);

        return result;
    }
}
