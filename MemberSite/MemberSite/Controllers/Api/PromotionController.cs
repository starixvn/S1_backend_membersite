using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MemberSite.Service.BLL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

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

        public PromotionController(IServiceProvider provider)
        {
            _selLangBLL = provider.GetRequiredService<SelLangBLL>();
            _promotionBLL = provider.GetRequiredService<PromotionBll>();
            _serverInfoBLL = provider.GetRequiredService<ServerInfoBll>();
            _authorizeBLL = provider.GetRequiredService<AuthorizeBLL>();
            _custInfoBLL = provider.GetRequiredService<CustInfoBll>();
        }

        public ApiResult<IEnumerable<Promotion>> GetPromotionList()
        {
            var apiResult = new ApiResult<IEnumerable<Promotion>>(_selLangBLL);

            try
            {
                IEnumerable<Promotion> promotionList = null;

                if (User.Identity.IsAuthenticated)
                {
                    var authData = _authorizeBLL.GetAuthData();
                    promotionList = _promotionBLL.GetPromotionListByCustId(authData.CustId);
                    if (promotionList != null)
                    {
                        foreach (var promotion in promotionList)
                        {
                            if (promotion.BonusInfoStatus == 1)
                            {
                                promotion.custRollOver = _custInfoBLL.GetCustRollOver(authData.CustId, promotion.BonusCode);
                            }
                        }
                    }
                }
                else
                {
                    var serverInfo = _serverInfoBLL.GetServerInfo();
                    var ccccurrencyId = _selLangBLL.GetPreferCurrencyId();
                    promotionList = _promotionBLL.GetPromotionListWithCache(serverInfo.SiteId).Where(x => x.CurrencyId == ccccurrencyId);
                }

                if (promotionList != null)
                {
                    foreach (var promotion in promotionList)
                    {
                        promotion.Title = _selLangBLL.GetResourceValue($"{promotion.BonusCode}Title");
                        promotion.Description = _selLangBLL.GetResourceValue($"{promotion.BonusCode}Description");
                        if (promotion.CurrencyId == 13)
                        {
                            promotion.Title = _selLangBLL.GetResourceValue($"{promotion.BonusCode}Title13");
                            promotion.Description = _selLangBLL.GetResourceValue($"{promotion.BonusCode}Description13");
                        }

                        promotion.CategoryName = _selLangBLL.GetResourceValue($"PromotionCategory{promotion.CategoryId}");
                    }
                }


                apiResult.Data = promotionList;
                apiResult.ErrorCode = 0;
            }
            catch (Exception ex)
            {
                apiResult.RawMessage = ex.Message;
            }

            return apiResult;
        }
        public class PromotionReq
        {
            public string BonusCode { set; get; }
            public decimal? LockedAmount { set; get; }
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
                apiResult = await _promotionBLL.SetClaimPromotionV2Async(authData.CustId, promotionReq.BonusCode, promotionReq.LockedAmount, deviceType);
                
            }
            catch (Exception ex)
            {
                apiResult.RawMessage = ex.Message;
                apiResult.Message = ex.Message;
            }

            return apiResult;
        }

        public class PromotionDeviceReq
        {
            public int DeviceType { set; get; }
        }
        [HttpPost]
        public ApiResult<List<CustPromotion>> GetPromotionListV3(PromotionDeviceReq promotionDeviceReq)
        {
            var result = new ApiResult<List<CustPromotion>>();

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

            result.Data = _promotionBLL.GetPromotionListV3(serverInfo.SiteId, custId, currencyId, promotionDeviceReq.DeviceType, isLogin, _selLangBLL.GetLangId(), serverInfo.DefLang);
            result.ErrorCode = 0;

            return result;
        }


        [HttpPost]
        public ApiResult<CustPromotion> GetPromotionDetail(CustPromotion data)
        {
            var result = new ApiResult<CustPromotion>();

            var serverInfo = _serverInfoBLL.GetServerInfo();
            var isLogin = User.Identity.IsAuthenticated;
            var custId = 0;
            int currencyId;
            int? DeviceType = 1;
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

            if (data.DeviceType != null)
            {
                DeviceType = data.DeviceType;
            }

            result.Data = _promotionBLL.GetPromotionDetail(serverInfo.SiteId, custId, currencyId, isLogin, _selLangBLL.GetLangId(), serverInfo.DefLang, data.BonusCode, DeviceType);
            result.ErrorCode = 0;

            return result;
        }

        [Authorize]
        [HttpPost]
        public ApiResult<List<CustPromotion>> GetSpecialPromotionListV2(PromotionDeviceReq promotionDeviceReq)
        {
            var serverInfo = _serverInfoBLL.GetServerInfo();
            var authData = _authorizeBLL.GetAuthData();
            var custInfo = _custInfoBLL.GetCustInfoWithCache(authData.CustId);

            return new ApiResult<List<CustPromotion>>
            {
                Data = _promotionBLL.GetSpecialPromotionListV2(serverInfo.SiteId, custInfo.CustId, custInfo.CurrencyId, _selLangBLL.GetLangId(), serverInfo.DefLang, promotionDeviceReq.DeviceType),
                ErrorCode = 0
            };
        }

        public class SetSpecialCodeReq
        {
            public string SpecialCode { set; get; }
            public int? DeviceType { set; get; }
        }

        [Authorize]
        [HttpPost]
        public ApiResult SetSpecialCode(SetSpecialCodeReq setSpecialCodeReq)
        {
            var result = new ApiResult
            {
                ErrorCode = (int)SpecialCodeErrorcode.SpecialCodeError,
                Message = _selLangBLL.GetMsg("msg_SpecialCodeErr")
            };

            try
            {
                var authData = _authorizeBLL.GetAuthData();
                if (!_promotionBLL.CheckSpecialCode(setSpecialCodeReq.SpecialCode))
                {
                    result.ErrorCode = 99;
                }
                else if (_promotionBLL.CheckScratchCardCode(setSpecialCodeReq.SpecialCode.ToUpper()))
                {
                    result.ErrorCode = _promotionBLL.SetScratchCardCode(authData.CustId, setSpecialCodeReq.SpecialCode.ToUpper(), setSpecialCodeReq.DeviceType);
                    if (result.ErrorCode == 0)
                        result.Message = "";
                }
                else
                {
                    if (_promotionBLL.SetSpecialCode(authData.CustId, setSpecialCodeReq.SpecialCode, setSpecialCodeReq.DeviceType))
                    {
                        result.ErrorCode = 0;
                        result.Message = "";
                    }
                    else
                    {
                        result.ErrorCode = 99;
                    }
                }
            }
            catch (MySql.Data.MySqlClient.MySqlException mySqlEx)
            {
                if (mySqlEx.Number == 1062)/*Duplicate for key 'PRIMARY'*/
                {
                    result.ErrorCode = 0;
                }
                else
                {
                    result.ErrorCode = 9000;
                    result.Message = mySqlEx.ToString();
                }
            }
            catch (Exception ex)
            {
                result.ErrorCode = 9000;
                result.Message = ex.ToString();
            }

            return result;
        }

        [Authorize]
        [HttpPost]
        public ApiResult<List<CustPromotion>> GetDepositPromotionList(PromotionDeviceReq promotionDeviceReq)
        {
            var result = new ApiResult<List<CustPromotion>>();

            var serverInfo = _serverInfoBLL.GetServerInfo();
            var authData = _authorizeBLL.GetAuthData();
            var custId = authData.CustId;
           

            result.Data = _promotionBLL.GetDepositPromotionList( custId, promotionDeviceReq.DeviceType, _selLangBLL.GetLangId(), serverInfo.DefLang);
            result.ErrorCode = 0;

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