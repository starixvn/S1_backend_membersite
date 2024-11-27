using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;

namespace MemberSite.Service.BLL;

public class FaceBookPixelBLL
{
    private readonly string _commonServiceUrl;
    private readonly CourierAdapter _courierAdapter;
    private readonly ServerInfoBll _serverInfoBll;
    private readonly FaceBookPixelService _faceBookPixelService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly Tools _tools;
    
    public FaceBookPixelBLL(IServiceProvider provider)
    {
        var configBll = provider.GetRequiredService<ConfigBLL>();
        _serverInfoBll = provider.GetRequiredService<ServerInfoBll>();
        _commonServiceUrl = configBll.GetConfigValueByEnvironment<ServiceHostConfig>("ServiceHost").CommonService;
        _courierAdapter = provider.GetRequiredService<CourierAdapter>();
        _faceBookPixelService = provider.GetRequiredService<FaceBookPixelService>();
        _httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
        _tools = provider.GetRequiredService<Tools>();
    }

    public async Task<object> Send(PixelRequest req)
    {
        var url = _commonServiceUrl + "FaceBookPixel/Send";
        var result = await _courierAdapter.PostAsync(url, req);

        return result;
    }

    public int CustFaceBookDataIns(int custId , string transId)
    {
        var context = _httpContextAccessor.HttpContext;
        var request = new CustFacebookData
        {
            CustId = custId,
            TransId = transId,
            Fbc = context?.Request.Cookies["_fbc"] ?? "",
            Fbp = context?.Request.Cookies["_fbp"] ?? "",
            UserAgent = context?.Request.Headers["User-Agent"],
            Ip = _tools.GetClientIP()
        }; 
        
        var result = _faceBookPixelService.CustFaceBookDataIns(request);

        return result;
    }
    
    public PixelRequest CreatePixelRequest(string transId, int siteId ,int custId, int currencyId, decimal value, IEnumerable<FaceBookPixel> pixelEvent)
    {
        return new PixelRequest
        {
            TransId = transId,
            SiteId = siteId,
            CustId = custId,
            CurrencyId = currencyId,
            Value = value,
            Url = $"https://{_serverInfoBll.GetDomain()}",
            PixelEvent = pixelEvent
        };
    }
}