using MemberSite.Service.BLL;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using MemberSite.Service.Model.Store.Redemption;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MemberSite.Controllers.Api;

[Authorize]
[Route("api/[controller]/[action]")]
[ApiController]
public class RedemptionController : Controller
{
    private readonly RedemptionBLL _redemptionBll;
    private readonly SelLangBLL _selLangBll;
    private readonly AuthorizeBLL _authorizeBll;
    private readonly CustInfoBll _custInfoBll;

    public RedemptionController(IServiceProvider provider)
    {
        _redemptionBll = provider.GetRequiredService<RedemptionBLL>();
        _selLangBll = provider.GetRequiredService<SelLangBLL>();
        _custInfoBll = provider.GetRequiredService<CustInfoBll>();
        _authorizeBll = provider.GetRequiredService<AuthorizeBLL>();
    }
    
    [HttpPost]
    public Task<ApiResult> CreateRedemptionAsync(SendCreateRedemptionRequest req)
    {
        var authData = _authorizeBll.GetAuthData();
        var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
        
        var sendReq = new CreateRedemptionRequest()
        {
            SiteId = custInfo.SiteId,
            CustId = custInfo.CustId,
            CurrencyId = custInfo.CurrencyId,
            Point = req.Point,
            GiftId = req.GiftId
        };
        
        var result = _redemptionBll.CreateRedemptionAsync(sendReq);

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