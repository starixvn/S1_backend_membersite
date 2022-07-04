using MemberSite.Service.BLL;
using MemberSite.Service.Model.Store.Redemption;
using Microsoft.AspNetCore.Mvc;

namespace MemberSite.Controllers.Api;

[Route("api/[controller]/[action]")]
[ApiController]
public class RedemptionController : Controller
{
    private readonly RedemptionBLL _redemptionBll;

    public RedemptionController(IServiceProvider provider)
    {
        _redemptionBll = provider.GetRequiredService<RedemptionBLL>();
    }
    
    [HttpPost]
    public Task<object> CreateRedemptionAsync(CreateRedemptionRequest req)
    {
        var result = _redemptionBll.CreateRedemptionAsync(req);
        
        return result;
    }
}