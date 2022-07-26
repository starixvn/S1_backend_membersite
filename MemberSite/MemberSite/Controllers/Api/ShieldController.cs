using MemberSite.Service.BLL;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MemberSite.Controllers.Api;

[AllowAnonymous]
[Route("api/[controller]/[action]")]
[Route("api/[controller]/[action]/{LangId}")]
[ApiController]
public class ShieldController : ControllerBase
{
    private readonly AuthorizeBLL _authorizeBll;
    private readonly ShieldBLL _shieldBll;
    private readonly CustInfoBll _custInfoBll;
    
    public ShieldController(IServiceProvider provider)
    {
        _authorizeBll = provider.GetRequiredService<AuthorizeBLL>();
        _shieldBll = provider.GetRequiredService<ShieldBLL>();
        _custInfoBll = provider.GetRequiredService<CustInfoBll>();
    }
    
    [HttpPost]
    public ApiResult ShieldRiskCheck(ShieldRiskRequest input)
    {
        var apiResult = new ApiResult();
        var authData = _authorizeBll.GetAuthData();
        var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);

        if (string.IsNullOrEmpty(input.DeviceIntelligence.ShieldId))
        {
            apiResult.ErrorCode = (int) ShieldErrorCode.InvalidRequest;
            apiResult.Message = "ShieldId is invalid";
        }

        var result = _shieldBll.InsertCustShieldInfo(input, custInfo);

        if (result > 0)
        {
            apiResult.ErrorCode = (int) ShieldErrorCode.Success;
        }
        else
        {
            apiResult.ErrorCode = (int) ShieldErrorCode.Failed;
        }

        return apiResult;
    }

    [HttpPost]
    public async Task<ApiResult<GetEndpointResult>> GetEndpoint()
    {
        var apiResult = new ApiResult<GetEndpointResult>();
        var result = await _shieldBll.GetEndpoint();

        if (!string.IsNullOrEmpty(result.Endpoint))
        {
            apiResult.Data = result;
            apiResult.ErrorCode = (int) ShieldErrorCode.Success;
        }
        else
        {
            apiResult.ErrorCode = (int) ShieldErrorCode.Failed;
        }
        
        return apiResult;
    }
}