using MemberSite.Service.BLL;
using MemberSite.Service.Library;
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
    private readonly ICacheHelper _redisHelper;

    public RedemptionController(IServiceProvider provider)
    {
        _redemptionBll = provider.GetRequiredService<RedemptionBLL>();
        _selLangBll = provider.GetRequiredService<SelLangBLL>();
        _custInfoBll = provider.GetRequiredService<CustInfoBll>();
        _authorizeBll = provider.GetRequiredService<AuthorizeBLL>();
        _redisHelper = provider.GetRequiredService<ICacheHelper>();
    }

    [HttpPost]
    public async Task<ApiResult> CreateRedemptionAsync(SendCreateRedemptionRequest req)
    {
        var result = new ApiResult();
        var authData = _authorizeBll.GetAuthData();
        if (authData is { CustId: > 0 })
        {
            var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);           
            
            var lockKey = $"CreateRedemptionLock:{custInfo.CustId}";
            var lockValue = Guid.NewGuid().ToString(); // 每次取鎖使用唯一值，確保為同一個執行序所執行
            var lockExpiry = TimeSpan.FromSeconds(15); //lock 15 sec        
            if (await _redisHelper.TryCreateLockAsync(lockKey, lockValue, lockExpiry))
            {
                result = await _redemptionBll.CreateRedemptionAsync(new CreateRedemptionRequest()
                {
                    SiteId = custInfo.SiteId,
                    CustId = custInfo.CustId,
                    CurrencyId = custInfo.CurrencyId,
                    Point = req.Point,
                    GiftId = req.GiftId
                });

                await _redisHelper.UnLockAsync(lockKey, lockValue);
            }                          
        }

        switch (result.ErrorCode)
        {
            case (int)RedemptionErrorCode.InsufficientPoint:
                result.Message = _selLangBll.GetMsg("msg_InsufficientPoint");
                break;

            case (int)RedemptionErrorCode.RedemptionLimitOverLimit:
                result.Message = _selLangBll.GetMsg("msg_RedemptionLimitOverLimit");
                break;

            default:
                return result;
        }

        return result;
    }

    [HttpPost]
    public async Task<object> SetShoppingCartAsync(SendSetShoppingCartRequest req)
    {
        object result;
        var authData = _authorizeBll.GetAuthData();
        if (authData is { CustId: > 0 })
        {
            var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);

            var sendReq = new SetShoppingCartRequest()
            {
                SiteId = custInfo.SiteId,
                CustId = custInfo.CustId,
                CurrencyId = custInfo.CurrencyId,
                IsAdd = req.IsAdd,
                GiftIds = req.GiftIds
            };

            result = await _redemptionBll.SetShoppingCartAsync(sendReq);
        }
        else
        {
            result = new ApiResult();
        }

        return result;
    }
}