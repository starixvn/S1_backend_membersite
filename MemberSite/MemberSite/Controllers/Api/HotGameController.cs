using MemberSite.Service.BLL.Interface;
using MemberSite.Service.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MemberSite.Controllers.Api
{
    [AllowAnonymous]
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class HotGameController : ControllerBase
    {
        private readonly IHotGameBLL _hotGameBll;

        public HotGameController(IHotGameBLL hotGameBll)
        {
            this._hotGameBll = hotGameBll;
        }


        [HttpPost]
        public async Task<ApiResult<Dictionary<string, IEnumerable<HotGameResponse>>>> GetDataList(GetHotGameDataListRequest request)
        {
           var data = await _hotGameBll.GetHotGames(1, request.CurrencyId.Value, request.DeviceType);

           var apiResult = new ApiResult<Dictionary<string, IEnumerable<HotGameResponse>>>
           {
               Data = data,
               ErrorCode = 0
           };

           return apiResult;
        }

    }
}
