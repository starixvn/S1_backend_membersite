using MemberSite.Service.BLL;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static MemberSite.Extensions.MethodExtension;

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
    private readonly SelLangBLL _selLangBll;
    private readonly ILogger<ShieldController> _logger;

    public ShieldController(IServiceProvider provider)
    {
        _authorizeBll = provider.GetRequiredService<AuthorizeBLL>();
        _shieldBll = provider.GetRequiredService<ShieldBLL>();
        _custInfoBll = provider.GetRequiredService<CustInfoBll>();
        _selLangBll = provider.GetRequiredService<SelLangBLL>();
        _logger = provider.GetRequiredService<ILogger<ShieldController>>();
    }
    
    [HttpPost]
    public ApiResult ShieldRiskCheck(ShieldRiskRequest input)
    {
        var apiResult = new ApiResult();
        try
        {
            var authData = _authorizeBll.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                apiResult.Message = _selLangBll.GetMsg("msg_ForceLogedout");
                return apiResult;
            }
            var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
            
            if (input.DeviceIntelligence == null || string.IsNullOrEmpty(input.DeviceIntelligence.ShieldId))
            {
                apiResult.ErrorCode = (int) ShieldErrorCode.InvalidRequest;
                apiResult.Message = "ShieldId is invalid";
                return apiResult;
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

        }
        catch(Exception ex)
        {
            apiResult.RawMessage = ex.Message;
            apiResult.Message = ex.Message;
            _logger.LogError(ex, GetCurrentMethod());
        }
        
        return apiResult;
    }

    [HttpPost]
    public async Task<ApiResult<GetEndpointResult>> GetEndpoint(GetEndpointRequest input)
    {
        var apiResult = new ApiResult<GetEndpointResult>();
        try
        {
            var result = await _shieldBll.GetEndpoint(input.SiteId);

            if (!string.IsNullOrEmpty(result.Endpoint))
            {
                apiResult.Data = result;
                apiResult.ErrorCode = (int) ShieldErrorCode.Success;
            }
            else
            {
                apiResult.ErrorCode = (int) ShieldErrorCode.Failed;
            }
        }
        catch (Exception ex)
        {
            apiResult.RawMessage = ex.Message;
            apiResult.Message = ex.Message;
            _logger.LogError(ex, GetCurrentMethod());
        }
        
        return apiResult;
    }
}