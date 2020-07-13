using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MemberSite.Service.Model;
using MemberSite.Service.BLL;
using System.Security.Claims;
using MemberSite.Service.Library;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using MemberSite.Service.Model.enums;

namespace MemberSite.Controllers.Api
{
    [Authorize]
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly CustInfoBLL custInfoBLL;
        private readonly Tools tools;
        private readonly SelLangBLL selLangBLL;
        private readonly AuthorizeBLL authorizeBLL;
        private readonly BankInfoBLL bankInfoBLL;
        private readonly WalletBLL walletBLL;
        private readonly CourierAdapter courierAdapter;
        private readonly string apiUrl;
        private readonly int mainWalletType;
        private readonly MessageBLL messageBLL;
        private readonly PaymentBLL paymentBLL;
        private readonly PromotionBLL promotionBLL;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly CasinoBLL casinoBLL;

        public AccountController(IConfiguration configuration, CustInfoBLL custInfoBLL, Tools tools, SelLangBLL selLangBLL, AuthorizeBLL authorizeBLL, BankInfoBLL bankInfoBLL, WalletBLL walletBLL, CourierAdapter courierAdapter, MessageBLL messageBLL, PaymentBLL paymentBLL, PromotionBLL promotionBLL, ThirdPartyBLL thirdPartyBLL, CasinoBLL casinoBLL)
        {
            this.custInfoBLL = custInfoBLL;
            this.tools = tools;
            this.selLangBLL = selLangBLL;
            this.authorizeBLL = authorizeBLL;
            this.bankInfoBLL = bankInfoBLL;
            this.walletBLL = walletBLL;
            this.courierAdapter = courierAdapter;
            apiUrl = configuration.GetValue<string>("TpService");
            mainWalletType = configuration.GetValue<int>("MainWalletType");
            this.messageBLL = messageBLL;
            this.paymentBLL = paymentBLL;
            this.promotionBLL = promotionBLL;
            this.thirdPartyBLL = thirdPartyBLL;
            this.casinoBLL = casinoBLL;
        }

        [HttpPost]
        public async Task<ApiResult> onlineCheck()
        {
            var apiResult = new ApiResult(selLangBLL);
            var authData = authorizeBLL.GetAuthData();
            bool result = authorizeBLL.LoginCheckin();
            if (!result)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                apiResult.ErrorCode = 1001;
                apiResult.Message = selLangBLL.GetMsg("msg_ForceLogedout");
                _ = Task.Run(async () =>
                {
                    _ = await LogoutAsync(1, authData.CustId);
                });
            }
            else
            {
                apiResult.ErrorCode = 0;
            }

