using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MemberSite.Service.BLL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace MemberSite.Controllers.Api
{
    [AllowAnonymous]
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
        private readonly string tpApiUrl;
        private readonly int mainWalletType;

        public PromotionController(IConfiguration configuration, SelLangBLL selLangBLL, PromotionBLL promotionBLL, ServerInfoBLL serverInfoBLL, AuthorizeBLL authorizeBLL, CustInfoBLL custInfoBLL, CourierAdapter courierAdapter, WalletBLL walletBLL)
        {
            this.selLangBLL = selLangBLL;
            this.promotionBLL = promotionBLL;
            this.serverInfoBLL = serverInfoBLL;
            this.authorizeBLL = authorizeBLL;
            this.custInfoBLL = custInfoBLL;
            this.courierAdapter = courierAdapter;
            this.walletBLL = walletBLL;

            tpApiUrl = configuration.GetValue<string>("TpService");
            mainWalletType = configuration.GetValue<int>("MainWalletType");
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
                    var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);
                    promotionList = promotionBLL.GetPromotionListByCustId(authData.CustId);
                    if (promotionList != null)
                    {
                        foreach (var promotion in promotionList)
                        {
                            if (promotion.BonusInfoStatus == 1)
                            {
                                promotion.custRollOver = custInfoBLL.GetCustRollOver(custInfo.CustId, promotion.BonusCode);
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
        }
        [Authorize]
        [HttpPost]
        public async Task<ApiResult> SetClaimPromotionAsync(PromotionReq promotionReq)
        {
            var apiResult = new ApiResult(selLangBLL);

            try
            {
                var authData = authorizeBLL.GetAuthData();
                if (promotionBLL.SetClaimPromotion(authData.CustId, promotionReq.BonusCode) > 0)
                {
                    if (mainWalletType == 2)
                    {
                        ApiResult<TransferResult> transferToSabaResult = null;
                        var input = new TpTransfer();
                        input.Amount = walletBLL.GetBalance(authData.CustId);
                        input.CustId = authData.CustId;
                        input.direction = TpDirection.ToTp;
                        input.TpId = 1;
                        transferToSabaResult = await TransferToSabaAsync(input);
                    }

                    apiResult.ErrorCode = 0;
                }
            }
            catch (Exception ex)
            {
                apiResult.RawMessage = ex.Message;
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
    }
}