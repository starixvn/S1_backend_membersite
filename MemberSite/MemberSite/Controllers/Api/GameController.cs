using MemberSite.Service.BLL;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using MemberSite.Service.Model.Game;
using Microsoft.AspNetCore.Mvc;

namespace MemberSite.Controllers.Api
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class GameController : ControllerBase
    {
        private readonly ILogger<GameController> _logger;
        private readonly SlotGameBLL _slotGameBll;

        public GameController(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<GameController>>();
            _slotGameBll = provider.GetRequiredService<SlotGameBLL>();
        }

        [HttpPost]
        public async Task<ApiResult<GetSlotGamePageResponse>> GetSlotGamePage(GetSlotGamePageRequest request)
        {
            try
            {
                return new ApiResult<GetSlotGamePageResponse>
                {
                    ErrorCode = (int)ErrorCode.Success,
                    Data = await _slotGameBll.GetSlotGamePage(request, User)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new ApiResult<GetSlotGamePageResponse>
                {
                    ErrorCode = (int)ErrorCode.SystemException,
                    Message = ex.Message,
                };
            }
        }
    }
}
