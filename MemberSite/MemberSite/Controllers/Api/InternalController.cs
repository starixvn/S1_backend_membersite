using MemberSite.Service.BLL;
using MemberSite.Service.Model;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace MemberSite.Controllers.Api
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class InternalController : ControllerBase
    {
        private readonly ILogger<InternalController> _logger;
        private readonly string _internalKey;
        private readonly InternalBLL _internalBLL;
        public InternalController(IServiceProvider provider)
        {
            var configBll = provider.GetRequiredService<ConfigBLL>();
            _internalKey = configBll.GetConfigValue("InternalKey");
            _logger = provider.GetRequiredService<ILogger<InternalController>>();
            _internalBLL  =  provider.GetRequiredService<InternalBLL>();
        }


        [HttpPost]
        public async Task<ApiResult<IEnumerable<HotEventInfo>>> GetHotEventInfoList(InternalRequest request)
        {
            
            var apiResult = new ApiResult<IEnumerable<HotEventInfo>>();
            if(!VerifyInternalKey(request.VerifyKey))
            {
                apiResult.ErrorCode = 777;
                apiResult.Message = "Verify Internal Key,Please Check Again";
                apiResult.Data = null;
                return apiResult;
            }

            var requestDes = JsonConvert.DeserializeObject<HotEventInfoRequest>(request.RequestData);
            var responseData = await _internalBLL.GetHotEventDataList(requestDes.SiteId);

            var hotEventInfos = responseData as HotEventInfo[] ?? responseData.ToArray();
            if (!hotEventInfos.Any())
            {
                apiResult.ErrorCode = 111;
                apiResult.Message = "Can't Get Any Data";
                apiResult.Data = null;
                return apiResult;
            }

            apiResult.ErrorCode = 0;
            apiResult.Message = "Success";
            apiResult.Data = hotEventInfos;

            return apiResult;
        }
        
        [HttpPost]
        public ApiResult RegisterRejectClick(RegisterRejectClick inputs)
        {
            var apiResult = new ApiResult() { ErrorCode = 0 };
            _ = _internalBLL.RegisterRejectClickInsert(inputs);
            return apiResult; 
        }
        
        #region private

        private bool VerifyInternalKey(string key)
        {
            if (_internalKey == null)
            {
                return false;
            }
            return key == _internalKey;
        }
        
        #endregion

    }
}