using MemberSite.Service.BLL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static MemberSite.Extensions.MethodExtension;

namespace MemberSite.Controllers.Api
{
    [Route("api/[controller]/[action]/{LangId}")]
    [ApiController]
    public class PromotionController : ControllerBase
    {
        private readonly SelLangBLL _selLangBLL;
        private readonly PromotionBll _promotionBLL;
        private readonly ServerInfoBll _serverInfoBLL;
        private readonly AuthorizeBLL _authorizeBLL;
        private readonly CustInfoBll _custInfoBLL;
        private readonly string _bonusServiceUrl;
        private readonly CourierAdapter _courierAdapter;
        private readonly ILogger<PromotionController> _logger;
        private readonly Tools _tools;

        public PromotionController(IServiceProvider provider)
        {
            _selLangBLL = provider.GetRequiredService<SelLangBLL>();
            _promotionBLL = provider.GetRequiredService<PromotionBll>();
            _serverInfoBLL = provider.GetRequiredService<ServerInfoBll>();
            _authorizeBLL = provider.GetRequiredService<AuthorizeBLL>();
            _custInfoBLL = provider.GetRequiredService<CustInfoBll>();
            _bonusServiceUrl = provider.GetRequiredService<IConfiguration>().GetValue<string>("BonusService");
            _courierAdapter = provider.GetRequiredService<CourierAdapter>();
            _logger = provider.GetRequiredService<ILogger<PromotionController>>();
            _tools = provider.GetRequiredService<Tools>();
        }
        
        public class PromotionReq
        {
            public string BonusCode { set; get; }
            public decimal? LockedAmount { set; get; }
            public int? DeviceType { set; get; }
        }
        
        public class PromotionRequest
        {
            public string BonusCode { set; get; }
            public int CustId { set; get; }
            public string custIP { set; get; }
            public decimal? LockedAmount { set; get; }
            public int? DeviceType { set; get; }
        }
        
        public class CustPromotionRequest
        {
            public int SiteId { set; get; }
            public int CustId { set; get; }
            public int CurrencyId { set; get; }
            public int DeviceType { set; get; }
            public bool IsLogin { set; get; }
            public string LangId { set; get; }
            public string DefLang { set; get; }
        }
        
        public class PromotionDetailRequest : CustPromotionRequest
        {
            public string BonusCode { set; get; }
        }
        
        public class PromotionDeviceReq
        {
            public int DeviceType { set; get; }
        }

        public class PromotionDetailReq : PromotionDeviceReq
        {
            public string BonusCode { set; get; }
        }
        
        public class SetSpecialCodeReq
        {
            public string SpecialCode { set; get; }
            public int? DeviceType { set; get; }
        }

        [Authorize]
        [HttpPost]
        public async Task<ApiResult> SetClaimPromotionAsync(PromotionReq promotionReq)
        {
            var apiResult = new ApiResult(_selLangBLL);
            try
            {
                var authData = _authorizeBLL.GetAuthData();
                int? deviceType = 1;
                if (promotionReq.DeviceType != null)
                {
                    deviceType = promotionReq.DeviceType;
                }
                
                var apiUrl = $"{_bonusServiceUrl}SetClaimPromotion";
                
                var req = new PromotionRequest
                {
                    BonusCode = promotionReq.BonusCode,
                    CustId = authData.CustId,
                    custIP = _tools.GetClientIP(),
                    LockedAmount = promotionReq.LockedAmount,
                    DeviceType = deviceType
                };
                
                apiResult = await _courierAdapter.MyPostAsync<ApiResult, PromotionRequest>(apiUrl, req);
                
            }
            catch (Exception ex)
            {
                apiResult.RawMessage = ex.Message;
                apiResult.Message = ex.Message;
                _logger.LogError(ex, GetCurrentAsyncMethod());
            }

            return apiResult;
        }
        
