using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using SixLabors.ImageSharp.Metadata;

namespace MemberSite.Service.BLL;

public class FaceBookPixelBLL
{
    private readonly string _commonServiceUrl;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly CourierAdapter _courierAdapter;

    public FaceBookPixelBLL(IServiceProvider provider)
    {
        var configBll = provider.GetRequiredService<ConfigBLL>();
        _commonServiceUrl = configBll.GetConfigValueByEnvironment<ServiceHostConfig>("ServiceHost").CommonService;
        _httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
        _courierAdapter = provider.GetRequiredService<CourierAdapter>();
    }

    public async Task<object> Send(PixelRequest req)
    {
        var Url = _commonServiceUrl + "FaceBookPixel/Send";
        var result = await _courierAdapter.PostAsync(Url, req);

        return result;
    }

    public PixelRequest CreatePixelRequest(int custId,int currencyId, List<PixelData> data,string url = null)
    {
        return new PixelRequest
        {
            CustId = custId,
            CurrencyId = currencyId,
            Url = url,
            Data = data
        };
    }
}