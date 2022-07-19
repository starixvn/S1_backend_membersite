using MemberSite.Service.BLL;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using MemberSite.Service.Model.Store.Redemption;
using Microsoft.AspNetCore.Mvc;

namespace MemberSite.Controllers.Api;

[Route("api/[controller]/[action]")]
[ApiController]
public class RedemptionController : Controller
{
    private readonly RedemptionBLL _redemptionBll;
    private readonly SelLangBLL _selLangBll;

    public RedemptionController(IServiceProvider provider)
    {
        _redemptionBll = provider.GetRequiredService<RedemptionBLL>();
        _selLangBll = provider.GetRequiredService<SelLangBLL>();
    }
    
    [HttpPost]
    public Task<ApiResult> CreateRedemptionAsync(CreateRedemptionRequest req)
    {
        var result = _redemptionBll.CreateRedemptionAsync(req);

        switch (result.Result.ErrorCode)
        {
            case (int)RedemptionErrorCode.InsufficientPoint:
                result.Result.Message = _selLangBll.GetMsg("msg_InsufficientPoint");
                break;
            
            case (int)RedemptionErrorCode.RedemptionLimitOverLimit:
                result.Result.Message = _selLangBll.GetMsg("msg_RedemptionLimitOverLimit");
                break;
            
            default:
                return result;
        }
        
        return result;
    }
}