        [HttpPost]
        public ApiResult<List<CustPromotion>> GetPromotionListV3(PromotionDeviceReq promotionDeviceReq)
        {
            var result = new ApiResult<List<CustPromotion>>();
            try
            {
                var serverInfo = _serverInfoBLL.GetServerInfo();
                var isLogin = User.Identity.IsAuthenticated;
                var custId = 0;
                int currencyId;
                if (isLogin)
                {
                    var authData = _authorizeBLL.GetAuthData();
                    var custInfo = _custInfoBLL.GetCustInfoWithCache(authData.CustId);
                    custId = authData.CustId;
                    currencyId = custInfo.CurrencyId;
                }
                else
                {
                    currencyId = _selLangBLL.GetPreferCurrencyId();
                }
            
                var apiUrl = $"{_bonusServiceUrl}GetPromotionListV3";
                var req = new CustPromotionRequest
                {
                    SiteId = serverInfo.SiteId,
                    CustId = custId,
                    CurrencyId = currencyId,
                    DeviceType = promotionDeviceReq.DeviceType,
                    IsLogin = isLogin,
                    LangId = _selLangBLL.GetLangId(),
                    DefLang = serverInfo.DefLang
                };

                result = _courierAdapter.MyPostAsync<List<CustPromotion>, CustPromotionRequest>(apiUrl, req).Result;
            }
            catch (Exception e)
            {
                _logger.LogError(e, GetCurrentMethod());
            }
            
            return result;
        }

        [HttpPost]
        public ApiResult<CustPromotion> GetPromotionDetail(PromotionDetailReq data)
        {
            var result = new ApiResult<CustPromotion>();
            try
            {
                var serverInfo = _serverInfoBLL.GetServerInfo();
                var isLogin = User.Identity.IsAuthenticated;
                var custId = 0;
                int currencyId;
                if (isLogin)
                {
                    var authData = _authorizeBLL.GetAuthData();
                    var custInfo = _custInfoBLL.GetCustInfoWithCache(authData.CustId);
                    custId = authData.CustId;
                    currencyId = custInfo.CurrencyId;
                }
                else
                {
                    currencyId = _selLangBLL.GetPreferCurrencyId();
                }

                var apiUrl = $"{_bonusServiceUrl}GetPromotionDetail";
                var req = new PromotionDetailRequest
                {
                    SiteId = serverInfo.SiteId,
                    CustId = custId,
                    CurrencyId = currencyId,
                    DeviceType = data.DeviceType,
                    IsLogin = isLogin,
                    LangId = _selLangBLL.GetLangId(),
                    DefLang = serverInfo.DefLang,
                    BonusCode = data.BonusCode
                };

                result = _courierAdapter.MyPostAsync<CustPromotion, PromotionDetailRequest>(apiUrl, req).Result;
            }
            catch (Exception e)
            {
                _logger.LogError(e, GetCurrentMethod());
            }

            return result;
        }

        [Authorize]
        [HttpPost]
        public ApiResult<List<CustPromotion>> GetSpecialPromotionListV2(PromotionDeviceReq promotionDeviceReq)
        {
            var result = new ApiResult<List<CustPromotion>>();
            try
            {
                var serverInfo = _serverInfoBLL.GetServerInfo();
                var authData = _authorizeBLL.GetAuthData();
                var custInfo = _custInfoBLL.GetCustInfoWithCache(authData.CustId);

                var apiUrl = $"{_bonusServiceUrl}GetSpecialPromotionListV2";
                var req = new CustPromotionRequest
                {
                    SiteId = serverInfo.SiteId,
                    CustId = custInfo.CustId,
                    CurrencyId = custInfo.CurrencyId,
                    DeviceType = promotionDeviceReq.DeviceType,
                    LangId = _selLangBLL.GetLangId(),
                    DefLang = serverInfo.DefLang
                };

                result = _courierAdapter.MyPostAsync<List<CustPromotion>, CustPromotionRequest>(apiUrl, req).Result;
            }
            catch (Exception e)
            {
                _logger.LogError(e, GetCurrentMethod());
            }

            return result;
        }

