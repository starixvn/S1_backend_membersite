using System.Net;

using MemberSite.Service.BLL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using MemberSite.Service.Model.Interfaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using static MemberSite.Controllers.Api.PromotionController;
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
        private readonly ICacheHelper _cacheHelper;
        private readonly CheckAttackBLL _checkAttackBll;

        public PromotionController(IServiceProvider provider)
        {
            _selLangBll = provider.GetRequiredService<SelLangBLL>();
            _promotionBll = provider.GetRequiredService<PromotionBll>();
            _serverInfoBll = provider.GetRequiredService<ServerInfoBll>();
            _authorizeBll = provider.GetRequiredService<AuthorizeBLL>();
            _custInfoBll = provider.GetRequiredService<CustInfoBll>();
            _shieldBll = provider.GetRequiredService<ShieldBLL>();
            var configBll = provider.GetRequiredService<ConfigBLL>();
            _bonusServiceUrl = configBll.GetConfigValueByEnvironment<ServiceHostConfig>("ServiceHost").BonusService;
            _courierAdapter = provider.GetRequiredService<CourierAdapter>();
            _logger = provider.GetRequiredService<ILogger<PromotionController>>();
            _tools = provider.GetRequiredService<Tools>();
            _cacheHelper = provider.GetRequiredService<ICacheHelper>();
            _checkAttackBll = provider.GetRequiredService<CheckAttackBLL>();
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

        public class CustPromotionRequest : IRedisKey
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

        public class PromotionDetailRequest : CustPromotionRequest, IRedisKey
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

        public class GetRebateExcludeGameListReq
        {
            public int CurrencyId { set; get; }
        }

        public class CustPrivatePromotionReq
        {
            public int DeviceType { set; get; }
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
                    var key = $"SetClaimPromotion:{authData.CustId}";

                    if (!await _cacheHelper.SetNxAsync(key, string.Empty, TimeSpan.FromSeconds(5)))
                    {
                        apiResult.ErrorCode = -100;
                        apiResult.Message = _selLangBll.GetMsg("msg_Ineligible");

                        _logger.LogInformation("Block duplicate claim bonus");

                        return apiResult;
                    }

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

                    if (custInfo.SiteId is 2 or 3) // bwing and goal
                    {
                        _ = Task.Run(() =>
                        {
                            _ = _shieldBll.CheckCustShieldRisk(
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
        public async Task<ApiResult<List<CustPromotion>>> GetPromotionListV3(PromotionDeviceReq input)
        {
            try
            {
                if (await _checkAttackBll.CheckAttackIp(Request, 100))
                {
                    Request.HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                    return new ApiResult<List<CustPromotion>>
                    {
                        ErrorCode = (int)ErrorCode.Attack,
                        Data = new List<CustPromotion>(),
                        Message = "GetPromotionListV3 request exceeds the limit count."
                    };
                }

                CustPromotionRequest request;
                var serverInfo = _serverInfoBll.GetServerInfo();
                var authData = _authorizeBll.GetAuthData();
                var isLogin = authData is { CustId: > 0 };
                if (isLogin)
                {
                    var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
                    request = new CustPromotionRequest
                    {
                        SiteId = serverInfo.SiteId,
                        CustId = authData.CustId,
                        CurrencyId = custInfo.CurrencyId,
                        DeviceType = input.DeviceType,
                        IsLogin = isLogin,
                        LangId = _selLangBll.GetLangId(),
                        DefLang = serverInfo.DefLang
                    };
                }
                else
                {
                    request = new CustPromotionRequest
                    {
                        SiteId = serverInfo.SiteId,
                        CustId = 0,
                        CurrencyId = input.CurrencyId.HasValue ? input.CurrencyId.Value : _selLangBll.GetPreferCurrencyId(),
                        DeviceType = input.DeviceType,
                        IsLogin = isLogin,
                        LangId = _selLangBll.GetLangId(),
                        DefLang = serverInfo.DefLang
                    };
                }

                // 未登入檢查是否有Cache
                var key = $"GetPromotionListV3:{((IRedisKey)request).GetRedisKey()}";
                if (!isLogin && _cacheHelper.TryGetValue(key, out List<CustPromotion> custPromotionList))
                {
                    return new ApiResult<List<CustPromotion>>
                    {
                        ErrorCode = (int)ErrorCode.Success,
                        Data = custPromotionList
                    };
                }

                var apiUrl = $"{_bonusServiceUrl}GetPromotionListV3";
                var result = await _courierAdapter.MyPostAsync<List<CustPromotion>, CustPromotionRequest>(apiUrl, request);

                // 新增未登入快取
                if (!isLogin && result.Data != null)
                {
                    _cacheHelper.Create(key, result.Data, TimeSpan.FromMinutes(5));
                }
                return result;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return new ApiResult<List<CustPromotion>>();
            }
        }

        [HttpPost]
        public async Task<ApiResult<CustPromotion>> GetPromotionDetail(PromotionDetailReq input)
        {
            try
            {
                PromotionDetailRequest request;
                var serverInfo = _serverInfoBll.GetServerInfo();
                var authData = _authorizeBll.GetAuthData();
                var isLogin = authData is { CustId: > 0 };
                if (isLogin)
                {
                    var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
                    request = new PromotionDetailRequest
                    {
                        SiteId = serverInfo.SiteId,
                        CustId = authData.CustId,
                        CurrencyId = custInfo.CurrencyId,
                        DeviceType = input.DeviceType,
                        IsLogin = isLogin,
                        LangId = _selLangBll.GetLangId(),
                        DefLang = serverInfo.DefLang,
                        BonusCode = input.BonusCode
                    };
                }
                else
                {
                    request = new PromotionDetailRequest
                    {
                        SiteId = serverInfo.SiteId,
                        CustId = 0,
                        CurrencyId = input.CurrencyId.HasValue ? input.CurrencyId.Value : _selLangBll.GetPreferCurrencyId(),
                        DeviceType = input.DeviceType,
                        IsLogin = isLogin,
                        LangId = _selLangBll.GetLangId(),
                        DefLang = serverInfo.DefLang,
                        BonusCode = input.BonusCode
                    };
                }

                // 未登入檢查是否有Cache
                var key = $"GetPromotionDetail:{((IRedisKey)request).GetRedisKey()}";
                if (!isLogin && _cacheHelper.TryGetValue(key, out CustPromotion custPromotion))
                {
                    return new ApiResult<CustPromotion>
                    {
                        ErrorCode = (int)ErrorCode.Success,
                        Data = custPromotion
                    };
                }

                var apiUrl = $"{_bonusServiceUrl}GetPromotionDetail";
                var result = await _courierAdapter.MyPostAsync<CustPromotion, PromotionDetailRequest>(apiUrl, request);

                // 新增未登入快取
                if (!isLogin && result.Data != null)
                {
                    _cacheHelper.Create(key, result.Data, TimeSpan.FromMinutes(5));
                }
                return result;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return new ApiResult<CustPromotion>();
            }
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
        public async Task<ApiResult<IEnumerable<RebateExcludeGameDetail>>> GetRebateExcludeGameListAsync(GetRebateExcludeGameListReq input, [FromRoute] string langId = "en")
        {
            try
            {
                GetRebateExcludeGameListRequest request;
                var serverInfo = _serverInfoBll.GetServerInfo();
                if (!serverInfo.ICurrencyList.Contains(input.CurrencyId))
                {
                    return new ApiResult<IEnumerable<RebateExcludeGameDetail>>()
                    {
                        Data = new List<RebateExcludeGameDetail>(),
                        ErrorCode = 0
                    };
                }

                var authData = _authorizeBll.GetAuthData();
                _logger.LogInformation($"authData={JsonConvert.SerializeObject(authData)}.");
                var isLogin = authData is { CustId: > 0 };

                if (isLogin)
                {
                    var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
                    request = new GetRebateExcludeGameListRequest
                    {
                        SiteId = serverInfo.SiteId,
                        CurrencyId = custInfo.CurrencyId,
                        VipLevel = custInfo.UserLevel,
                        LangId = langId
                    };
                }
                else
                {
                    request = new GetRebateExcludeGameListRequest
                    {
                        SiteId = serverInfo.SiteId,
                        CurrencyId = input.CurrencyId,
                        VipLevel = 1,
                        LangId = langId
                    };
                }

                // 未登入檢查是否有Cache
                var key = $"GetRebateExcludeGameList:{((IRedisKey)request).GetRedisKey()}";
                if (!isLogin && _cacheHelper.TryGetValue(key, out IEnumerable<RebateExcludeGameDetail> rebateExcludeGameDetailList))
                {
                    return new ApiResult<IEnumerable<RebateExcludeGameDetail>>
                    {
                        ErrorCode = (int)ErrorCode.Success,
                        Data = rebateExcludeGameDetailList
                    };
                }

                var apiUrl = $"{_bonusServiceUrl}GetRebateExcludeGameList";
                var result = await _courierAdapter.MyPostAsync<IEnumerable<RebateExcludeGameDetail>, GetRebateExcludeGameListRequest>(apiUrl, request);

                // 新增未登入快取
                if (!isLogin && result.Data != null)
                {
                    _cacheHelper.Create(key, result.Data, TimeSpan.FromMinutes(5));
                }
                return result;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return new ApiResult<IEnumerable<RebateExcludeGameDetail>>();
            }
        }

        [Authorize]
        [HttpPost]
        public ApiResult<PopUpPromotion> GetPopUpPromotion(PromotionDeviceReq data)
        {
            var result = new ApiResult<PopUpPromotion>();
            try
            {

                var authData = _authorizeBll.GetAuthData();
                if (authData is { CustId: > 0 })
                {
                    var serverInfo = _serverInfoBll.GetServerInfo();
                    var custId = authData.CustId;
                    var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
                    var currencyId = custInfo.CurrencyId;

                    var apiUrl = $"{_bonusServiceUrl}GetPopUpPromotion";
                    var req = new CustPromotionRequest
                    {
                        SiteId = serverInfo.SiteId,
                        CustId = custId,
                        CurrencyId = currencyId,
                        DeviceType = data.DeviceType,
                        LangId = _selLangBll.GetLangId(),
                        IsLogin = true
                    };

                    result = _courierAdapter.MyPostAsync<PopUpPromotion, CustPromotionRequest>(apiUrl, req).Result;
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
        public ApiResult<PopUpPromotion> GetRetentionPromotion(PromotionDeviceReq data)
        {
            var result = new ApiResult<PopUpPromotion>();
            try
            {
                var authData = _authorizeBll.GetAuthData();
                if (authData is { CustId: > 0 })
                {
                    var serverInfo = _serverInfoBll.GetServerInfo();
                    var custId = authData.CustId;
                    var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
                    var currencyId = custInfo.CurrencyId;

                    var apiUrl = $"{_bonusServiceUrl}GetRetentionPromotion";
                    var req = new CustPromotionRequest
                    {
                        SiteId = serverInfo.SiteId,
                        CustId = custId,
                        CurrencyId = currencyId,
                        DeviceType = data.DeviceType,
                        LangId = _selLangBll.GetLangId(),
                        IsLogin = true
                    };

                    result = _courierAdapter.MyPostAsync<PopUpPromotion, CustPromotionRequest>(apiUrl, req).Result;
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
        public ApiResult<List<CustPromotion>> GetCustPrivatePromotionList(CustPrivatePromotionReq data)
        {
            var result = new ApiResult<List<CustPromotion>>();
            try
            {
                var serverInfo = _serverInfoBll.GetServerInfo();
                var authData = _authorizeBll.GetAuthData();

                if (authData is { CustId: > 0 })
                {
                    var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);

                    var apiUrl = $"{_bonusServiceUrl}GetCustPrivatePromotionList";

                    var req = new CustPromotionRequest
                    {
                        SiteId = serverInfo.SiteId,
                        CustId = custInfo.CustId,
                        CurrencyId = custInfo.CurrencyId,
                        DeviceType = data.DeviceType,
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
        public async Task<ApiResult<List<CustPromotion>>> GetPromotionUploadCustList(PromotionDeviceReq input)
        {
            try
            {

                CustPromotionRequest request;
                var serverInfo = _serverInfoBll.GetServerInfo();
                var authData = _authorizeBll.GetAuthData();
                var isLogin = authData is { CustId: > 0 };
                if (isLogin)
                {
                    var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
                    request = new CustPromotionRequest
                    {
                        SiteId = serverInfo.SiteId,
                        CustId = authData.CustId,
                        CurrencyId = custInfo.CurrencyId,
                        DeviceType = input.DeviceType,
                        IsLogin = isLogin,
                        LangId = _selLangBll.GetLangId(),
                        DefLang = serverInfo.DefLang
                    };
                }
                else
                {
                    request = new CustPromotionRequest
                    {
                        SiteId = serverInfo.SiteId,
                        CustId = 0,
                        CurrencyId = input.CurrencyId.HasValue ? input.CurrencyId.Value : _selLangBll.GetPreferCurrencyId(),
                        DeviceType = input.DeviceType,
                        IsLogin = isLogin,
                        LangId = _selLangBll.GetLangId(),
                        DefLang = serverInfo.DefLang
                    };
                }

                var apiUrl = $"{_bonusServiceUrl}GetPromotionUploadCustList";
                var result = await _courierAdapter.MyPostAsync<List<CustPromotion>, CustPromotionRequest>(apiUrl, request);

                if(result.Data != null)
                {
                    result.Data.ForEach(item => item.IsPromotionUploadCustList = true);
                }

                return result;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return new ApiResult<List<CustPromotion>>();
            }
        }
        [Authorize]
        [HttpPost]
        public async Task<ApiResult> SetClaimPromotionForUploadCustListAsync(PromotionReq promotionReq)
        {
            var apiResult = new ApiResult(_selLangBll);
            try
            {
                var authData = _authorizeBll.GetAuthData();
                if (authData is { CustId: > 0 })
                {
                    var key = $"SetClaimPromotionForUploadCustList:{authData.CustId}";

                    if (!await _cacheHelper.SetNxAsync(key, string.Empty, TimeSpan.FromSeconds(5)))
                    {
                        apiResult.ErrorCode = -100;
                        apiResult.Message = _selLangBll.GetMsg("msg_Ineligible");

                        _logger.LogInformation("Block duplicate claim bonus");

                        return apiResult;
                    }

                    var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
                    var deviceType = (int)PromotionDeviceType.All;
                    if (promotionReq.DeviceType != null)
                    {
                        deviceType = Convert.ToInt32(promotionReq.DeviceType);
                    }

                    var apiUrl = $"{_bonusServiceUrl}SetClaimPromotionForUploadCustList";

                    var req = new PromotionRequest
                    {
                        BonusCode = promotionReq.BonusCode,
                        CustId = authData.CustId,
                        custIP = _tools.GetClientIP(),
                        LockedAmount = promotionReq.LockedAmount,
                        DeviceType = deviceType
                    };

                    apiResult = await _courierAdapter.MyPostAsync<ApiResult, PromotionRequest>(apiUrl, req);

                    if (custInfo.SiteId is 2 or 3) // bwing and goal
                    {
                        _ = Task.Run(() =>
                        {
                            _ = _shieldBll.CheckCustShieldRisk(
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
    }
}