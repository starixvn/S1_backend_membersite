using System.Web;
using MemberSite.Service.BLL;
using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using static MemberSite.Extensions.MethodExtension;

namespace MemberSite.Controllers.Api
{
    [Authorize]
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class TpController : ControllerBase
    {
        private readonly CourierAdapter _courierAdapter;
        private readonly Tools _tools;
        private readonly SelLangBLL _selLangBll;
        private readonly AuthorizeBLL _authorizeBll;
        private readonly string _apiUrl;
        private readonly ThirdPartyBll _thirdPartyBll;
        private readonly WalletBLL _walletBll;
        private readonly BonusWalletBLL _bonusWalletBll;
        private readonly CustInfoBll _custInfoBll;
        private readonly GameBLL _gameBll;
        private readonly ServerInfoBll _serverInfoBll;
        private readonly StatementService _statementService;
        private readonly IOptions<Dictionary<string, SiteConfig>> _siteConfig;
        private readonly ILogger<TpController> _logger;
        
        public TpController(IServiceProvider provider)
        {
            _courierAdapter = provider.GetRequiredService<CourierAdapter>();
            _tools = provider.GetRequiredService<Tools>();
            _selLangBll = provider.GetRequiredService<SelLangBLL>();
            _authorizeBll = provider.GetRequiredService<AuthorizeBLL>();
            _apiUrl = provider.GetRequiredService<IConfiguration>().GetValue<string>("TpService");
            _thirdPartyBll = provider.GetRequiredService<ThirdPartyBll>();
            _walletBll = provider.GetRequiredService<WalletBLL>();
            _bonusWalletBll = provider.GetRequiredService<BonusWalletBLL>();
            _custInfoBll = provider.GetRequiredService<CustInfoBll>();
            _gameBll = provider.GetRequiredService<GameBLL>();
            _serverInfoBll = provider.GetRequiredService<ServerInfoBll>();
            _statementService = provider.GetRequiredService<StatementService>();
            _siteConfig = provider.GetRequiredService<IOptions<Dictionary<string, SiteConfig>>>();
            _logger = provider.GetRequiredService<ILogger<TpController>>();
        }

        public class TpInfo
        {
            public int TpId { set; get; }
            public int AppType { set; get; }
            public Dictionary<string, string> inputs { set; get; }
        }
        [HttpPost]
        [AllowAnonymous]
        public async Task<ApiResult> GotoGame(TpInfo inInfo)
        {
            var result = new ApiResult<bool>();
            try
            {
                var serverInfo = _serverInfoBll.GetServerInfo();
                var tpMaintainList = _thirdPartyBll.GetTpMaintainListWithCache(serverInfo.SiteId);
                if (_gameBll.IsMaintain(tpMaintainList, inInfo.TpId, inInfo.inputs.GetValueOrDefault("tpGameType"),
                        out var tpMaintain))
                {
                    var tpName = _statementService.GetTpListWithCache()?.First(i => i.Id == tpMaintain.TpId).TpName;
                    var host = _siteConfig.Value[serverInfo.SiteName].MaintainURL;
                    var maintainUrl = $"{host}/index_{_selLangBll.GetLangId() ?? "en"}.html" +
                                      $"?t={HttpUtility.UrlEncode(tpName)}&e={HttpUtility.UrlEncode($"{tpMaintain.EndDate:u}")}";
                    return new ApiResult(3000, maintainUrl);
                }

                var input = new Dictionary<string, string>();
                var custId = 0;
                var langId = "";
                if (inInfo.inputs.ContainsKey("AppType") && int.TryParse(inInfo.inputs["AppType"], out var appType))
                {
                    inInfo.AppType = appType;
                }

                if (inInfo.inputs != null)
                {
                    input = inInfo.inputs;
                }

                input.Add("Host", $"{Request.Scheme}://{Request.Host}{Request.PathBase}");
                input.Add("ClientIP", _tools.GetClientIP());
                var authData = _authorizeBll.GetAuthData();

                if (input.TryGetValue("fun", out var fun) && fun.Equals("1"))
                {
                    langId = _selLangBll.GetLangId();
                }
                else if (authData is { CustId: > 0 })
                {
                    var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
                    langId = custInfo.UserLang;
                    custId = authData.CustId;
                    if (inInfo.TpId == (int)Tp.AG)
                    {
                        var tpCurrency = _thirdPartyBll.GetTpCurrency(inInfo.TpId, custInfo.CurrencyId);
                        var balance = _walletBll.GetBalance(authData.CustId);
                        var bonusAmount = _walletBll.GetBonus(authData.CustId, 2);

                        balance += bonusAmount;

                        if (tpCurrency.ExchangeRate != 0 && tpCurrency.ExchangeRate != 1)
                        {
                            balance /= tpCurrency.ExchangeRate;
                        }

                        input.Add("Balance", _tools.Floor(balance, 2).ToString());
                        input.Add("Token", authData.Token);
                    }
                    else
                    {
                        input.Add("Token", authData.Token);
                    }
                }
                else if (input.TryGetValue("token", out var jwtToken)
                         && _authorizeBll.TryValidateJwtToken(jwtToken, out var appAuthData))
                {
                    //將inputs參數的token換成jwtToken避免與原本的Token混淆
                    input.Remove("token");
                    input.Add("jwtToken", jwtToken);

                    langId = appAuthData.LangId;
                    custId = appAuthData.CustId;
                    input.Add("Token", appAuthData.Token);
                }

                var myUrl = $"{_apiUrl}GotoGame";
                var tpInfo = new
                {
                    inInfo.TpId,
                    CustId = custId,
                    LangId = langId,
                    inInfo.AppType,
                    inputs = input
                };

                result = await _courierAdapter.MyPostAsync<bool, dynamic>(myUrl, tpInfo);
                result.SetSelLang(_selLangBll);
            }
            catch (Exception e)
            {
                _logger.LogError(e, GetCurrentAsyncMethod());
            }

            return result;
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
            public decimal Bonus { get; set; }
            public decimal Rollover { get; set; }
            public decimal Turnover { get; set; }
            public int BonusStatus { get; set; }
        }
        public enum TransferStatus
        {
            Success = 0, Fail = 1, Pending = 2, PendingD = 3
        }

        public class TpTransfer
        {
            public int TpId { set; get; }
            public int CustId { set; get; }
            public string Description { set; get; }
            public decimal Amount { set; get; }
            public TpDirection direction { set; get; }
            public Dictionary<string, string> inputs { set; get; }
        }

        public enum TpDirection
        {
            ToTp = 1,
            ToMain = 2
        }

        public class TpTransferReq
        {
            public int TpId { set; get; }
            public decimal Amount { set; get; }
            public TpDirection direction { set; get; }
        }

        [HttpPost]
        public async Task<ApiResult<TransferResult>> Transfer([FromBody] TpTransferReq inTpTransfer)
        {
            var result = new ApiResult<TransferResult>();
            var authData = _authorizeBll.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                result.Message = _selLangBll.GetMsg("msg_ForceLogedout");
                return result;
            }
            
            var bonusWalletInfo = inTpTransfer.direction == TpDirection.ToMain ? _bonusWalletBll.CheckBonusWallet(authData.CustId, inTpTransfer.TpId) : null;
            if (bonusWalletInfo != null)
            {
                result.ErrorCode = 9;
                result.Data = new TransferResult()
                {
                    Bonus = bonusWalletInfo.Bonus,
                    Turnover = bonusWalletInfo.Turnover,
                    Rollover = bonusWalletInfo.Rollover,
                    BonusStatus = bonusWalletInfo.Status
                };

                return result;
            }
            else
            {
                var amount = inTpTransfer.Amount;
                if (amount.ToString().IndexOf(".") > -1)
                {
                    decimal.TryParse(amount.ToString("#.##"), out amount);
                }
                var input = new Dictionary<string, string>
                {
                    { "Host", $"{Request.Scheme}://{Request.Host}{Request.PathBase}" },
                    { "ClientIP", _tools.GetClientIP() }
                };

                var tpTransfer = new TpTransfer
                {
                    TpId = inTpTransfer.TpId,
                    CustId = authData.CustId,
                    Amount = amount,
                    direction = inTpTransfer.direction,
                    inputs = input
                };
                var myUrl = $"{_apiUrl}Transfer";
                result = await _courierAdapter.MyPostAsync<TransferResult, TpTransfer>(myUrl, tpTransfer);
                if (result.ErrorCode != 0 && string.IsNullOrEmpty(result.Message) && !string.IsNullOrEmpty(result.RawMessage))
                {
                    result.Message = result.RawErrorCode == "3" ? _selLangBll.GetMsg("msg_InsufficientFund") : result.RawMessage;
                }
                else if (result.ErrorCode == 1201)
                {
                    result.Message = _selLangBll.GetMsg("msg_InsufficientFund");
                }

                result.SetSelLang(_selLangBll);
                return result;
            }
        }

