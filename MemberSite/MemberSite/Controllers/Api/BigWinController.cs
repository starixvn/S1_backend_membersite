using MemberSite.Service.BLL;
using MemberSite.Service.Model;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using static MemberSite.Extensions.MethodExtension;

namespace MemberSite.Controllers.Api
{
    [AllowAnonymous]
    [Route("api/[controller]/[action]")]
    [Route("api/[controller]/[action]/{LangId}")]
    [ApiController]
    public class BigWinController : ControllerBase
    {
        private readonly AuthorizeBLL _authorizeBll;
        private readonly CustInfoBll _customerInfoBll;
        private readonly ServerInfoBll _serverInfoBll;
        private readonly SelLangBLL _selLangBll;
        private readonly BigWinBLL _bigWinBLL;
        private readonly CustInfoBll _custInfoBll;
        private readonly ILogger<BigWinController> _logger;
        public BigWinController(IServiceProvider provider)
        {
            _authorizeBll = provider.GetRequiredService<AuthorizeBLL>();
            _customerInfoBll = provider.GetRequiredService<CustInfoBll>();
            _serverInfoBll = provider.GetRequiredService<ServerInfoBll>();
            _selLangBll = provider.GetRequiredService<SelLangBLL>();
            _bigWinBLL = provider.GetRequiredService<BigWinBLL>();
            _custInfoBll = provider.GetRequiredService<CustInfoBll>();
            _logger = provider.GetRequiredService<ILogger<BigWinController>>();
        }

        public class GetPopUpDataListRequest
        {
            public int deviceType { get; set; }
        }
        [HttpPost]
        public async Task<ApiResult<BigWinPopUp>> GetPopUpDataList(GetPopUpDataListRequest request)
        {
            var result = new ApiResult<BigWinPopUp>();
            try
            {

                var authData = _authorizeBll.GetAuthData();
                if (authData is { CustId: > 0 })
                {
                    var serverInfo = _serverInfoBll.GetServerInfo();
                    var custId = authData.CustId;
                    var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
                    var currencyId = custInfo.CurrencyId;
                    var langId = _selLangBll.GetLangId();

                    result.Data = await _bigWinBLL.GetBigWinPopUpData(custInfo.SiteId, custInfo.CurrencyId, langId, request.deviceType);
                    result.ErrorCode = 0;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, GetCurrentMethod());
            }
            return result;
        }
    }
}
