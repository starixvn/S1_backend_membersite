using MemberSite.Service.BLL;
using MemberSite.Service.Model;
using Microsoft.AspNetCore.Mvc;

namespace MemberSite.Controllers.Api
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class WorldCupPredictionController : ControllerBase
    {
        private readonly WorldCupPredictionBLL _worldCupPredictionBll;

        public WorldCupPredictionController(IServiceProvider provider)
        {
            _worldCupPredictionBll = provider.GetRequiredService<WorldCupPredictionBLL>();
        }

        [HttpPost]
        public ApiResult<WorldCupPredictionSettingResponse> GetWorldCupPredictionSetting(WorldCupPredictionSettingRequest request)
        {
            var apiResult = new ApiResult<WorldCupPredictionSettingResponse>();
            try
            {
                var responseData = _worldCupPredictionBll.GetWorldCupPredictionSettingResponse(request.LangId);
                apiResult.Data = responseData;
                apiResult.ErrorCode = 0;
            }
            catch (Exception ex)
            {
                apiResult.Message = ex.Message;
            }

            return apiResult;
        }
    }
}
