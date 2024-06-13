using MemberSite.Service.Library;
using MemberSite.Service.Model;

namespace MemberSite.Service.BLL;

public class FaceBookPixelBLL
{
    private readonly string _commonServiceUrl;
    private readonly CourierAdapter _courierAdapter;
    private readonly ServerInfoBll _serverInfoBll;

    public FaceBookPixelBLL(IServiceProvider provider)
    {
        var configBll = provider.GetRequiredService<ConfigBLL>();
        _serverInfoBll = provider.GetRequiredService<ServerInfoBll>();
        _commonServiceUrl = configBll.GetConfigValueByEnvironment<ServiceHostConfig>("ServiceHost").CommonService;
        _courierAdapter = provider.GetRequiredService<CourierAdapter>();
    }

    public async Task<object> Send(PixelRequest req)
    {
        var url = _commonServiceUrl + "FaceBookPixel/Send";
        var result = await _courierAdapter.PostAsync(url, req);

        return result;
    }

    public PixelRequest CreatePixelRequest(int custId, int currencyId, decimal? value, IEnumerable<FaceBookPixel> pixelEvent)
    {
        return new PixelRequest
        {
            CustId = custId,
            CurrencyId = currencyId,
            Value = value,
            Url = $"https://{_serverInfoBll.GetDomain()}",
            PixelEvent = pixelEvent
        };
    }
}