        public class TransferEXReq
        {
            public int FromTpId { set; get; }
            public int ToTpId { set; get; }
            public decimal Amount { set; get; }
            public int TransferAction { set; get; }
        }

        [HttpPost]
        public async Task<ApiResult<TransferResult>> TransferEX(TransferEXReq inTpTransfer)
        {
            var result = new ApiResult<TransferResult>();
            var authData = _authorizeBll.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                result.Message = _selLangBll.GetMsg("msg_ForceLogedout");
                return result;
            }
            
            var tpWalletList = _thirdPartyBll.GetTpWalletList(authData.CustId, 0);//0:全部列表
            TpDirection direction;
            int activeTpId;
            var fromWallet = tpWalletList.FirstOrDefault(x => x.TpId == inTpTransfer.FromTpId);
            var toWallet = tpWalletList.FirstOrDefault(x => x.TpId == inTpTransfer.ToTpId);
            var fromWalletMode = fromWallet?.WalletMode ?? 0;
            var toWalletMode = toWallet?.WalletMode ?? 0;

            if (inTpTransfer.TransferAction != (int)TransferAction.Manual && ((fromWalletMode == (int)WalletMode.Manual) || toWalletMode == (int)WalletMode.Manual)) //restore all not support  Wallet which WalletMode = Manual
            {
                if (inTpTransfer.ToTpId != 0) //main wallet can restore all
                {
                    result.ErrorCode = 1208;
                    result.Message = $"Not Support";
                    if (inTpTransfer.TransferAction == (int)TransferAction.RestoreAll) //當TransferAction.RestoreAll不執行動作但是不噴錯
                    {
                        result.ErrorCode = 0;
                        result.Message = $"Transfer Skip";
                    }
                    return result;
                }
            }
            if (inTpTransfer.FromTpId != 0)
            {
                direction = TpDirection.ToMain;
                activeTpId = inTpTransfer.FromTpId;
            }
            else if (inTpTransfer.ToTpId != 0)
            {
                direction = TpDirection.ToTp;
                activeTpId = inTpTransfer.ToTpId;
            }
            else
            {
                result.ErrorCode = 99;
                return result;

            }