        [Authorize]
        [HttpPost]
        public ApiResult SetSpecialCode(SetSpecialCodeReq setSpecialCodeReq)
        {
            setSpecialCodeReq.DeviceType ??= (int)PromotionDeviceType.App;
            var result = new ApiResult();
            try
            {
                var authData = _authorizeBLL.GetAuthData();

                var apiUrl = $"{_bonusServiceUrl}SetSpecialCode";
                var req = new SpecialCodeRequest
                {
                    CustId = authData.CustId,
                    DeviceType = Convert.ToInt32(setSpecialCodeReq.DeviceType),
                    SpecialCode = setSpecialCodeReq.SpecialCode
                };

                result = _courierAdapter.MyPostAsync<ApiResult, SpecialCodeRequest>(apiUrl, req).Result;
                
                result.Message = result.ErrorCode switch
                {
                    (int)SpecialCode.SpecialCodeError => _selLangBLL.GetMsg("msg_SpecialCodeErr"),
                    9000 => _selLangBLL.GetMsg("msg_Ineligible"),
                    _ => result.Message
                };
            }
            catch (Exception e)
            {
                result.ErrorCode = 9000;
                result.Message = _selLangBLL.GetMsg("msg_Ineligible");
                _logger.LogError(e, GetCurrentMethod());
            }
            
            return result;
        }

        [Authorize]
        [HttpPost]
        public ApiResult<List<CustPromotion>> GetDepositPromotionList(PromotionDeviceReq promotionDeviceReq)
        {
            var result = new ApiResult<List<CustPromotion>>();
            try
            {
                var serverInfo = _serverInfoBLL.GetServerInfo();
                var authData = _authorizeBLL.GetAuthData();

                var apiUrl = $"{_bonusServiceUrl}GetDepositPromotionList";
                var req = new CustPromotionRequest
                {
                    CustId = authData.CustId,
                    DeviceType = promotionDeviceReq.DeviceType,
                    LangId = _selLangBLL.GetLangId(),
                    DefLang = serverInfo.DefLang
                };

                result = _courierAdapter.MyPostAsync<List<CustPromotion>, CustPromotionRequest>(apiUrl, req).Result;

            }
            catch (Exception e)
            {
                _logger.LogError(e, GetCurrentMethod());
            }

            return result;
        }

        public class SetDepositPromotionQueueReq
        {
            public string TransId { set; get; }
            public string BonusCode { set; get; }
            public decimal? LockedAmount { set; get; }
            public int? DeviceType { set; get; }

        }

        [Authorize]
        [HttpPost]
        public ApiResult SetDepositPromotionQueue(SetDepositPromotionQueueReq setDepositPromotionQueueReq)
        {
            var deviceType = 1;
            if (setDepositPromotionQueueReq.DeviceType != null)
            {
                deviceType = (int)setDepositPromotionQueueReq.DeviceType;
            }
            var authData = _authorizeBLL.GetAuthData();
            var apiResult = new ApiResult();
            var lockedAmount = setDepositPromotionQueueReq.LockedAmount ?? 0;
            var custInfo = _custInfoBLL.GetCustInfoWithCache(authData.CustId);
            try
            {
                if (_promotionBLL.SetDepositPromotionQueue(setDepositPromotionQueueReq.TransId, setDepositPromotionQueueReq.BonusCode, authData.CustId, custInfo.SiteId, custInfo.UserName, custInfo.CurrencyId, deviceType, lockedAmount))
                {
                    apiResult.ErrorCode = 0;
                }
            }
            catch (MySql.Data.MySqlClient.MySqlException mySqlEx)
            {
                if (mySqlEx.Number == 1062)/*Duplicate for key 'PRIMARY'*/
                {
                    apiResult.ErrorCode = 1062;
                    apiResult.Message = "Duplicate";
                }
                else
                {
                    apiResult.ErrorCode = 9000;
                    apiResult.Message = mySqlEx.ToString();
                }
            }
            catch (Exception ex)
            {
                apiResult.ErrorCode = 9000;
                apiResult.Message = ex.ToString();
            }

            return apiResult;
        }
    }
}