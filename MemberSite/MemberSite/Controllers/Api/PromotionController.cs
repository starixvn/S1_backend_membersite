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

namespace MemberSite.Controllers.Api
{
    [Route("api/[controller]/[action]/{LangId}")]
    [ApiController]
    public class PromotionController : ControllerBase
    {
        private readonly SelLangBLL selLangBLL;
        private readonly PromotionBLL promotionBLL;
        private readonly ServerInfoBLL serverInfoBLL;
        private readonly AuthorizeBLL authorizeBLL;
        private readonly CustInfoBLL custInfoBLL;
        private readonly CourierAdapter courierAdapter;
        private readonly WalletBLL walletBLL;
        private readonly BonusWalletBLL bonusWalletBLL;
        private readonly string tpApiUrl;
        private readonly int mainWalletType;
        private readonly int bonusType;

        public PromotionController(IConfiguration configuration, SelLangBLL selLangBLL, PromotionBLL promotionBLL, ServerInfoBLL serverInfoBLL, AuthorizeBLL authorizeBLL, CustInfoBLL custInfoBLL, CourierAdapter courierAdapter, WalletBLL walletBLL, BonusWalletBLL bonusWalletBLL)
        {
            this.selLangBLL = selLangBLL;
            this.promotionBLL = promotionBLL;
            this.serverInfoBLL = serverInfoBLL;
            this.authorizeBLL = authorizeBLL;
            this.custInfoBLL = custInfoBLL;
            this.courierAdapter = courierAdapter;
            this.walletBLL = walletBLL;
            this.bonusWalletBLL = bonusWalletBLL;

            tpApiUrl = configuration.GetValue<string>("TpService");
            mainWalletType = configuration.GetValue<int>("MainWalletType");
            bonusType = configuration.GetValue<int>("BonusType");
        }

