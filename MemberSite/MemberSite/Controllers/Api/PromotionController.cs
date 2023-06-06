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
        private readonly SelLangBLL _selLangBll;
        private readonly PromotionBll _promotionBll;
        private readonly ServerInfoBll _serverInfoBll;
        private readonly AuthorizeBLL _authorizeBll;
        private readonly CustInfoBll _custInfoBll;
        private readonly ShieldBLL _shieldBll;
        private readonly string _bonusServiceUrl;
        private readonly CourierAdapter _courierAdapter;
        private readonly ILogger<PromotionController> _logger;
        private readonly Tools _tools;

        public PromotionController(IServiceProvider provider)
        {
            _selLangBll = provider.GetRequiredService<SelLangBLL>();
            _promotionBll = provider.GetRequiredService<PromotionBll>();
            _serverInfoBll = provider.GetRequiredService<ServerInfoBll>();
            _authorizeBll = provider.GetRequiredService<AuthorizeBLL>();
            _custInfoBll = provider.GetRequiredService<CustInfoBll>();
            _shieldBll = provider.GetRequiredService<ShieldBLL>();
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
            public int DeviceType { set; get; }
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
            public int PromotionType { set; get; }
        }

        public class PromotionDetailRequest : CustPromotionRequest
        {
            public string BonusCode { set; get; }
        }

        public class PromotionDeviceReq
        {
            public int DeviceType { set; get; }
            public int? CurrencyId { set; get; }
        }

        public class PromotionDetailReq : PromotionDeviceReq
        {
            public string BonusCode { set; get; }
            public int? CurrencyId { set; get; }
        }

        public class SetSpecialCodeReq
        {
            public string SpecialCode { set; get; }
            public int? DeviceType { set; get; }
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
        public async Task<ApiResult> SetClaimPromotionAsync(PromotionReq promotionReq)
        {
            var apiResult = new ApiResult(_selLangBll);
            try
            {
                var authData = _authorizeBll.GetAuthData();
                if (authData is { CustId: > 0 })
                {
                    var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
                    var deviceType = (int)PromotionDeviceType.All;
                    if (promotionReq.DeviceType != null)
                    {
                        deviceType = Convert.ToInt32(promotionReq.DeviceType);
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

                    if (custInfo.SiteId != (int)SiteId.Nova88)
                    {
                        _ = Task.Run(async () =>
                        {
                            var checkResult = _shieldBll.CheckCustShieldRisk(
                                custInfo,
                                ShieldRiskTriggeredPoint.ClaimBonus,
                                ShieldRiskTaggedIssue.HighRisk);
                        });
                    }
                }
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
                var serverInfo = _serverInfoBll.GetServerInfo();
                var authData = _authorizeBll.GetAuthData();
                var isLogin = authData is { CustId: > 0 };
                var custId = 0;
                int currencyId;
                if (isLogin)
                {
                    var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
                    custId = authData.CustId;
                    currencyId = custInfo.CurrencyId;
                }
                else
                {
                    currencyId = _selLangBll.GetPreferCurrencyId();

                    if (promotionDeviceReq.CurrencyId.HasValue)
                    {
                        currencyId = promotionDeviceReq.CurrencyId.Value;
                    }
                }

                var apiUrl = $"{_bonusServiceUrl}GetPromotionListV3";
                var req = new CustPromotionRequest
                {
                    SiteId = serverInfo.SiteId,
                    CustId = custId,
                    CurrencyId = currencyId,
                    DeviceType = promotionDeviceReq.DeviceType,
                    IsLogin = isLogin,
                    LangId = _selLangBll.GetLangId(),
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
                var serverInfo = _serverInfoBll.GetServerInfo();
                var authData = _authorizeBll.GetAuthData();
                var isLogin = authData is { CustId: > 0 };
                var custId = 0;
                int currencyId;
                if (isLogin)
                {
                    var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
                    custId = authData.CustId;
                    currencyId = custInfo.CurrencyId;
                }
                else
                {
                    currencyId = _selLangBll.GetPreferCurrencyId();
                    if (data.CurrencyId.HasValue)
                    {
                        currencyId = data.CurrencyId.Value;
                    }
                }

                var apiUrl = $"{_bonusServiceUrl}GetPromotionDetail";
                var req = new PromotionDetailRequest
                {
                    SiteId = serverInfo.SiteId,
                    CustId = custId,
                    CurrencyId = currencyId,
                    DeviceType = data.DeviceType,
                    IsLogin = isLogin,
                    LangId = _selLangBll.GetLangId(),
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
                var serverInfo = _serverInfoBll.GetServerInfo();
                var authData = _authorizeBll.GetAuthData();
                if (authData is { CustId: > 0 })
                {
                    var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);

                    var apiUrl = $"{_bonusServiceUrl}GetSpecialPromotionListV2";
                    var req = new CustPromotionRequest
                    {
                        SiteId = serverInfo.SiteId,
                        CustId = custInfo.CustId,
                        CurrencyId = custInfo.CurrencyId,
                        DeviceType = promotionDeviceReq.DeviceType,
                        LangId = _selLangBll.GetLangId(),
                        DefLang = serverInfo.DefLang
                    };

                    result = _courierAdapter.MyPostAsync<List<CustPromotion>, CustPromotionRequest>(apiUrl, req).Result;
                }
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
                var authData = _authorizeBll.GetAuthData();

                if (authData is { CustId: > 0 })
                {
                    var apiUrl = $"{_bonusServiceUrl}SetSpecialCode";
                    var req = new SpecialCodeRequest
                    {
                        CustId = authData.CustId,
                        DeviceType = Convert.ToInt32(setSpecialCodeReq.DeviceType),
                        SpecialCode = setSpecialCodeReq.SpecialCode
                    };

                    result = _courierAdapter.MyPostAsync<ApiResult, SpecialCodeRequest>(apiUrl, req).Result;
                }

                result.Message = result.ErrorCode switch
                {
                    (int)SpecialCode.SpecialCodeError => _selLangBll.GetMsg("msg_SpecialCodeErr"),
                    (int)SpecialCode.SpecialCodeVipLevelsOrBlockTagInvalid => _selLangBll.GetMsg("msg_SpecialCodeVipLevelsOrBlockTagInvalid"),
                    101 => _selLangBll.GetMsg("msg_PromoCodeHasBeenUsed"),
                    9000 => _selLangBll.GetMsg("msg_Ineligible"),
                    _ => result.Message
                };
            }
            catch (Exception e)
            {
                result.ErrorCode = 9000;
                result.Message = _selLangBll.GetMsg("msg_Ineligible");
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
                var serverInfo = _serverInfoBll.GetServerInfo();
                var authData = _authorizeBll.GetAuthData();

                if (authData is { CustId: > 0 })
                {
                    var apiUrl = $"{_bonusServiceUrl}GetDepositPromotionList";
                    var req = new CustPromotionRequest
                    {
                        CustId = authData.CustId,
                        DeviceType = promotionDeviceReq.DeviceType,
                        LangId = _selLangBll.GetLangId(),
                        DefLang = serverInfo.DefLang
                    };

                    result = _courierAdapter.MyPostAsync<List<CustPromotion>, CustPromotionRequest>(apiUrl, req).Result;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, GetCurrentMethod());
            }

            return result;
        }

        [Authorize]
        [HttpPost]
        public ApiResult SetDepositPromotionQueue(SetDepositPromotionQueueReq request)
        {
            var result = new ApiResult
            {
                ErrorCode = 0
            };
            try
            {
                if (!_promotionBll.VerifyBonusCode(request.BonusCode))
                {
                    result.ErrorCode = 9000;
                    result.Message = _selLangBll.GetMsg("msg_Ineligible");
                    return result;
                }

                var authData = _authorizeBll.GetAuthData();
                if (authData is { CustId: > 0 })
                {
                    var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
                    var req = new DepositPromotionQueue
                    {
                        TransId = request.TransId,
                        BonusCode = request.BonusCode,
                        CustId = authData.CustId,
                        SiteId = custInfo.SiteId,
                        UserName = custInfo.UserName,
                        CurrencyId = custInfo.CurrencyId,
                        DeviceType = request.DeviceType ?? 1,
                        LockedAmount = request.LockedAmount ?? 0,
                        ClientIp = _tools.GetClientIP()
                    };
                    if (!_promotionBll.SetDepositPromotionQueue(req))
                    {
                        result.ErrorCode = 9000;
                        result.Message = _selLangBll.GetMsg("msg_Ineligible");
                    }
                }
                else
                {
                    result.ErrorCode = 9000;
                    result.Message = _selLangBll.GetMsg("msg_Ineligible");
                }
            }
            catch (MySql.Data.MySqlClient.MySqlException mySqlEx)
            {
                if (mySqlEx.Number == 1062)/*Duplicate for key 'PRIMARY'*/
                {
                    result.ErrorCode = 1062;
                    result.Message = "Duplicate";
                }
                else
                {
                    result.ErrorCode = 9000;
                    result.Message = _selLangBll.GetMsg("msg_Ineligible");
                    _logger.LogError(mySqlEx, GetCurrentMethod());
                }
            }
            catch (Exception e)
            {
                result.ErrorCode = 9000;
                result.Message = _selLangBll.GetMsg("msg_Ineligible");
                _logger.LogError(e, GetCurrentMethod());
            }

            return result;
        }

        [HttpPost]
        public async Task<ApiResult<IEnumerable<RebateExcludeGameDetail>>> GetRebateExcludeGameListAsync([FromRoute] string langId = "en")
        {
            var result = new ApiResult<IEnumerable<RebateExcludeGameDetail>>();
            try
            {
                var serverInfo = _serverInfoBll.GetServerInfo();
                var authData = _authorizeBll.GetAuthData();
                var isLogin = authData is { CustId: > 0 };
                int currencyId = serverInfo.DefCurrency;
                int userLevel = 1;
                if (isLogin)
                {
                    var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
                    currencyId = custInfo.CurrencyId;
                    userLevel = custInfo.UserLevel;
                }
                else
                    currencyId = _selLangBll.GetPreferCurrencyId();


                var apiUrl = $"{_bonusServiceUrl}GetRebateExcludeGameList";
                var req = new GetRebateExcludeGameListRequest
                {
                    SiteId = serverInfo.SiteId,
                    CurrencyId = currencyId,
                    VipLevel = userLevel,
                    LangId = langId
                };

                result = await _courierAdapter.MyPostAsync<IEnumerable<RebateExcludeGameDetail>, GetRebateExcludeGameListRequest>(apiUrl, req);
            }
            catch (Exception e)
            {
                _logger.LogError(e, GetCurrentMethod());
            }
            return result;

        }
    }
}