            var bonusWalletInfo = direction == TpDirection.ToMain ? _bonusWalletBll.CheckBonusWallet(authData.CustId, activeTpId) : null;

            if (bonusWalletInfo != null)
            {
                result.ErrorCode = 9;
                result.Data = new TransferResult()
                {
                    Bonus = bonusWalletInfo.Bonus,
                    Turnover = bonusWalletInfo.Turnover,
                    Rollover = bonusWalletInfo.Rollover,
                    BonusStatus = bonusWalletInfo.Status
                };

                return result;
            }
            else
            {
                var amount = inTpTransfer.Amount;
                if (amount.ToString().IndexOf(".") > -1)
                {
                    decimal.TryParse(amount.ToString("#.##"), out amount);
                }
                var input = new Dictionary<string, string>
                {
                    { "Host", $"{Request.Scheme}://{Request.Host}{Request.PathBase}" },
                    { "ClientIP", _tools.GetClientIP() }
                };

                var tpTransfer = new TpTransfer
                {
                    TpId = activeTpId,
                    CustId = authData.CustId,
                    Amount = amount,
                    direction = direction,
                    inputs = input
                };
                var myUrl = $"{_apiUrl}Transfer";
                result = await _courierAdapter.MyPostAsync<TransferResult, TpTransfer>(myUrl, tpTransfer);
                if (result.ErrorCode != 0 && string.IsNullOrEmpty(result.Message) && !string.IsNullOrEmpty(result.RawMessage))
                {
                    result.Message = result.RawErrorCode switch
                    {
                        "3" => _selLangBll.GetMsg("msg_InsufficientFund"),
                        "1207" => _selLangBll.GetMsg("msg_DecimalOutOfRange"),
                        _ => result.RawMessage
                    };
                }
                else if (result.ErrorCode == 1201)
                {
                    result.Message = _selLangBll.GetMsg("msg_InsufficientFund");
                }
                else if (result.ErrorCode == 0 && direction == TpDirection.ToMain && inTpTransfer.ToTpId != 0)
                {
                    //Do Main to TP
                    var transferEXReq = new TransferEXReq()
                    {
                        FromTpId = 0,
                        ToTpId = inTpTransfer.ToTpId,
                        Amount = inTpTransfer.Amount,
                        TransferAction = inTpTransfer.TransferAction
                    };

                    result = await TransferEX(transferEXReq);
                }

                result.SetSelLang(_selLangBll);
                return result;
            }
        }
        
        public class HistoryReq
        {
            public long StartDate { get; set; }
            public long EndDate { get; set; }
            public int PageNumber { get; set; }
        }
        [HttpPost]
        public ApiListResult<TpTransferInfoHistory> GetTransferHistory(HistoryReq historyReq)
        {
            var apiResult = new ApiListResult<TpTransferInfoHistory>();
            var authData = _authorizeBll.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                apiResult.Message = _selLangBll.GetMsg("msg_ForceLogedout");
                return apiResult;
            }
            var pageNumber = historyReq.PageNumber;
            apiResult.Data = _thirdPartyBll.GetTransferHistory(authData.CustId, historyReq.StartDate, historyReq.EndDate, ref pageNumber, out var totalPages, out var rowCount);
            apiResult.PageNumber = pageNumber;
            apiResult.TotalPages = totalPages;
            apiResult.RowCount = rowCount;
            apiResult.ErrorCode = 0;
            return apiResult;
        }

        public ApiResult<IEnumerable<TpTransferInfoHistory>> GetTransferRecent()
        {
            var apiResult = new ApiResult<IEnumerable<TpTransferInfoHistory>>();
            var authData = _authorizeBll.GetAuthData();
            if (authData is not { CustId: > 0 })
            {
                apiResult.Message = _selLangBll.GetMsg("msg_ForceLogedout");
                return apiResult;
            }

            apiResult.Data = _thirdPartyBll.GetTransferRecent(authData.CustId);
            apiResult.ErrorCode = 0;
            return apiResult;
        }
    }
}