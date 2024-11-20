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
        private readonly GameBLL _gameBll;
        private readonly ServerInfoBll _serverInfoBll;

        public GameController(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<GameController>>();
            _slotGameBll = provider.GetRequiredService<SlotGameBLL>();
            _gameBll = provider.GetRequiredService<GameBLL>();
            _serverInfoBll = provider.GetRequiredService<ServerInfoBll>();
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

        /// <summary>
        /// Home 近期瀏覽
        /// </summary>
        /// <remarks>
        ///
        /// Http Method：POST
        ///
        /// Func Path：Home > [Recently Viewed]
        ///
        /// </remarks>
        /// <returns>結果</returns>
        [HttpPost]
        public async Task<ApiResult<RecentlyViewedResponse>> GetRecentlyViewed(RecentlyViewedRequest request)
        {
            try
            {
                return new ApiResult<RecentlyViewedResponse>
                {
                    ErrorCode = (int)ErrorCode.Success,
                    Data = await _slotGameBll.GetRecentlyViewed(request, User)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new ApiResult<RecentlyViewedResponse>
                {
                    ErrorCode = (int)ErrorCode.SystemException,
                    Message = ex.Message,
                };
            }
        }

        [HttpPost]
        public async Task<ApiResult<IEnumerable<LobbyHeaderResponse>>> GetLobbyHeader(GetLobbyHeaderRequest request)
        {
            try
            {
                var serverInfo = _serverInfoBll.GetServerInfo();
                var data = await _gameBll.GetLobbyHeader(request, serverInfo);

                return new ApiResult<IEnumerable<LobbyHeaderResponse>>
                {
                    ErrorCode = (int)ErrorCode.Success,
                    Data = data
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new ApiResult<IEnumerable<LobbyHeaderResponse>>
                {
                    ErrorCode = (int)ErrorCode.SystemException,
                    Message = ex.Message,
                };
            }
        }

        [HttpPost]
        public async Task<ApiResult<LobbyGameResponse>> GetLobbyGame(GetLobbyGameRequest request)
        {
            try
            {
                var serverInfo = _serverInfoBll.GetServerInfo();
                var data = await _gameBll.GetLobbyGame(request, serverInfo);

                return new ApiResult<LobbyGameResponse>
                {
                    ErrorCode = (int)ErrorCode.Success,
                    Data = data
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new ApiResult<LobbyGameResponse>
                {
                    ErrorCode = (int)ErrorCode.SystemException,
                    Message = ex.Message,
                };
            }
        }
    }
}
