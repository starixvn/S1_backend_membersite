using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MemberSite.Service.BLL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MemberSite.Controllers.Api
{
    [Authorize]
    [Route("api/[controller]/[action]")]
    [Route("api/[controller]/[action]/{LangId}")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly CustInfoBll _custInfoBll;
        private readonly Tools _tools;
        private readonly SelLangBLL _selLangBll;
        private readonly AuthorizeBLL _authorizeBll;
        private readonly BankInfoBLL _bankInfoBll;
        private readonly WalletBLL _walletBll;
        private readonly CourierAdapter _courierAdapter;
        private readonly string _apiUrl;
        private readonly bool _isJwt;
        private readonly int _mainWalletType;
        private readonly MessageBll _messageBll;
        private readonly PaymentBLL _paymentBll;
        private readonly ThirdPartyBll _thirdPartyBll;
        private readonly CasinoBll _casinoBll;
        private readonly BonusWalletBLL _bonusWalletBll;
        private readonly EventsBLL _eventsBll;
        private readonly ServerInfoBll _serverInfoBll;

        public AccountController(IServiceProvider provider)
        {
            _custInfoBll = provider.GetRequiredService<CustInfoBll>();
            _tools = provider.GetRequiredService<Tools>();
            _selLangBll = provider.GetRequiredService<SelLangBLL>();
            _authorizeBll = provider.GetRequiredService<AuthorizeBLL>();
            _bankInfoBll = provider.GetRequiredService<BankInfoBLL>();
            _walletBll = provider.GetRequiredService<WalletBLL>();
            _courierAdapter = provider.GetRequiredService<CourierAdapter>();
            var configuration = provider.GetRequiredService<IConfiguration>();
            _apiUrl = configuration.GetValue<string>("TpService");
            _mainWalletType = configuration.GetValue<int>("MainWalletType");
            _isJwt = configuration.GetValue<bool>("JwtSettings:IsJwt");
            _messageBll = provider.GetRequiredService<MessageBll>();
            _paymentBll = provider.GetRequiredService<PaymentBLL>();
            _thirdPartyBll = provider.GetRequiredService<ThirdPartyBll>();
            _casinoBll = provider.GetRequiredService<CasinoBll>();
            _bonusWalletBll = provider.GetRequiredService<BonusWalletBLL>();
            _eventsBll = provider.GetRequiredService<EventsBLL>();
            _serverInfoBll = provider.GetRequiredService<ServerInfoBll>();
        }

        [HttpPost]
        public async Task<ApiResult> onlineCheck()
        {
            var apiResult = new ApiResult(_selLangBll);
            var authData = _authorizeBll.GetAuthData();
            var fromUrl = _tools.GetFromUrl();
            var deviceType = (int) _tools.GetDeviceId(fromUrl);
            var result = _authorizeBll.LoginCheckin(deviceType);
            if (!result)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                apiResult.ErrorCode = 1001;
                apiResult.Message = _selLangBll.GetMsg("msg_ForceLogedout");
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
        public async Task<ApiResult> onlineCheckApp()
        {
            var apiResult = new ApiResult(_selLangBll);
            var authData = _authorizeBll.GetAuthData();
            var fromUrl = _tools.GetFromUrl();
            var deviceType = (int) _tools.GetDeviceId(fromUrl);
            var result = _authorizeBll.LoginCheckin(deviceType);
            if (!result)
            {
                //await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                apiResult.ErrorCode = 1001;
                apiResult.Message = _selLangBll.GetMsg("msg_ForceLogedout");
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
            var apiResult = new ApiResult(_selLangBll);
            var authData = _authorizeBll.GetAuthData();
            var result = _authorizeBll.Logout();

            Response.Cookies.Delete($"InitializationReset{ User.Identity.Name}");
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            apiResult.ErrorCode = 0;
            apiResult.Message = _selLangBll.GetMsg("msg_loggedout");

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
        public async Task<ApiResult> SetLanguageAsync(LanguageModel languageModel)
        {
            var apiResult = new ApiResult(_selLangBll);
            var authData = _authorizeBll.GetAuthData();
            var result = _custInfoBll.SetLanguage(authData.CustId, languageModel.LangId);

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
            if (!_isJwt)
            {
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
                apiResult.Message = Url.RouteUrl("desktop", new { action = HttpContext.Request.RouteValues["action"], controller = HttpContext.Request.RouteValues["controller"], languageModel.LangId });
            }
            else
            {
                var claims = new[]
                {
                    new Claim(ClaimTypes.Name,authData.UserName ),
                    new Claim(ClaimTypes.NameIdentifier,authData.CustId.ToString() ),
                    new Claim("Token",identity.FindFirst("Token").Value ),
                    new Claim("LangId",languageModel.LangId ),
                    //new Claim("CustId",customer.CustId.ToString() ),
                    //new Claim("SiteId",customer.SiteId.ToString() ),
                    //new Claim("UserName",customer.UserName ),
                    //new Claim(JwtRegisteredClaimNames.Sub, customer.UserName),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString() )
                };
                var accessToken = _tools.GenerateJwtTokens(claims);

                apiResult.RawMessage = accessToken;
            }

            apiResult.ErrorCode = 0;


            return apiResult;
        }
        [HttpPost]
        public ApiResult SignOutApp()
        {
            var apiResult = new ApiResult(_selLangBll);
            var authData = _authorizeBll.GetAuthData();
            var result = _authorizeBll.Logout();

            //Response.Cookies.Delete($"InitializationReset{ User.Identity.Name}");
            //await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            apiResult.ErrorCode = 0;
            apiResult.Message = _selLangBll.GetMsg("msg_loggedout");

            _ = Task.Run(async () =>
            {
                _ = await LogoutAsync(1, authData.CustId);
            });

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
            var oldPassword = changePasswordModel.OldPassword;
            var newPassword = changePasswordModel.NewPassword;

            var apiResult = new ApiResult(_selLangBll);
            var authData = _authorizeBll.GetAuthData();

            if (oldPassword == newPassword)
            {
                apiResult.ErrorCode = -1;
                apiResult.Message = _selLangBll.GetMsg("msg_SamePassword");
            }
            else
            {
                apiResult.ErrorCode = _custInfoBll.SetPassword(authData.CustId, oldPassword, newPassword);

                switch (apiResult.ErrorCode)
                {
                    case -2:
                        apiResult.Message = _selLangBll.GetMsg("msg_PasswordValidate");
                        break;
                    case -3:
                        apiResult.Message = _selLangBll.GetMsg("msg_InvalidOldPassword");
                        break;
                    case -4:
                        apiResult.Message = _selLangBll.GetMsg("msg_UpdateFailed");
                        break;
                    default:
                        apiResult.ErrorCode = 0;
                        apiResult.Message = _selLangBll.GetMsg("msg_Updatesuccessful");
                        break;
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
            var apiResult = new ApiResult<bool>(_selLangBll);
            var authData = _authorizeBll.GetAuthData();
            apiResult.Data = _custInfoBll.ExistsEmail(authData.CustId, profileModel.Email);
            apiResult.ErrorCode = 0;
            return apiResult;
        }

        [HttpPost]
        public async Task<ApiResult<decimal>> GetBalanceAsync()
        {
            var apiResult = new ApiResult<decimal>(_selLangBll);
            var authData = _authorizeBll.GetAuthData();


            if (_mainWalletType == 1)
            {
                apiResult.Data = _walletBll.GetBalance(authData.CustId);
            }
            else
            {
                var sportsWallet = await _thirdPartyBll.GetSportsBalanceAsync(authData.CustId);
                if (sportsWallet.ErrorCode == 0)
                {
                    apiResult.Data = sportsWallet.Data.balance + sportsWallet.Data.bonus;
                }
            }

            apiResult.ErrorCode = 0;
            return apiResult;
        }

        [HttpPost]
        public ApiResult<dynamic> GetWithdrawalAvailableBalanceAsync()
        {
            var apiResult = new ApiResult<dynamic>(_selLangBll);
            var authData = _authorizeBll.GetAuthData();
            var limitInfo = _paymentBll.GetCustWithdrawalLimitInfo(authData.CustId);
            if (limitInfo != null)
            {
                var tmpDeduction = limitInfo.WithdrawalLimit - limitInfo.TurnOver;
                var deduction = tmpDeduction < 0 ? 0 : tmpDeduction;
                var balance = limitInfo.Balance - (deduction);
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

        private class TpInfo
        {
            public int TpId { set; get; }
            public int CustId { set; get; }
            public Dictionary<string, string> inputs { set; get; }
        }
        private async Task<bool> LogoutAsync(int tpId, int custId)
        {
            var myUrl = $"{_apiUrl}Logout";
            var oTpInfo = new TpInfo
            {
                TpId = tpId,
                CustId = custId,
                inputs = new Dictionary<string, string>()
            };

            var data = await _courierAdapter.MyPostAsync<bool, TpInfo>(myUrl, oTpInfo);

            return data.ErrorCode == 0 ? true : false;
        }

        [HttpPost]
        public ApiResult<CustBankInfoRep> GetCustBankInfo()
        {
            var apiResult = new ApiResult<CustBankInfoRep>(_selLangBll);
            var authData = _authorizeBll.GetAuthData();
            var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
            var bankInfoSysList = _bankInfoBll.GetBankInfoSysListByCurrency(custInfo.CurrencyId).ToList();

            if (custInfo.SiteId == (int)SiteId.Nova88
                && custInfo.CurrencyId == (int)CurrencyId.THB) // Nova88 泰國銀行名稱支援多語系
            {
                _bankInfoBll.BankNameLocalization(bankInfoSysList, authData.LangId);
            }

            var custBankInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<CustBankInfoRep>(Newtonsoft.Json.JsonConvert.SerializeObject(_custInfoBll.GetCustBankInfo(authData.CustId)));

            if (custBankInfo != null && custBankInfo.bankInfoDetail != null && custBankInfo.bankInfoDetail.Any())
            {
                foreach (var bankInfo in custBankInfo.bankInfoDetail)
                {
                    var refBankInfo = bankInfoSysList.FirstOrDefault(x => x.SysId == bankInfo.SysId);
                    if (refBankInfo != null)
                    {
                        bankInfo.BankName = refBankInfo.BankName;
                        bankInfo.BankLogo = refBankInfo.BankLogo;
                        bankInfo.BankSite = refBankInfo.BankSite;
                    }

                    if (!custBankInfo.bankInfoDetail.Any())
                    {
                        custBankInfo.DefAccountNo = custBankInfo.bankInfoDetail.ElementAt(0).AccountNo;
                    }
                    else
                    {
                        var defAccountNo = _custInfoBll.GetDefAccountNo(authData.CustId);
                        if (custBankInfo.bankInfoDetail.Any(x => x.AccountNo == defAccountNo))
                        {
                            custBankInfo.DefAccountNo = defAccountNo;
                        }
                    }
                }
            }

            if (custBankInfo != null)
            {
                custBankInfo.bankInfoDetail = custBankInfo.bankInfoDetail ?? Enumerable.Empty<BankInfoDetailRep>();
                custBankInfo.uSDTDetail = custBankInfo.uSDTDetail ?? Enumerable.Empty<USDTDetail>();
            }

            apiResult.Data = custBankInfo;
            apiResult.ErrorCode = 0;

            return apiResult;
        }
        public class GetCustBankInfoReq
        {
            public int CustId { get; set; }
        }
        [HttpPost]
        public ApiResult<CustBankInfoRep> GetCustBankInfoInput(GetCustBankInfoReq getCustBankInfoReq)
        {
            var apiResult = new ApiResult<CustBankInfoRep>(_selLangBll);
            //var authData = authorizeBLL.GetAuthData();

            var custInfo = _custInfoBll.GetCustInfoWithCache(getCustBankInfoReq.CustId);
            var bankInfoSysList = _bankInfoBll.GetBankInfoSysListByCurrency(custInfo.CurrencyId);

            var custBankInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<CustBankInfoRep>(Newtonsoft.Json.JsonConvert.SerializeObject(_custInfoBll.GetCustBankInfo(getCustBankInfoReq.CustId)));

            if (custBankInfo != null && custBankInfo.bankInfoDetail != null && custBankInfo.bankInfoDetail.Any())
            {
                foreach (var bankInfo in custBankInfo.bankInfoDetail)
                {
                    var refBankInfo = bankInfoSysList.FirstOrDefault(x => x.SysId == bankInfo.SysId);
                    if (refBankInfo != null)
                    {
                        bankInfo.BankName = refBankInfo.BankName;
                        bankInfo.BankLogo = refBankInfo.BankLogo;
                        bankInfo.BankSite = refBankInfo.BankSite;
                    }

                    if (!custBankInfo.bankInfoDetail.Any())
                    {
                        custBankInfo.DefAccountNo = custBankInfo.bankInfoDetail.ElementAt(0).AccountNo;
                    }
                    else
                    {
                        var defAccountNo = _custInfoBll.GetDefAccountNo(getCustBankInfoReq.CustId);
                        if (custBankInfo.bankInfoDetail.Any(x => x.AccountNo == defAccountNo))
                        {
                            custBankInfo.DefAccountNo = defAccountNo;
                        }
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
            public int PageSize { get; set; }
        }

        [HttpPost]
        public ApiListResult<Msg> GetMsgList(MsgReq msgReq)
        {
            var authData = _authorizeBll.GetAuthData();
            var apiResult = new ApiListResult<Msg>();
            var pageNumber = msgReq.PageNumber;
            var pageSize = msgReq.PageSize == 0 ? 5 : msgReq.PageSize;
            apiResult.Data = _messageBll.GetMsgList(authData.CustId, pageSize, ref pageNumber, out var totalPages, out var rowCount);
            apiResult.PageNumber = pageNumber;
            apiResult.TotalPages = totalPages;
            apiResult.RowCount = rowCount;
            apiResult.ErrorCode = 0;
            return apiResult;
        }

        public class MsgContentReq
        {
            public int MsgId { get; set; }
            public int IsBatch { get; set; }
            //public int PageSize { get; set; }
        }

        [HttpPost]
        public ApiResult<MsgContent> GetMsgContent(MsgContentReq msgContentReq)
        {
            var authData = _authorizeBll.GetAuthData();
            var apiResult = new ApiResult<MsgContent>
            {
                Data = _messageBll.GetMsgContent(authData.CustId, msgContentReq.MsgId, msgContentReq.IsBatch),
                ErrorCode = 0
            };
            return apiResult;
        }
        public class MsgReadReq
        {
            public int[] MsgIds { get; set; }
        }

        [HttpPost]
        public ApiResult SetMsgRead(MsgReadReq msgReadReq)
        {
            var authData = _authorizeBll.GetAuthData();
            var apiResult = new ApiResult<MsgContent>();
            if (_messageBll.SetMsgRead(authData.CustId, msgReadReq.MsgIds))
            {
                apiResult.ErrorCode = 0;
            }
            else
            {
                apiResult.ErrorCode = 99;
            }

            return apiResult;
        }
        
        [HttpPost]
        public async Task<ApiResult<PopupMsg>> GetPopupMsg()
        {
            var authData = _authorizeBll.GetAuthData();
            var apiResult = new ApiResult<PopupMsg>
            {
                Data = await _messageBll.GetPopupMessage(authData.CustId),
                ErrorCode = 0
            };
            return apiResult;
        }
        
        [HttpPost]
        public async Task<ApiResult> SetPopupMsgRead()
        {
            var authData = _authorizeBll.GetAuthData();
            var apiResult = new ApiResult()
            {
                ErrorCode = await _messageBll.SetPopupMsgRead(authData.CustId) ? 0 : 99
            };
            return apiResult;
        }
        
        public class DelMsgReq
        {
            public int[] MsgIds { get; set; }
        }

        [HttpPost]
        public ApiResult DelMsg(DelMsgReq msgReq)
        {
            var authData = _authorizeBll.GetAuthData();
            var apiResult = new ApiResult<MsgContent>();
            if (_messageBll.DelMsg(authData.CustId, msgReq.MsgIds))
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
            var authData = _authorizeBll.GetAuthData();
            var apiResult = new ApiResult<int>
            {
                Data = _messageBll.GetMsgCount(authData.CustId),
                ErrorCode = 0
            };

            return apiResult;
        }

        [HttpPost]
        public async Task<ApiResult<WalletInfo>> GetWalletInfoAsync()
        {
            var authData = _authorizeBll.GetAuthData();
            var apiResult = new ApiResult<WalletInfo>
            {
                Data = _walletBll.GetWalletInfo(authData.CustId)
            };
            var isHideBalance = false;
            bool.TryParse(Request.Cookies[$"isHideBalance.{authData.CustId}"], out isHideBalance);
            apiResult.Data.IsHideBalance = isHideBalance;

            if (_mainWalletType == 2)
            {
                var tpWallet = new TpWallet
                {
                    Balance = apiResult.Data.Balance,
                    TpId = 0,
                    TpName = _selLangBll.GetResourceValue("lbl_MainWallet"),
                    displayKey = "lbl_MainWallet"
                };
                var sportsBalance = await _thirdPartyBll.GetSportsBalanceAsync(authData.CustId);
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
                apiResult.Data.tpWallets = _thirdPartyBll.GetTpWallet(authData.CustId);
                if (apiResult.Data.tpWallets != null)
                {
                    foreach (var tpWallet in apiResult.Data.tpWallets)
                    {
                        tpWallet.Description = _selLangBll.GetResourceValue($"lbl_WalletDescription_{tpWallet.TpName}");
                        tpWallet.displayKey = $"lbl_Wallet_{tpWallet.TpName}";
                        tpWallet.TpName = _selLangBll.GetResourceValue($"lbl_Wallet_{tpWallet.TpName}");
                        if (tpWallet.Transfer)
                        {
                            var tpWalletBalance = await _thirdPartyBll.GetTpWalletBalanceAsync(tpWallet.TpId, authData.CustId);
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

            apiResult.Data.bonusWallets = _bonusWalletBll.GetCustBonusWallet(authData.CustId);
            apiResult.ErrorCode = 0;
            return apiResult;
        }

        [HttpGet]
        public ApiResult SetHideBalance(bool value)
        {
            var apiResult = new ApiResult();
            var authData = _authorizeBll.GetAuthData();
            var option = new CookieOptions
            {
                Expires = DateTime.Now.AddYears(10)
            };
            var key = $"isHideBalance.{authData.CustId}";
            Response.Cookies.Append(key, value.ToString(), option);
            apiResult.ErrorCode = 0;
            return apiResult;
        }

        [HttpPost]
        public ApiResult<CustInfoProfile> GetCustInfoProfile()
        {
            var authData = _authorizeBll.GetAuthData();
            var apiResult = new ApiResult<CustInfoProfile>
            {
                Data = _custInfoBll.GetCustInfoProfile(authData.CustId),
                ErrorCode = 0
            };

            return apiResult;
        }
        [HttpPost]
        public ApiResult<string> GetReferralCode()
        {
            var apiResult = new ApiResult<string>();
            var authData = _authorizeBll.GetAuthData();
            try
            {
                apiResult.Data = _custInfoBll.GetAffiliateId(authData.CustId);
                apiResult.ErrorCode = 0;
                apiResult.Message = "";
            }
            catch (Exception ex)
            {
                apiResult.Message = ex.ToString();
            }
            return apiResult;
        }
        [HttpPost]
        public ApiResult<IEnumerable<ReferralSetting>> GetReferralSetting()
        {
            var apiResult = new ApiResult<IEnumerable<ReferralSetting>>();
            var authData = _authorizeBll.GetAuthData();
            var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
            try
            {
                apiResult.Data = _custInfoBll.GetReferralSetting(custInfo.SiteId, custInfo.CurrencyId);
                apiResult.ErrorCode = 0;
                apiResult.Message = "";
            }
            catch (Exception ex)
            {
                apiResult.Message = ex.ToString();
            }
            return apiResult;
        }
        
        [HttpPost]
        public async Task<ApiResult<ReferralSettingDetail>> GetReferralSettingDetail()
        {
            var apiResult = new ApiResult<ReferralSettingDetail>();
            var authData = _authorizeBll.GetAuthData();
            var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
            try
            {
                apiResult.Data = await _custInfoBll.GetReferralSettingDetailAsync(custInfo.SiteId, custInfo.CurrencyId, custInfo.UserLang);
                apiResult.ErrorCode = 0;
            }
            catch (Exception ex)
            {
                apiResult.Message = ex.ToString();
            }
            return apiResult;
        }
        
        [HttpPost]
        public ApiResult<ReferralSummary> GetReferralSummary()
        {
            var apiResult = new ApiResult<ReferralSummary>();
            var authData = _authorizeBll.GetAuthData();
            apiResult.Data = new ReferralSummary();
            try
            {
                _custInfoBll.GetReferralSummary(authData.CustId, out var ReferralCount, out var conditionCount, out var AmountCount, out var AmountSum);

                apiResult.Data.ReferralCount = ReferralCount;
                apiResult.Data.ConditionCount = conditionCount;
                apiResult.Data.AmountCount = AmountCount;
                apiResult.Data.AmountSum = AmountSum;
                apiResult.ErrorCode = 0;
                apiResult.Message = "";
            }
            catch (Exception ex)
            {
                apiResult.Message = ex.ToString();
            }
            return apiResult;
        }
        public class RefFriendListReq
        {
            public long? StartDate { get; set; }
            public long? EndDate { get; set; }
            public int PageNumber { get; set; }
            public int PageSize { get; set; }
        }

        [HttpPost]
        public ApiListResult<ReferralFriend> GetRefFriendList(RefFriendListReq refFriendListReq)
        {
            var PageSize = 10;
            if (refFriendListReq.PageSize > 0)
                PageSize = refFriendListReq.PageSize;
            var pageNumber = refFriendListReq.PageNumber;
            var totalPages = 0;
            var rowCount = 0;
            var apiResult = new ApiListResult<ReferralFriend>();
            var authData = _authorizeBll.GetAuthData();

            try
            {
                apiResult.Data = _custInfoBll.GetRefFriendList(authData.CustId, refFriendListReq.StartDate, refFriendListReq.EndDate, PageSize, ref pageNumber, ref totalPages, ref rowCount);
                foreach (var L in apiResult.Data)
                {
                    L.UserName = _eventsBll.MaskName(L.UserName);
                }
                apiResult.PageNumber = pageNumber;
                apiResult.TotalPages = totalPages;
                apiResult.RowCount = rowCount;
                apiResult.ErrorCode = 0;
                apiResult.Message = "";
            }
            catch (Exception ex)
            {
                apiResult.Message = ex.ToString();
            }
            return apiResult;
        }

        [HttpPost]
        public ApiResult<bool> ExistsPhoneNo(RrofileReq rrofileReq)
        {
            var apiResult = new ApiResult<bool>(_selLangBll);
            var authData = _authorizeBll.GetAuthData();

            if (long.TryParse(rrofileReq.PhoneNo, out var phoneNo))
            {
                rrofileReq.PhoneNo = phoneNo.ToString();
                apiResult.Data = _custInfoBll.ExistsPhoneNo(authData.CustId, rrofileReq.AreaCode, rrofileReq.PhoneNo);
            }
            else
            {
                apiResult.Data = true;
            }

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
            var apiResult = new ApiResult<CustRollOver>(_selLangBll);
            var authData = _authorizeBll.GetAuthData();

            apiResult.Data = _custInfoBll.GetCustRollOver(authData.CustId, custRollOverReq.BonusCode);
            apiResult.ErrorCode = 0;
            return apiResult;
        }

        [HttpPost]
        public ApiResult<bool> CheckMainWellet()
        {
            var apiResult = new ApiResult<bool>(_selLangBll);
            var authData = _authorizeBll.GetAuthData();
            var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
            var balance = _walletBll.GetBalance(custInfo.CustId);
            if (_custInfoBll.IsConvertRequired(custInfo.CurrencyId))
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
            var apiResult = new ApiResult<decimal>(_selLangBll);
            var authData = _authorizeBll.GetAuthData();
            var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
            if (checkWelletReq.TpId == 1)
            {
                var tpWalletBalanceData = await _thirdPartyBll.GetTpWalletBalanceAsync(checkWelletReq.TpId, authData.CustId);
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
                var balance = _walletBll.GetBalance(custInfo.CustId);
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
                    var authData = _authorizeBll.GetAuthData();
                    var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
                    var data = _casinoBll.GetFavoriteGames(custInfo.CustId);
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
        public ApiResult SetFavoriteGame(MemberFavorite FavoriteInfo)
        {
            var result = new ApiResult();
            try
            {
                if (User.Identity.IsAuthenticated)
                {
                    var authData = _authorizeBll.GetAuthData();
                    _casinoBll.SetFavoriteGames(authData.CustId, FavoriteInfo.ProviderId, FavoriteInfo.GameId, FavoriteInfo.IsRemove);
                    result.ErrorCode = 0;
                }
                else
                {
                    result.ErrorCode = 1103;
                    result.Message = _selLangBll.GetErrMsg(result.ErrorCode);
                }
            }
            catch (Exception ex)
            {
                result.ErrorCode = 9000;
                result.RawMessage = ex.Message;
                result.Message = _selLangBll.GetErrMsg(result.ErrorCode);
            }

            return result;
        }

        [HttpPost]
        public ApiResult<IEnumerable<BonusWallet>> GetCustBonusWallet()
        {
            var result = new ApiResult<IEnumerable<BonusWallet>>();
            try
            {
                var authData = _authorizeBll.GetAuthData();
                result.Data = _bonusWalletBll.GetCustBonusWallet(authData.CustId);
                result.ErrorCode = 0;
            }
            catch (Exception ex)
            {
                result.ErrorCode = 999;
                result.RawMessage = ex.Message;
            }

            return result;
        }

        [HttpPost]
        public ApiResult<BonusWallet> GetCustSpecificBonus(ParamGetSpecificBonus data)
        {
            var apiResult = new ApiResult<BonusWallet>();
            var authData = _authorizeBll.GetAuthData();
            apiResult.Data = _bonusWalletBll.GetCustSpecificBonus(authData.CustId, data.BonusCode, data.TpId, data.PromoCategoryId);
            apiResult.ErrorCode = 0;
            return apiResult;
        }

        public class ParamGetSpecificBonus
        {
            public int TpId { get; set; }
            public int PromoCategoryId { get; set; }
            public string BonusCode { get; set; }
        }

        public class TpWalletListReq
        {
            public int ListType { get; set; }
        }
        [HttpPost]
        public ApiResult<IEnumerable<TpWalletList>> GetTpWalletList(TpWalletListReq tpwalletListReq)
        {
            var authData = _authorizeBll.GetAuthData();
            var apiResult = new ApiResult<IEnumerable<TpWalletList>>
            {
                Data = _thirdPartyBll.GetTpWalletList(authData.CustId, tpwalletListReq.ListType)
            };
            foreach (var tpWalletItem in apiResult.Data)
            {
                tpWalletItem.Description = _selLangBll.GetResourceValue($"lbl_WalletDescription_{tpWalletItem.TpName}");
                tpWalletItem.displayKey = $"lbl_Wallet_{tpWalletItem.TpName}";
                tpWalletItem.TpName = _selLangBll.GetResourceValue($"lbl_Wallet_{tpWalletItem.TpName}");
            }
            apiResult.ErrorCode = 0;
            return apiResult;
        }
        public class TpWalletBalanceReq
        {
            public int TpId { get; set; }
        }

        [HttpPost]
        public async Task<ApiResult<TpWalletData>> GetTpWalletBalanceAsync(TpWalletBalanceReq tpWalletBalanceReq)
        {
            var authData = _authorizeBll.GetAuthData();
            var apiResult = new ApiResult<TpWalletData>
            {
                Data = new TpWalletData
                {
                    TpId = tpWalletBalanceReq.TpId
                }
            };
            var tpWalletBalance = await _thirdPartyBll.GetTpWalletBalanceAsync(tpWalletBalanceReq.TpId, authData.CustId);
            if (tpWalletBalance.ErrorCode == 0)
            {
                apiResult.Data.Balance = tpWalletBalance.Data.balance;
                apiResult.Data.Bonus = tpWalletBalance.Data.bonus;
                apiResult.Data.walletStatus = WalletStatus.Normal;
            }
            else if (tpWalletBalance.ErrorCode == 10)
            {
                apiResult.Data.walletStatus = WalletStatus.UM;
            }
            else
            {
                apiResult.Data.walletStatus = WalletStatus.Error;
            }
            apiResult.ErrorCode = 0;
            return apiResult;
        }

        [HttpPost]
        public async Task<ApiResult<WalletBalance>> GetWalletBalanceAsync()
        {
            var apiResult = new ApiResult<WalletBalance>(_selLangBll)
            {
                Data = new WalletBalance()
            };
            var authData = _authorizeBll.GetAuthData();
            var isHideBalance = false;
            bool.TryParse(Request.Cookies[$"isHideBalance.{authData.CustId}"], out isHideBalance);
            apiResult.Data.IsHideBalance = isHideBalance;
            var apiWalletInfo = _walletBll.GetWalletInfo(authData.CustId);
            apiResult.Data.Balance = apiWalletInfo.Balance;
            apiResult.Data.LastTransaction = apiWalletInfo.LastTransaction;
            apiResult.Data.LastLoginTime = apiWalletInfo.LastLoginTime;

            if (_mainWalletType == 2)
            {
                var sportsWallet = await _thirdPartyBll.GetSportsBalanceAsync(authData.CustId);
                if (sportsWallet.ErrorCode == 0)
                {
                    apiResult.Data.Balance = sportsWallet.Data.balance + sportsWallet.Data.bonus;
                }
            }
            apiResult.ErrorCode = 0;
            return apiResult;
        }

        [HttpPost]
        public ApiResult<int> GetWalletTransferStatus()
        {
            var authData = _authorizeBll.GetAuthData();
            var apiResult = new ApiResult<int>();
            var autoTransfer = 0;
            if (_walletBll.GetWalletTransferStatus(authData.CustId, out autoTransfer) == 1)
            {
                apiResult.ErrorCode = 0;
                apiResult.Data = autoTransfer;
            }
            else
            {
                apiResult.ErrorCode = 99;
            }
            return apiResult;
        }

        public class SetWalletTransferStatusReq
        {
            public int AutoTransfer { get; set; }
        }
        [HttpPost]
        public ApiResult<int> SetWalletTransferStatus(SetWalletTransferStatusReq setWalletTransferStatusReq)
        {
            var authData = _authorizeBll.GetAuthData();
            var apiResult = new ApiResult<int>();
            var autoTransfer = 0;
            if (_walletBll.SetWalletTransferStatus(authData.CustId, setWalletTransferStatusReq.AutoTransfer) == 1)
            {
                apiResult.ErrorCode = 0;
            }
            else
            {
                apiResult.ErrorCode = 99;
            }
            return apiResult;
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
        public class SetFireBaseTrokenReq
        {
            public string FireBaseTroken { get; set; }
        }

        [HttpPost]
        public ApiResult SetFireBaseTroken(SetFireBaseTrokenReq setFireBaseTrokenReq)
        {
            var apiResult = new ApiResult
            {
                ErrorCode = 99
            };
            var authData = _authorizeBll.GetAuthData();
            var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);

            if (_custInfoBll.SetFireBaseTroken(authData.CustId, custInfo.SiteId, setFireBaseTrokenReq.FireBaseTroken) > 0)
            {
                apiResult.ErrorCode = 0;
            }

            return apiResult;
        }
        public class SetCustActStatusReq
        {
            public int CustId { get; set; }
            public string Operator { get; set; }
        }
        [AllowAnonymous]
        [HttpPost]
        public ApiResult<bool> SetCustActStatus(SetCustActStatusReq setCustActStatusReq)
        {
            var apiResult = new ApiResult<bool>(_selLangBll);
            int setResult = _custInfoBll.SetCustActStatus(setCustActStatusReq.CustId, setCustActStatusReq.Operator);
            if (setResult > 0)
            {
                apiResult.Data = true;
                apiResult.ErrorCode = 0;
            }
            else
            {
                apiResult.Data = false;
                apiResult.ErrorCode = 99;
            }
            return apiResult;
        }
    }
}