        public ApiResult<IEnumerable<Promotion>> GetPromotionList()
        {
            var apiResult = new ApiResult<IEnumerable<Promotion>>(selLangBLL);

            try
            {
                IEnumerable<Promotion> promotionList = null;

                if (User.Identity.IsAuthenticated)
                {
                    var authData = authorizeBLL.GetAuthData();
                    promotionList = promotionBLL.GetPromotionListByCustId(authData.CustId);
                    if (promotionList != null)
                    {
                        foreach (var promotion in promotionList)
                        {
                            if (promotion.BonusInfoStatus == 1)
                            {
                                promotion.custRollOver = custInfoBLL.GetCustRollOver(authData.CustId, promotion.BonusCode);
                            }
                        }
                    }
                }
                else
                {
                    var serverInfo = serverInfoBLL.GetServerInfo();
                    var ccccurrencyId = selLangBLL.GetPreferCurrencyId();
                    promotionList = promotionBLL.GetPromotionListWithCache(serverInfo.SiteId).Where(x => x.CurrencyId == ccccurrencyId);
                }

                if (promotionList != null)
                {
                    foreach (var promotion in promotionList)
                    {
                        promotion.Title = selLangBLL.GetResourceValue($"{promotion.BonusCode}Title");
                        promotion.Description = selLangBLL.GetResourceValue($"{promotion.BonusCode}Description");
                        if (promotion.CurrencyId == 13)
                        {
                            promotion.Title = selLangBLL.GetResourceValue($"{promotion.BonusCode}Title13");
                            promotion.Description = selLangBLL.GetResourceValue($"{promotion.BonusCode}Description13");
                        }

                        promotion.CategoryName = selLangBLL.GetResourceValue($"PromotionCategory{promotion.CategoryId}");
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
            var apiResult = new ApiResult(selLangBLL);
            try
            {
                var authData = authorizeBLL.GetAuthData();
                int? deviceType = 1;
                if (promotionReq.DeviceType != null)
                {
                    deviceType = promotionReq.DeviceType;
                }
                apiResult = await promotionBLL.SetClaimPromotionAsync(authData.CustId, promotionReq.BonusCode, promotionReq.LockedAmount, deviceType);

                //if (result > 0)
                //{
                //    if (mainWalletType == 2)
                //    {
                //        ApiResult<TransferResult> transferToSabaResult = null;
                //        var input = new TpTransfer();
                //        input.Amount = walletBLL.GetBalance(authData.CustId);
                //        input.CustId = authData.CustId;
                //        input.direction = TpDirection.ToTp;
                //        input.TpId = 1;
                //        transferToSabaResult = await TransferToSabaAsync(input);
                //    }

                //    apiResult.ErrorCode = 0;
                //}
            }
            catch (Exception ex)
            {
                apiResult.RawMessage = ex.Message;
                apiResult.Message = ex.Message;
            }

            return apiResult;
        }

        public enum TpDirection
        {
            ToTp = 1,
            ToMain = 2
        }
        private async Task<ApiResult<TransferResult>> TransferToSabaAsync(TpTransfer inTpTransfer)
        {
            decimal amount = inTpTransfer.Amount;
            if (amount.ToString().IndexOf(".") > -1)
            {
                decimal.TryParse(amount.ToString("#.##"), out amount);
            }

            var tpTransfer = new TpTransfer
            {
                TpId = inTpTransfer.TpId,
                CustId = inTpTransfer.CustId,
                Amount = amount,
                direction = inTpTransfer.direction,
                inputs = null
            };
            var myUrl = $"{tpApiUrl}Transfer";
            var data = await courierAdapter.MyPostAsync<TransferResult, TpTransfer>(myUrl, tpTransfer);

            return data;
        }
        [HttpPost]
        public ApiResult<List<CustPromotion>> GetPromotionListV2()
        {
            var result = new ApiResult<List<CustPromotion>>();

            var serverInfo = serverInfoBLL.GetServerInfo();
            var isLogin = User.Identity.IsAuthenticated;
            int custId = 0;
            int currencyId;
            if (isLogin)
            {
                var authData = authorizeBLL.GetAuthData();
                var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);
                custId = authData.CustId;
                currencyId = custInfo.CurrencyId;
            }
            else
            {
                currencyId = selLangBLL.GetPreferCurrencyId();
            }

            result.Data = promotionBLL.GetPromotionListV2(serverInfo.SiteId, custId, currencyId, isLogin, selLangBLL.GetLangId(), serverInfo.DefLang);
            result.ErrorCode = 0;

            return result;
        }
        public class PromotionDeviceReq
        {
            public int DeviceType { set; get; }
        }
        [HttpPost]
        public ApiResult<List<CustPromotion>> GetPromotionListV3(PromotionDeviceReq promotionDeviceReq)
        {
            var result = new ApiResult<List<CustPromotion>>();

            var serverInfo = serverInfoBLL.GetServerInfo();
            var isLogin = User.Identity.IsAuthenticated;
            int custId = 0;
            int currencyId;
            if (isLogin)
            {
                var authData = authorizeBLL.GetAuthData();
                var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);
                custId = authData.CustId;
                currencyId = custInfo.CurrencyId;
            }
            else
            {
                currencyId = selLangBLL.GetPreferCurrencyId();
            }

            result.Data = promotionBLL.GetPromotionListV3(serverInfo.SiteId, custId, currencyId, promotionDeviceReq.DeviceType, isLogin, selLangBLL.GetLangId(), serverInfo.DefLang);
            result.ErrorCode = 0;

            return result;
        }


        [HttpPost]
        public ApiResult<CustPromotion> GetPromotionDetail(CustPromotion data)
        {
            var result = new ApiResult<CustPromotion>();

            var serverInfo = serverInfoBLL.GetServerInfo();
            var isLogin = User.Identity.IsAuthenticated;
            int custId = 0;
            int currencyId;
            int? DeviceType = 1;
            if (isLogin)
            {
                var authData = authorizeBLL.GetAuthData();
                var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);
                custId = authData.CustId;
                currencyId = custInfo.CurrencyId;
            }
            else
            {
                currencyId = selLangBLL.GetPreferCurrencyId();
            }

            if (data.DeviceType != null)
            {
                DeviceType = data.DeviceType;
            }

            result.Data = promotionBLL.GetPromotionDetail(serverInfo.SiteId, custId, currencyId, isLogin, selLangBLL.GetLangId(), serverInfo.DefLang, data.BonusCode, DeviceType);
            result.ErrorCode = 0;

            return result;
        }