            return apiResult;
        }

        [HttpPost]
        public async Task<ApiResult> SignOut()
        {
            var apiResult = new ApiResult(selLangBLL);
            var authData = authorizeBLL.GetAuthData();
            bool result = authorizeBLL.Logout();

            Response.Cookies.Delete($"InitializationReset{ User.Identity.Name}");
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            apiResult.ErrorCode = 0;
            apiResult.Message = selLangBLL.GetMsg("msg_loggedout");

            _ = Task.Run(async () =>
              {
                  _ = await LogoutAsync(1, authData.CustId);
              });

            return apiResult;
        }

        public class LanguageModel
        {
            public string LangId { set; get; }
        }

        [HttpPost]
        public async Task<ApiResult> SetLanguage(LanguageModel languageModel)
        {
            var apiResult = new ApiResult(selLangBLL);
            var authData = authorizeBLL.GetAuthData();
            int result = custInfoBLL.SetLanguage(authData.CustId, languageModel.LangId);

            if (result == -1)
            {
                apiResult.ErrorCode = result;
                apiResult.Message = "";/*Not Support Language*/
                return apiResult;
            }

            // create a new identity from the old one
            var identity = new ClaimsIdentity(User.Identity);

            // update claim value
            identity.RemoveClaim(identity.FindFirst("LangId"));
            identity.AddClaim(new Claim("LangId", languageModel.LangId));

            // tell the SignInAsync to use this new identity
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

            apiResult.ErrorCode = 0;
            apiResult.Message = Url.RouteUrl("desktop", new { action = HttpContext.Request.RouteValues["action"], controller = HttpContext.Request.RouteValues["controller"], languageModel.LangId });

            return apiResult;
        }

        public class ChangePasswordModel
        {
            public string OldPassword { set; get; }
            public string NewPassword { set; get; }
        }

        [HttpPost]/*會驗證[舊密碼]所以可以放在不嚴格的*/
        public ApiResult ChangePassword(ChangePasswordModel changePasswordModel)
        {
            string oldPassword = changePasswordModel.OldPassword;
            string newPassword = changePasswordModel.NewPassword;

            var apiResult = new ApiResult(selLangBLL);
            var authData = authorizeBLL.GetAuthData();

            if (oldPassword == newPassword)
            {
                apiResult.ErrorCode = -1;
                apiResult.Message = selLangBLL.GetMsg("msg_SamePassword");
            }
            else if (!tools.CheckUserInputPassword(newPassword))
            {
                apiResult.ErrorCode = -2;
                apiResult.Message = selLangBLL.GetMsg("msg_PasswordVaildate");
            }
            else
            {
                apiResult.ErrorCode = custInfoBLL.SetPassword(authData.CustId, oldPassword, newPassword);

                if (apiResult.ErrorCode == -3)
                {
                    apiResult.Message = selLangBLL.GetMsg("msg_InvalidOldPassword");
                }
                else if (apiResult.ErrorCode == -4)
                {
                    apiResult.Message = selLangBLL.GetMsg("msg_UpdateFailed");
                }
                else
                {
                    apiResult.ErrorCode = 0;
                    apiResult.Message = selLangBLL.GetMsg("msg_Updatesuccessful");
                }
            }

            return apiResult;
        }

        public class ProfileModel
        {
            public string Email { set; get; }
        }

        [HttpPost]
        public ApiResult<bool> ExistsEmail(ProfileModel profileModel)
        {
            var apiResult = new ApiResult<bool>(selLangBLL);
            var authData = authorizeBLL.GetAuthData();
            apiResult.Data = custInfoBLL.ExistsEmail(authData.CustId, profileModel.Email);
            apiResult.ErrorCode = 0;
            return apiResult;
        }

        [HttpPost]
        public async Task<ApiResult<decimal>> GetBalanceAsync()
        {
            var apiResult = new ApiResult<decimal>(selLangBLL);
            var authData = authorizeBLL.GetAuthData();


            if (mainWalletType == 1)
            {
                apiResult.Data = walletBLL.GetBalance(authData.CustId);
            }
            else
            {
                var sportsWallet = await thirdPartyBLL.GetSportsBalanceAsync(authData.CustId);
                if (sportsWallet.ErrorCode == 0)
                {
                    apiResult.Data = sportsWallet.Data.balance + sportsWallet.Data.bonus;
                }
            }

            apiResult.ErrorCode = 0;
            return apiResult;
        }
        //[HttpPost]
        //public async Task<ApiResult<decimal>> GetWithdrawalAvailableBalanceAsync()
        //{
        //    var apiResult = new ApiResult<decimal>(selLangBLL);
        //    var authData = authorizeBLL.GetAuthData();
        //    var withdrawalAvailableBalance = await GetWithdrawalAvailableBalanceAsync(authData.CustId);

        //    apiResult.Data = withdrawalAvailableBalance;
        //    var rollover = paymentBLL.ReachedRollover(authData.CustId);
        //    if (!rollover.ReachedRollover)
        //    {
        //        apiResult.Data = 0;
        //    }
        //    apiResult.ErrorCode = 0;
        //    return apiResult;
        //}

        [HttpPost]
        public ApiResult<dynamic> GetWithdrawalAvailableBalanceAsync()
        {
            var apiResult = new ApiResult<dynamic>(selLangBLL);
            var authData = authorizeBLL.GetAuthData();
            var limitInfo = paymentBLL.GetWithdrawalLimitInfo(authData.CustId);
            if (limitInfo != null)
            {
                //var withdrawalAvailableBalance = await GetWithdrawalAvailableBalanceAsync(authData.CustId);
                decimal tmpDeduction = limitInfo.WithdrawalLimit - limitInfo.TurnOver;
                decimal deduction = tmpDeduction < 0 ? 0 : tmpDeduction;
                decimal balance = limitInfo.Balance - (deduction);
                apiResult.Data = new { limitInfo.Balance, Deduction = deduction, AvailableBalance = balance < 0 ? 0 : balance };
                apiResult.ErrorCode = 0;
            }
            else
            {
                apiResult.Data = new { Balance = (decimal)0, Deduction = (decimal)0, AvailableBalance = (decimal)0 };
                apiResult.ErrorCode = 0;
            }

            return apiResult;
        }

        public class RolloverPanel
        {
            public IEnumerable<Promotion> promotions { get; set; }
            public decimal WithdrawalAvailableBalance { get; set; }
        }

        [HttpPost]
        public ApiResult<RolloverPanel> GetRolloverPanelData()
        {
            var apiResult = new ApiResult<RolloverPanel>(selLangBLL);
            var authData = authorizeBLL.GetAuthData();
            RolloverPanel rolloverPanel = new RolloverPanel();
            var promotions = promotionBLL.GetPromotionListByCustIdWithCache(authData.CustId).Where(x => x.BonusInfoStatus == 1);
            if (promotions != null && promotions?.Count() > 0)
            {
                foreach (var promotion in promotions)
                {
                    promotion.Title = selLangBLL.GetResourceValue($"{promotion.BonusCode}Title");
                    promotion.custRollOver = custInfoBLL.GetCustRollOver(authData.CustId, promotion.BonusCode);
                }
                rolloverPanel.promotions = promotions;
                //rolloverPanel.WithdrawalAvailableBalance = await GetWithdrawalAvailableBalanceAsync(authData.CustId);
            }

            apiResult.Data = rolloverPanel;
            apiResult.ErrorCode = 0;
            return apiResult;
        }
        private async Task<decimal> GetWithdrawalAvailableBalanceAsync(int custId)
        {
            var withdrawalAvailableBalance = paymentBLL.GetWithdrawalAvailableBalance(custId);

            if (mainWalletType == 2)
            {
                var sportsWallet = await thirdPartyBLL.GetSportsBalanceAsync(custId);
                if (sportsWallet.ErrorCode == 0)
                {
                    withdrawalAvailableBalance += sportsWallet.Data.balance;
                }
            }

            if (withdrawalAvailableBalance < 0)
            {
                withdrawalAvailableBalance = 0;
            }

            return withdrawalAvailableBalance;
        }
        private class TpInfo
        {
            public int TpId { set; get; }
            public int CustId { set; get; }
            public Dictionary<string, string> inputs { set; get; }
        }
        private async Task<bool> LogoutAsync(int tpId, int custId)
        {
            var myUrl = $"{apiUrl}Logout";
            var oTpInfo = new TpInfo
            {
                TpId = tpId,
                CustId = custId,
                inputs = new Dictionary<string, string>()
            };

            var data = await courierAdapter.MyPostAsync<bool, TpInfo>(myUrl, oTpInfo);

            return data.ErrorCode == 0 ? true : false;
        }

        [HttpPost]
        public ApiResult<CustBankInfoRep> GetCustBankInfo()
        {
            var apiResult = new ApiResult<CustBankInfoRep>(selLangBLL);
            var authData = authorizeBLL.GetAuthData();
            var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);
            var bankInfoSysList = bankInfoBLL.GetBankInfoSysListByCurrency(custInfo.CurrencyId);

            var custBankInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<CustBankInfoRep>(Newtonsoft.Json.JsonConvert.SerializeObject(custInfoBLL.GetCustBankInfo(authData.CustId)));

            if (custBankInfo != null && custBankInfo.bankInfoDetail != null && custBankInfo.bankInfoDetail.Count() > 0)
            {
                foreach (var bankInfo in custBankInfo.bankInfoDetail)
                {
                    var refBankInfo = bankInfoSysList.Where(x => x.SysId == bankInfo.SysId).FirstOrDefault();
                    if (refBankInfo != null)
                    {
                        bankInfo.BankName = refBankInfo.BankName;
                        bankInfo.BankLogo = refBankInfo.BankLogo;
                        bankInfo.BankSite = refBankInfo.BankSite;
                    }
                }
            }

            apiResult.Data = custBankInfo;
            apiResult.ErrorCode = 0;

            return apiResult;
        }

        public class MsgReq
        {
            public int PageNumber { get; set; }
            //public int PageSize { get; set; }
        }

        [HttpPost]
        public ApiListResult<Msg> GetMsgList(MsgReq msgReq)
        {
            var authData = authorizeBLL.GetAuthData();
            var apiResult = new ApiListResult<Msg>();
            int pageNumber = msgReq.PageNumber;
            int totalPages;
            int rowCount;
            apiResult.Data = messageBLL.GetMsgList(authData.CustId, 5, ref pageNumber, out totalPages, out rowCount);
            apiResult.PageNumber = pageNumber;
            apiResult.TotalPages = totalPages;
            apiResult.RowCount = rowCount;
            apiResult.ErrorCode = 0;
            return apiResult;
        }

        public class MsgContentReq
        {
            public int MsgId { get; set; }
            //public int PageSize { get; set; }
        }

        [HttpPost]
        public ApiResult<MsgContent> GetMsgContent(MsgContentReq msgContentReq)
        {
            var authData = authorizeBLL.GetAuthData();
            var apiResult = new ApiResult<MsgContent>();
            apiResult.Data = messageBLL.GetMsgContent(authData.CustId, msgContentReq.MsgId);
            apiResult.ErrorCode = 0;
            return apiResult;
        }

        public class DelMsgReq
        {
            public int[] MsgIds { get; set; }
        }

        [HttpPost]
        public ApiResult DelMsg(DelMsgReq msgReq)
        {
            var authData = authorizeBLL.GetAuthData();
            var apiResult = new ApiResult<MsgContent>();
            if (messageBLL.DelMsg(authData.CustId, msgReq.MsgIds))
            {
                apiResult.ErrorCode = 0;
            }
            else
            {
                apiResult.ErrorCode = 99;
            }

            return apiResult;
        }
        //

        [HttpPost]
        public ApiResult GetMsgCount()
        {
            var authData = authorizeBLL.GetAuthData();
            var apiResult = new ApiResult<int>();
            apiResult.Data = messageBLL.GetMsgCount(authData.CustId);
            apiResult.ErrorCode = 0;

            return apiResult;
        }

        [HttpPost]
        public async Task<ApiResult<WalletInfo>> GetWalletInfoAsync()
        {
            var authData = authorizeBLL.GetAuthData();
            var apiResult = new ApiResult<WalletInfo>();
            apiResult.Data = walletBLL.GetWalletInfo(authData.CustId);
            bool isHideBalance = false;
            bool.TryParse(Request.Cookies[$"isHideBalance.{authData.CustId}"], out isHideBalance);
            apiResult.Data.IsHideBalance = isHideBalance;

            if (mainWalletType == 2)
            {
                TpWallet tpWallet = new TpWallet();
                tpWallet.Balance = apiResult.Data.Balance;
                tpWallet.TpId = 0;
                tpWallet.TpName = selLangBLL.GetResourceValue("lbl_MainWallet");
                tpWallet.displayKey = "lbl_MainWallet";
                var sportsBalance = await thirdPartyBLL.GetSportsBalanceAsync(authData.CustId);
                apiResult.Data.Balance = sportsBalance.Data.balance;
                apiResult.ErrorCode = sportsBalance.ErrorCode;
                apiResult.RawErrorCode = sportsBalance.RawErrorCode;
                apiResult.RawMessage = sportsBalance.RawMessage;
                var tpWallets = new List<TpWallet>();
                tpWallets.Add(tpWallet);
                apiResult.Data.tpWallets = tpWallets;
            }
            else
            {
                apiResult.Data.tpWallets = thirdPartyBLL.GetTpWallet(authData.CustId);
                if (apiResult.Data.tpWallets != null)
                {
                    foreach (var tpWallet in apiResult.Data.tpWallets)
                    {
                        tpWallet.Description = selLangBLL.GetResourceValue($"lbl_WalletDescription_{tpWallet.TpName}");
                        tpWallet.displayKey = $"lbl_Wallet_{tpWallet.TpName}";
                        tpWallet.TpName = selLangBLL.GetResourceValue($"lbl_Wallet_{tpWallet.TpName}");                        
                        if (tpWallet.Transfer)
                        {
                            var tpWalletBalance = await thirdPartyBLL.GetTpWalletBalanceAsync(tpWallet.TpId, authData.CustId);
                            if (tpWalletBalance.ErrorCode == 0)
                            {
                                tpWallet.Balance = tpWalletBalance.Data.balance;
                                tpWallet.Bonus = tpWalletBalance.Data.bonus;
                                tpWallet.walletStatus = WalletStatus.Normal;
                            }
                            else if (tpWalletBalance.ErrorCode == 10)
                            {
                                tpWallet.walletStatus = WalletStatus.UM;
                            }
                            else
                            {
                                tpWallet.walletStatus = WalletStatus.Error;
                            }
                        }
                    }
                }
            }

            apiResult.ErrorCode = 0;
            return apiResult;
        }

        [HttpGet]
        public ApiResult SetHideBalance(bool value)
        {
            var apiResult = new ApiResult();
            var authData = authorizeBLL.GetAuthData();
            CookieOptions option = new CookieOptions();
            option.Expires = DateTime.Now.AddYears(10);
            string key = $"isHideBalance.{authData.CustId}";
            Response.Cookies.Append(key, value.ToString(), option);
            apiResult.ErrorCode = 0;
            return apiResult;
        }

        [HttpPost]
        public ApiResult<CustInfoProfile> GetCustInfoProfile()
        {
            var authData = authorizeBLL.GetAuthData();
            var apiResult = new ApiResult<CustInfoProfile>();
            apiResult.Data = custInfoBLL.GetCustInfoProfile(authData.CustId);
            apiResult.ErrorCode = 0;

            return apiResult;
        }

        public class RrofileReq
        {
            public string AreaCode { get; set; }
            public string PhoneNo { get; set; }
        }

        [HttpPost]
        public ApiResult<bool> ExistsPhoneNo(RrofileReq rrofileReq)
        {
            var apiResult = new ApiResult<bool>(selLangBLL);
            var authData = authorizeBLL.GetAuthData();

            apiResult.Data = custInfoBLL.ExistsPhoneNo(authData.CustId, rrofileReq.AreaCode, rrofileReq.PhoneNo);
            apiResult.ErrorCode = 0;
            return apiResult;
        }
        public class CustRollOverReq
        {
            public string BonusCode { set; get; }
        }

        [HttpPost]
        public ApiResult<CustRollOver> GetCustRollOver(CustRollOverReq custRollOverReq)
        {
            var apiResult = new ApiResult<CustRollOver>(selLangBLL);
            var authData = authorizeBLL.GetAuthData();

            apiResult.Data = custInfoBLL.GetCustRollOver(authData.CustId, custRollOverReq.BonusCode);
            apiResult.ErrorCode = 0;
            return apiResult;
        }

        [HttpPost]
        public ApiResult<bool> CheckMainWellet()
        {
            var apiResult = new ApiResult<bool>(selLangBLL);
            var authData = authorizeBLL.GetAuthData();
            var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);
            var balance = walletBLL.GetBalance(custInfo.CustId);
            if (custInfoBLL.IsConvertRequired(custInfo.CurrencyId))
            {
                balance /= 1000;
            }
            apiResult.Data = balance > 1m;
            apiResult.ErrorCode = 0;
            return apiResult;
        }

        public class CheckWelletReq
        {
            public int TpId { set; get; }
        }

        [HttpPost]
        public async Task<ApiResult<decimal>> CheckWelletAsync(CheckWelletReq checkWelletReq)
        {
            var apiResult = new ApiResult<decimal>(selLangBLL);
            var authData = authorizeBLL.GetAuthData();
            var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);
            if (checkWelletReq.TpId == 1)
            {
                var tpWalletBalanceData = await thirdPartyBLL.GetTpWalletBalanceAsync(checkWelletReq.TpId, authData.CustId);
                if (tpWalletBalanceData.ErrorCode == 0)
                {
                    apiResult.Data = tpWalletBalanceData.Data.balance + tpWalletBalanceData.Data.bonus;
                    apiResult.ErrorCode = 0;
                }
                else if (tpWalletBalanceData.ErrorCode == 10)
                {
                    apiResult.ErrorCode = 10;
                }
                else
                {
                    apiResult.ErrorCode = tpWalletBalanceData.ErrorCode;
                    apiResult.Message = tpWalletBalanceData.Message;
                }
            }
            else
            {
                var balance = walletBLL.GetBalance(custInfo.CustId);
                apiResult.Data = balance;
                apiResult.ErrorCode = 0;
            }

            return apiResult;
        }

        [HttpPost]
        public ApiResult<object> GetFavoriteGame()
        {
            var result = new ApiResult<object>();
            try
            {
                if (User.Identity.IsAuthenticated)
                {
                    var authData = authorizeBLL.GetAuthData();
                    var custInfo = custInfoBLL.GetCustInfoWithCache(authData.CustId);
                    var data = casinoBLL.GetFavoriteGames(custInfo.CustId);
                    result.Data = data != null && data.Any() ? data.Select(x => new { x.ProviderId, x.GameId }) : null;
                    result.ErrorCode = 0;
                }
            }
            catch (Exception)
            {

            }

            return result;
        }

        [HttpPost]
        public void SetFavoriteGame(MemberFavorite FavoriteInfo)
        {
            if (User.Identity.IsAuthenticated)
            {
                var authData = authorizeBLL.GetAuthData();
                casinoBLL.SetFavoriteGames(authData.CustId, FavoriteInfo.ProviderId, FavoriteInfo.GameId, FavoriteInfo.IsRemove);
            }
        }

        //[HttpPost]
        //public ApiResult DelCustBankInfo(BankInfo bankInfo)
        //{
        //    var apiResult = new ApiResult(selLangBLL);
        //    var authData = authorizeBLL.GetAuthData();
        //    IEnumerable<BankInfoDetail> bankInfoList = null;

        //    var custBankInfo = custInfoBLL.GetCustBankInfo(authData.CustId);
        //    if (custBankInfo == null || custBankInfo.BankInfo == null)
        //    {
        //        apiResult.ErrorCode = -2;
        //        return apiResult;
        //    }
        //    else if (custBankInfo != null && custBankInfo.bankInfoDetail != null)
        //    {
        //        bankInfoList = custBankInfo.bankInfoDetail.Where(x => !(x.SysId == bankInfo.SysId && x.AccountNo == bankInfo.AccountNo));
        //    }

        //    apiResult.ErrorCode = custInfoBLL.SetCustBankInfo(authData.CustId, custBankInfo.AccountName, bankInfoList);
        //    return apiResult;
        //}
    }
}