        public class TpTransfer
        {
            public int TpId { set; get; }
            public int CustId { set; get; }
            public decimal Amount { set; get; }
            public TpDirection direction { set; get; }
            public Dictionary<string, string> inputs { set; get; }
        }

        public class TransferResult
        {
            public TransferResult()
            {
                Status = TransferStatus.Pending;
            }
            public TransferStatus Status { get; set; }
            public string ThirdId { get; set; }
            public decimal Difference { get; set; }
        }
        public enum TransferStatus
        {
            Success = 0, Fail = 1, Pending = 2, PendingD = 3
        }

        [Authorize]
        [HttpPost]
        public ApiResult<List<CustPromotion>> GetSpecialPromotionList()
        {
            var result = new ApiResult<List<CustPromotion>>();

            var serverInfo = serverInfoBLL.GetServerInfo();
            var authData = authorizeBLL.GetAuthData();
            var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);
            result.Data = promotionBLL.GetSpecialPromotionList(serverInfo.SiteId, custInfo.CustId, custInfo.CurrencyId, selLangBLL.GetLangId(), serverInfo.DefLang);
            result.ErrorCode = 0;

            return result;
        }

        [Authorize]
        [HttpPost]
        public ApiResult<List<CustPromotion>> GetSpecialPromotionListV2(PromotionDeviceReq promotionDeviceReq)
        {
            var result = new ApiResult<List<CustPromotion>>();

            var serverInfo = serverInfoBLL.GetServerInfo();
            var authData = authorizeBLL.GetAuthData();
            var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);

            result.Data = promotionBLL.GetSpecialPromotionListV2(serverInfo.SiteId, custInfo.CustId, custInfo.CurrencyId, selLangBLL.GetLangId(), serverInfo.DefLang, promotionDeviceReq.DeviceType);
            result.ErrorCode = 0;

            return result;
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
                Message = selLangBLL.GetMsg("msg_SpecialCodeErr")
            };

            try
            {
                var authData = authorizeBLL.GetAuthData();
                if (promotionBLL.CheckScratchCardCode(setSpecialCodeReq.SpecialCode.ToUpper()))
                {
                    result.ErrorCode = promotionBLL.SetScratchCardCode(authData.CustId, setSpecialCodeReq.SpecialCode.ToUpper(), setSpecialCodeReq.DeviceType);
                    if(result.ErrorCode == 0)
                        result.Message = "";
                }
                else
                {
                    if (promotionBLL.SetSpecialCode(authData.CustId, setSpecialCodeReq.SpecialCode, setSpecialCodeReq.DeviceType))
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
        public class SetSpecialCodeJoinedReq
        {
            public string SpecialCode { set; get; }
            public string BonusCode { set; get; }

        }

        [Authorize]
        [HttpPost]
        public ApiResult<List<CustPromotion>> GetDepositPromotionList(PromotionDeviceReq promotionDeviceReq)
        {
            var result = new ApiResult<List<CustPromotion>>();

            var serverInfo = serverInfoBLL.GetServerInfo();
            var authData = authorizeBLL.GetAuthData();
            int custId = authData.CustId;
           

            result.Data = promotionBLL.GetDepositPromotionList( custId, promotionDeviceReq.DeviceType, selLangBLL.GetLangId(), serverInfo.DefLang);
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
            int deviceType = 1;
            if (setDepositPromotionQueueReq.DeviceType != null)
            {
                deviceType = (int)setDepositPromotionQueueReq.DeviceType;
            }
            var authData = authorizeBLL.GetAuthData();
            var apiResult = new ApiResult();
            var lockedAmount = setDepositPromotionQueueReq.LockedAmount ?? 0;
            var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);
            try
            {
                if (promotionBLL.SetDepositPromotionQueue(setDepositPromotionQueueReq.TransId, setDepositPromotionQueueReq.BonusCode, authData.CustId, custInfo.SiteId, custInfo.UserName, custInfo.CurrencyId, deviceType, lockedAmount))
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