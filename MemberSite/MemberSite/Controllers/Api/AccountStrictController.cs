
﻿using MemberSite.Service.BLL;
using MemberSite.Service.BLL.Interface;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using MemberSite.Service.Model.Mapping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using static MemberSite.Extensions.MethodExtension;
using static MemberSite.Controllers.Api.AccountStrictController;

namespace MemberSite.Controllers.Api
{
    [Authorize]
    [Route("api/[controller]/[action]")]
    [Route("api/[controller]/[action]/{LangId}")]
    [ApiController]
    public class AccountStrictController : ControllerBase
    {
        private readonly SelLangBLL _selLangBLL;
        private readonly AuthorizeBLL _authorizeBLL;
        private readonly CustInfoBll _custInfoBLL;
        private readonly BankInfoBLL _bankInfoBLL;
        private readonly CommonBLL _commonBLL;
        private readonly ServerInfoBll _serverInfoBLL;
        private readonly SelLangBLL _selLangBll;
        private readonly AuthBLL _authBll;
        private readonly CryptoPayInfoBll _cryptoPayInfoBLL;
        private readonly LineLoginBLL _lineLoginBll;
        private readonly StringVaildHelper _stringVaildHelper;
        private readonly IEnumerable<AreaCodeMap> _areaCodeMap;
        private readonly ILogger<AccountStrictController> _logger;
        private readonly ICustPhotoIdBLL _custPhoto;

        public AccountStrictController(IServiceProvider provider)
        {
            _selLangBLL = provider.GetRequiredService<SelLangBLL>();
            _authorizeBLL = provider.GetRequiredService<AuthorizeBLL>();
            _custInfoBLL = provider.GetRequiredService<CustInfoBll>();
            _bankInfoBLL = provider.GetRequiredService<BankInfoBLL>();
            _commonBLL = provider.GetRequiredService<CommonBLL>();
            _serverInfoBLL = provider.GetRequiredService<ServerInfoBll>();
            _selLangBll = provider.GetRequiredService<SelLangBLL>();
            _authBll = provider.GetRequiredService<AuthBLL>();
            _cryptoPayInfoBLL = provider.GetRequiredService<CryptoPayInfoBll>();
            _lineLoginBll = provider.GetRequiredService<LineLoginBLL>();
            _stringVaildHelper = provider.GetRequiredService<StringVaildHelper>();
            _custPhoto = provider.GetRequiredService<ICustPhotoIdBLL>();
            _areaCodeMap = provider.GetRequiredService<IEnumerable<AreaCodeMap>>();
            _logger = provider.GetRequiredService<ILogger<AccountStrictController>>();
        }

        public class BankInfo
        {
            public int SysId { set; get; }
            public string AccountName { set; get; }
            public string AccountNo { set; get; }
            public string IFSCCode { set; get; }
        }

        [HttpPost]
        public ApiResult SetCustBankInfo(BankInfo bankInfo)
        {
            var apiResult = new ApiResult(_selLangBLL);
            var authData = _authorizeBLL.GetAuthData();

            if (authData is { CustId: > 0 })
            {
                var custInfo = _custInfoBLL.GetCustInfoWithCache(authData.CustId);
                var bankInfoSysList = _bankInfoBLL.GetBankInfoSysListByCurrencyForCtrl(custInfo.CurrencyId);
                var blockBankAccountList = _bankInfoBLL.GetBlockBankAccount(bankInfo.SysId, bankInfo.AccountNo).ToList();

                var custBankDetail = _custInfoBLL.GetCustBankDetail(bankInfo.SysId, bankInfo.AccountNo, custInfo.SiteId).ToList();

                if (bankInfoSysList == null || bankInfoSysList.Count(x => x.SysId == bankInfo.SysId) == 0)
                {
                    apiResult.ErrorCode = -1;
                    apiResult.Message = _selLangBLL.GetMsg("msg_UpdateFailed");
                    return apiResult;
                }

                if (custBankDetail.Count > 0)
                {
                    var bankDetail = custBankDetail.Find(x => x.CustId == authData.CustId);
                    if (bankDetail == null)
                    {
                        apiResult.ErrorCode = -5;
                        apiResult.Message = _selLangBLL.GetMsg("msg_BankAccountDuplicate");
                        return apiResult;
                    }
                }

                if (blockBankAccountList.Count > 0)
                {
                    apiResult.ErrorCode = -1;
                    apiResult.Message = _selLangBLL.GetMsg("msg_UpdateFailed");
                    return apiResult;
                }

                var custBankInfo = _custInfoBLL.GetCustBankInfo(authData.CustId);
                if (custBankInfo == null && string.IsNullOrEmpty(bankInfo.AccountName))
                {
                    apiResult.ErrorCode = -2;
                    apiResult.Message = _selLangBLL.GetMsg("msg_NeedAccountName");
                    return apiResult;
                }
                else if (custBankInfo != null && custBankInfo.bankInfoDetail?.Count() >= 3)
                {
                    apiResult.ErrorCode = -3;
                    apiResult.Message = _selLangBLL.GetMsg("msg_UpdateFailed");
                    return apiResult;
                }
                else
                {
                    if (custBankInfo != null && custBankInfo.bankInfoDetail != null)
                    {
                        var bankInfoList = custBankInfo.bankInfoDetail.ToList();

                        if (bankInfoList.Count(x => x.SysId == bankInfo.SysId && x.AccountNo == bankInfo.AccountNo) > 0)
                        {
                            apiResult.ErrorCode = -4;
                            apiResult.Message = _selLangBLL.GetMsg("msg_BankAccountExists");
                            return apiResult;
                        }
                    }
                }
                apiResult.ErrorCode = _custInfoBLL.InsertCustBankInfo(authData.CustId, bankInfo.AccountName, bankInfo.SysId, bankInfo.AccountNo, bankInfo.IFSCCode, CustBankInfoLogStatus.Create);
                apiResult.Message = _selLangBLL.GetMsg("msg_Updatesuccessful");
            }

            return apiResult;
        }

        [HttpPost]
        public ApiResult DelCustBankInfo(BankInfo bankInfo)
        {
            var apiResult = new ApiResult(_selLangBLL);
            var authData = _authorizeBLL.GetAuthData();

            if (authData is { CustId: > 0 })
            {
                var custBankInfo = _custInfoBLL.GetCustBankInfo(authData.CustId);
                if (custBankInfo == null || custBankInfo.bankInfoDetail == null)
                {
                    apiResult.ErrorCode = -2;
                    apiResult.Message = _selLangBLL.GetMsg("msg_UpdateFailed");
                    return apiResult;
                }
                apiResult.ErrorCode = _custInfoBLL.DeleteCustBankInfo(authData.CustId, bankInfo.SysId, bankInfo.AccountNo, bankInfo.IFSCCode, CustBankDetailStatus.Disable);
                apiResult.Message = _selLangBLL.GetMsg(apiResult.ErrorCode == 0 ? "msg_Updatesuccessful" : "msg_UpdateFailed");
            }

            return apiResult;
        }

        public class USDTDetailreq
        {
            public string AccountName { set; get; }
            public string NickName { set; get; }
            public string USDTAddress { set; get; }
            public string Network { set; get; }
            public int CurrencyId { set; get; }
        }

        [HttpPost]
        public ApiResult SetCustUSDTInfo(USDTDetailreq uSDTDetailreq)
        {
            var apiResult = new ApiResult(_selLangBLL);
            var authData = _authorizeBLL.GetAuthData();

            if (authData is { CustId: > 0 })
            {
                var custBankInfo = _custInfoBLL.GetCustBankInfo(authData.CustId);
                var uSDTInfoList = new List<CryptoWalletDetail>();
                var uSDTInfoLogList = new List<CryptoWalletDetailLog>();

                if (custBankInfo == null && string.IsNullOrEmpty(uSDTDetailreq.AccountName))
                {
                    apiResult.ErrorCode = -2;
                    apiResult.Message = _selLangBLL.GetMsg("msg_NeedAccountName");
                    return apiResult;
                }
                else if (custBankInfo != null && custBankInfo.usdtDetail?.Count() >= 3)
                {
                    apiResult.ErrorCode = -3;
                    apiResult.Message = _selLangBLL.GetMsg("msg_UpdateFailed");
                    return apiResult;
                }
                else if (custBankInfo != null && custBankInfo.usdtDetail != null)
                {
                    uSDTInfoList = custBankInfo.usdtDetail.ToList();

                    if (uSDTInfoList.Any(x => x.USDTAddress == uSDTDetailreq.USDTAddress
                                              && x.CurrencyId == uSDTDetailreq.CurrencyId)) // 可能幣種不同但錢包位址相同
                    {
                        apiResult.ErrorCode = -4;
                        apiResult.Message = _selLangBLL.GetMsg("msg_UpdateFailed");
                        return apiResult;
                    }
                }

                var currencyId = uSDTDetailreq.CurrencyId == 0 ? 96 : uSDTDetailreq.CurrencyId;
                uSDTInfoList.Add(new CryptoWalletDetail
                {
                    NickName = uSDTDetailreq.NickName,
                    USDTAddress = uSDTDetailreq.USDTAddress,
                    Network = uSDTDetailreq.Network,
                    CurrencyId = currencyId
                });
                uSDTInfoLogList.Add(new CryptoWalletDetailLog
                {
                    NickName = uSDTDetailreq.NickName,
                    USDTAddress = uSDTDetailreq.USDTAddress,
                    Network = uSDTDetailreq.Network,
                    CurrencyId = currencyId,
                    Status = 1
                });

                apiResult.ErrorCode = _custInfoBLL.SetCustUSDTInfo(authData.CustId, uSDTDetailreq.AccountName,
                    uSDTInfoList, uSDTInfoLogList);
                apiResult.Message = _selLangBLL.GetMsg("msg_Updatesuccessful");

            }

            return apiResult;
        }

        public class CustUSDTInfoBatchreq
        {
            public string AccountName { set; get; }
            public IEnumerable<CryptoWalletDetail> USDTInfo { set; get; }
        }

        [HttpPost]
        public ApiResult SetCustUSDTInfoBatch(CustUSDTInfoBatchreq custUSDTInfoBatchreq)
        {
            var apiResult = new ApiResult(_selLangBLL);
            var authData = _authorizeBLL.GetAuthData();

            if (custUSDTInfoBatchreq.USDTInfo.Any(x =>
                    !_cryptoPayInfoBLL.CryptoAddressValidation(x.Network, x.USDTAddress)))
            {
                apiResult.ErrorCode = -5; // 錢包地址格式錯誤
                apiResult.Message = _selLangBLL.GetMsg("msg_InvalidCryptoWalletAddress");
                return apiResult;
            }

            if (custUSDTInfoBatchreq.USDTInfo.GroupBy(i => new { i.USDTAddress, i.CurrencyId }) // 可能幣種不同但錢包位址相同
                .Any(g => g.Count() > 1))
            {
                apiResult.ErrorCode = -4; //輸入資料重複
                apiResult.Message = _selLangBLL.GetMsg("msg_UpdateFailed");
                return apiResult;

            }

            List<CryptoWalletDetail> uSDTInfoList;
            var uSDTInfoLogList = new List<CryptoWalletDetailLog>();

            if (authData is { CustId: > 0 })
            {
                var custBankInfo = _custInfoBLL.GetCustBankInfo(authData.CustId);
                if (custBankInfo == null && string.IsNullOrEmpty(custUSDTInfoBatchreq.AccountName))
                {
                    apiResult.ErrorCode = -2;
                    apiResult.Message = _selLangBLL.GetMsg("msg_NeedAccountName");
                    return apiResult;
                }
                if (custBankInfo != null && (custBankInfo.usdtDetail?.Count() >= 3 || (custBankInfo.usdtDetail?.Count() + custUSDTInfoBatchreq.USDTInfo?.Count() > 3)))
                {
                    apiResult.ErrorCode = -3;
                    apiResult.Message = _selLangBLL.GetMsg("msg_UpdateFailed");
                    return apiResult;
                }
                if (custBankInfo != null && custBankInfo.usdtDetail != null)
                {
                    uSDTInfoList = custBankInfo.usdtDetail.ToList();
                    foreach (var custUSDTInfo in custUSDTInfoBatchreq.USDTInfo)
                    {
                        if (uSDTInfoList.Any(x => x.USDTAddress == custUSDTInfo.USDTAddress
                            && x.CurrencyId == custUSDTInfo.CurrencyId)) // 可能幣種不同但錢包位址相同
                        {
                            apiResult.ErrorCode = -4;
                            apiResult.Message = _selLangBLL.GetMsg("msg_UpdateFailed");
                            return apiResult;
                        }
                    }
                }
                else
                {
                    uSDTInfoList = new List<CryptoWalletDetail>();
                }
                foreach (var custUSDTInfo in custUSDTInfoBatchreq.USDTInfo)
                {
                    uSDTInfoList.Add(new CryptoWalletDetail { NickName = custUSDTInfo.NickName, USDTAddress = custUSDTInfo.USDTAddress, Network = custUSDTInfo.Network, CurrencyId = custUSDTInfo.CurrencyId });
                    uSDTInfoLogList.Add(new CryptoWalletDetailLog { NickName = custUSDTInfo.NickName, USDTAddress = custUSDTInfo.USDTAddress, Network = custUSDTInfo.Network, CurrencyId = custUSDTInfo.CurrencyId, Status = 1 });
                }

                apiResult.ErrorCode = _custInfoBLL.SetCustUSDTInfo(authData.CustId, custUSDTInfoBatchreq.AccountName, uSDTInfoList, uSDTInfoLogList);
                apiResult.Message = _selLangBLL.GetMsg("msg_Updatesuccessful");
            }

            return apiResult;
        }

        [HttpPost]
        public ApiResult DelCustUSDTInfo(USDTDetailreq uSDTDetailreq)
        {
            var apiResult = new ApiResult(_selLangBLL);
            var authData = _authorizeBLL.GetAuthData();

            IEnumerable<CryptoWalletDetail> USDTInfoList = null;
            var uSDTInfoLogList = new List<CryptoWalletDetailLog>();

            if (authData is { CustId: > 0 })
            {
                var custBankInfo = _custInfoBLL.GetCustBankInfo(authData.CustId);
                if (custBankInfo?.usdtDetail == null)
                {
                    apiResult.ErrorCode = -2;
                    apiResult.Message = _selLangBLL.GetMsg("msg_UpdateFailed");
                    return apiResult;
                }

                if (custBankInfo != null && custBankInfo.usdtDetail != null)
                {
                    USDTInfoList = custBankInfo.usdtDetail.Where(x =>
                        !(x.USDTAddress == uSDTDetailreq.USDTAddress &&
                          x.CurrencyId == uSDTDetailreq.CurrencyId));

                    var deluSDTInfo = custBankInfo.usdtDetail.FirstOrDefault(x =>
                        (x.USDTAddress == uSDTDetailreq.USDTAddress &&
                         x.CurrencyId == uSDTDetailreq.CurrencyId));
                    uSDTInfoLogList.Add(new CryptoWalletDetailLog { NickName = deluSDTInfo.NickName, USDTAddress = deluSDTInfo.USDTAddress, Network = deluSDTInfo.Network, CurrencyId = deluSDTInfo.CurrencyId, Status = 2 });
                }

                apiResult.ErrorCode = _custInfoBLL.SetCustUSDTInfo(authData.CustId, custBankInfo.AccountName, USDTInfoList, uSDTInfoLogList);
                apiResult.Message = _selLangBLL.GetMsg("msg_Updatesuccessful");
            }

            return apiResult;
        }

        public class SetAccountNameReq
        {
            [JsonProperty("name")]
            public string Name { get; set; }
        }

        /// <summary>
        /// For Goal 123 add account name direct
        /// </summary>
        /// <param name="content"></param>
        /// <returns>ApiResult</returns>
        [HttpPost]
        public ApiResult SetAccountName(SetAccountNameReq content)
        {
            var apiResult = new ApiResult(_selLangBLL);
            var authData = _authorizeBLL.GetAuthData();

            //check input
            if (string.IsNullOrWhiteSpace(content.Name))
            {
                apiResult.ErrorCode = -3;
                apiResult.Message = _selLangBLL.GetMsg("msg_NeedAccountName");
                return apiResult;
            }
            if (content.Name.Length > 50)
            {
                apiResult.ErrorCode = -4;
                return apiResult;
            }

            //check AccountName exist or not
            //existed
            if (authData is { CustId: > 0 })
            {
                if (_custInfoBLL.CheckAccountName(authData.CustId))
                {
                    apiResult.ErrorCode = -5;
                    return apiResult;
                }
                apiResult.ErrorCode = _custInfoBLL.SetCustBankInfo(authData.CustId, content.Name.Trim());
                apiResult.Message = _selLangBLL.GetMsg(apiResult.ErrorCode == 0 ? "msg_Updatesuccessful" : "msg_UpdateFailed");
            }

            return apiResult;
        }

        [HttpPost]
        public ApiResult<object> SetProfile(Dictionary<string, string> custProfile)
        {
            var apiResult = new ApiResult<object>(_selLangBLL);
            var authData = _authorizeBLL.GetAuthData();

            if (authData is { CustId: > 0 })
            {
                var custInfo = _custInfoBLL.GetCustInfoWithCache(authData.CustId);
                var beforeCustInfo = _custInfoBLL.GetCustInfoProfile(authData.CustId);
                if (custProfile.ContainsKey("AccountName"))
                    if (string.IsNullOrWhiteSpace(custProfile["AccountName"])
                        || !_stringVaildHelper.VaildAccountNameForSetProfile(custProfile["AccountName"]))
                    {
                        apiResult.ErrorCode = 21;
                        apiResult.Message = $"{_selLangBLL.GetMsg("msg_RealNameWrongFormat")}";
                        return apiResult;
                    }
                
                var accountName = custProfile["AccountName"].Trim();
                if (string.IsNullOrEmpty(beforeCustInfo.AccountName) && !string.IsNullOrEmpty(beforeCustInfo.PhoneNo))
                {
                    var result = _custInfoBLL.SetCustBankInfo(authData.CustId, accountName);

                    if (result != 0)
                    {
                        _logger.LogWarning($"CustId: {authData.CustId} SetCustBankInfo failed.");
                    }
                    if (!string.IsNullOrEmpty(beforeCustInfo.CountryCode))
                        custProfile["CountryCode"] = beforeCustInfo.CountryCode;
                    if (!string.IsNullOrEmpty(beforeCustInfo.AreaCode))
                        custProfile["AreaCode"] = beforeCustInfo.AreaCode;
                    if (!string.IsNullOrEmpty(beforeCustInfo.PhoneNo))
                        custProfile["PhoneNo"] = beforeCustInfo.PhoneNo;
                }
                else
                {
                    string AreaCode = custProfile[nameof(AreaCode)];
                    string PhoneNo = custProfile[nameof(PhoneNo)];

                    if (long.TryParse(PhoneNo, out var phoneNo))
                    {
                        PhoneNo = phoneNo.ToString();
                    }
                    else
                    {
                        apiResult.ErrorCode = 21;
                        apiResult.Message = $"({AreaCode}){PhoneNo} {_selLangBLL.GetMsg("msg_PhoneWrongFormat")}";
                        return apiResult;
                    }

                    if (_custInfoBLL.ExistsPhoneNo(authData.CustId, AreaCode, PhoneNo))
                    {
                        apiResult.ErrorCode = 21;
                        apiResult.Message = $"({AreaCode}){PhoneNo} {_selLangBLL.GetMsg("msg_HasExist")}";
                        return apiResult;
                    }

                    if (!string.IsNullOrEmpty(beforeCustInfo.Birth_d))
                        custProfile["Birth_d"] = beforeCustInfo.Birth_d;
                    if (!string.IsNullOrEmpty(beforeCustInfo.Birth_m))
                        custProfile["Birth_m"] = beforeCustInfo.Birth_m;
                    if (!string.IsNullOrEmpty(beforeCustInfo.Birth_y))
                        custProfile["Birth_y"] = beforeCustInfo.Birth_y;

                    if (string.IsNullOrEmpty(beforeCustInfo.Birth_d)
                        && string.IsNullOrEmpty(beforeCustInfo.Birth_m)
                        && string.IsNullOrEmpty(beforeCustInfo.Birth_y)
                        && !DateTime.TryParse($"{custProfile["Birth_y"]}-{custProfile["Birth_m"]}-{custProfile["Birth_d"]}", out _))
                    {
                        apiResult.ErrorCode = 21;
                        apiResult.Message = $"({_selLangBLL.GetMsg("msg_BirthdayIsRequired")}";
                        return apiResult;
                    }

                    if (!string.IsNullOrEmpty(beforeCustInfo.AccountName))
                    {
                        custProfile["AccountName"] = beforeCustInfo.AccountName;
                    }
                    else
                    {
                        var result = _custInfoBLL.SetCustBankInfo(authData.CustId, accountName);
                    }
                }

                if (custProfile.TryGetValue("Email", out var email) && !string.IsNullOrEmpty(email) &&
                    _custInfoBLL.ExistsEmail(authData.CustId, email))
                {
                    apiResult.ErrorCode = 21;
                    apiResult.Message = $"{email} {_selLangBLL.GetMsg("msg_HasExist")}";
                    return apiResult;
                }

                if (!string.IsNullOrEmpty(beforeCustInfo.Email))
                {
                    custProfile["Email"] = beforeCustInfo.Email;
                }

                custProfile = SetCountryCodeAndAreaCode(custProfile, beforeCustInfo, custInfo.SiteId);

                apiResult.ErrorCode = _custInfoBLL.SetProfile(authData.CustId, custProfile);
                if (apiResult.ErrorCode == 0)
                {
                    apiResult.Message = _selLangBLL.GetMsg("msg_Updatesuccessful");

                    var custInfoProfile = _custInfoBLL.GetCustInfoProfile(authData.CustId);
                    apiResult.Data = new
                    {
                        LangCode = _selLangBLL.GetLangId(),
                        custInfoProfile.UserName,
                        custInfoProfile.Email,
                        custInfoProfile.AccountName,
                        custInfoProfile.Birth_d,
                        custInfoProfile.Birth_m,
                        custInfoProfile.Birth_y,
                        custInfoProfile.RegTime,
                        LastLogin = custInfoProfile.LastLoginTime,
                        Country = custInfoProfile.CountryCode,
                        custInfoProfile.PhoneNo,
                        custInfoProfile.AddressCountry,
                        custInfoProfile.AddressState,
                        custInfoProfile.AddressPostCode,
                        custInfoProfile.AddressCity,
                        custInfoProfile.AddressLineFirst,
                        custInfoProfile.AddressLineSecond
                    };
                }
                else
                {
                    apiResult.Message = _selLangBLL.GetMsg("msg_UpdateFailed");
                }
            }

            return apiResult;
        }

        [HttpPost]
        public ApiResult<object> SetProfileV2(Dictionary<string, string> custProfile)
        {
            var apiResult = new ApiResult<object>(_selLangBLL);
            var authData = _authorizeBLL.GetAuthData();

            try
            {
                if (authData is { CustId: > 0 })
                {
                    var custInfo = _custInfoBLL.GetCustInfoWithCache(authData.CustId);
                    var beforeCustInfo = _custInfoBLL.GetCustInfoProfile(authData.CustId);
                    
                    if (string.IsNullOrEmpty(beforeCustInfo.AccountName) && !string.IsNullOrEmpty(beforeCustInfo.PhoneNo))
                    {
                        if(string.IsNullOrWhiteSpace(custProfile["AccountName"]))
                        {
                            apiResult.ErrorCode = 21;
                            apiResult.Message = $"{_selLangBLL.GetMsg("msg_RealNameWrongFormat")}";
                            return apiResult;
                        }
                        
                        var result = _custInfoBLL.SetCustBankInfo(authData.CustId, custProfile["AccountName"].Trim());
                        if (!string.IsNullOrEmpty(beforeCustInfo.CountryCode))
                            custProfile["CountryCode"] = beforeCustInfo.CountryCode;

                        if (!string.IsNullOrEmpty(beforeCustInfo.AreaCode))
                            custProfile["AreaCode"] = beforeCustInfo.AreaCode;

                        if (!string.IsNullOrEmpty(beforeCustInfo.PhoneNo))
                            custProfile["PhoneNo"] = beforeCustInfo.PhoneNo;
                    }
                    else
                    {
                        string AreaCode = custProfile[nameof(AreaCode)];
                        string PhoneNo = custProfile[nameof(PhoneNo)];

                        if (long.TryParse(PhoneNo, out var phoneNo))
                        {
                            PhoneNo = phoneNo.ToString();
                        }
                        else
                        {
                            apiResult.ErrorCode = 21;
                            apiResult.Message = $"({AreaCode}){PhoneNo} {_selLangBLL.GetMsg("msg_PhoneWrongFormat")}";
                            return apiResult;
                        }

                        if (_custInfoBLL.ExistsPhoneNo(authData.CustId, AreaCode, PhoneNo))
                        {
                            apiResult.ErrorCode = 21;
                            apiResult.Message = $"({AreaCode}){PhoneNo} {_selLangBLL.GetMsg("msg_HasExist")}";
                            return apiResult;
                        }

                        if (!string.IsNullOrEmpty(beforeCustInfo.Birth_d))
                            custProfile["Birth_d"] = beforeCustInfo.Birth_d;

                        if (!string.IsNullOrEmpty(beforeCustInfo.Birth_m))
                            custProfile["Birth_m"] = beforeCustInfo.Birth_m;

                        if (!string.IsNullOrEmpty(beforeCustInfo.Birth_y))
                            custProfile["Birth_y"] = beforeCustInfo.Birth_y;

                        if (!string.IsNullOrEmpty(beforeCustInfo.AccountName))
                        {
                            custProfile["AccountName"] = beforeCustInfo.AccountName;
                        }
                        else
                        {
                            var result = _custInfoBLL.SetCustBankInfo(authData.CustId, custProfile["AccountName"].Trim());
                        }
                    }

                    if (custProfile.TryGetValue("Email", out var email) && !string.IsNullOrEmpty(email) &&
                        _custInfoBLL.ExistsEmail(authData.CustId, email))
                    {
                        apiResult.ErrorCode = 21;
                        apiResult.Message = $"{email} {_selLangBLL.GetMsg("msg_HasExist")}";
                        return apiResult;
                    }

                    if (!string.IsNullOrEmpty(beforeCustInfo.Email))
                    {
                        custProfile["Email"] = beforeCustInfo.Email;
                    }

                    custProfile = SetCountryCodeAndAreaCode(custProfile, beforeCustInfo, custInfo.SiteId);

                    apiResult.ErrorCode = _custInfoBLL.SetProfileV2(authData.CustId, custProfile);
                    if (apiResult.ErrorCode == 0)
                    {
                        apiResult.Message = _selLangBLL.GetMsg("msg_Updatesuccessful");

                        var custInfoProfile = _custInfoBLL.GetCustInfoProfile(authData.CustId);
                        apiResult.Data = new
                        {
                            LangCode = _selLangBLL.GetLangId(),
                            custInfoProfile.UserName,
                            custInfoProfile.Email,
                            custInfoProfile.AccountName,
                            custInfoProfile.Birth_d,
                            custInfoProfile.Birth_m,
                            custInfoProfile.Birth_y,
                            custInfoProfile.RegTime,
                            LastLogin = custInfoProfile.LastLoginTime,
                            Country = custInfoProfile.CountryCode,
                            custInfoProfile.AreaCode,
                            custInfoProfile.PhoneNo,
                            custInfoProfile.AddressCountry,
                            custInfoProfile.AddressState,
                            custInfoProfile.AddressPostCode,
                            custInfoProfile.AddressCity,
                            custInfoProfile.AddressLineFirst,
                            custInfoProfile.AddressLineSecond
                        };
                    }
                    else
                    {
                        apiResult.Message = _selLangBLL.GetMsg("msg_UpdateFailed");
                    }
                }
            }
            catch (Exception ex)
            {
                apiResult.ErrorCode = 9000;
                apiResult.RawMessage = ex.ToString();
                apiResult.Message = ex.ToString();
            }
            return apiResult;
        }

        [HttpPost]
        public ApiResult<object> SetProfileAddress(Dictionary<string, string> custAddress)
        {
            var apiResult = new ApiResult<object>(_selLangBLL);
            var authData = _authorizeBLL.GetAuthData();
            try
            {
                if (authData is { CustId: > 0 })
                {
                    apiResult.ErrorCode = _custInfoBLL.SetProfileAddress(authData.CustId, custAddress);

                    if (apiResult.ErrorCode == 0)
                    {
                        apiResult.Message = _selLangBLL.GetMsg("msg_Updatesuccessful");

                        var custInfo = _custInfoBLL.GetCustInfoProfile(authData.CustId);
                        apiResult.Data = new
                        {
                            LangCode = _selLangBLL.GetLangId(),
                            custInfo.UserName,
                            custInfo.Email,
                            custInfo.AccountName,
                            custInfo.Birth_d,
                            custInfo.Birth_m,
                            custInfo.Birth_y,
                            custInfo.RegTime,
                            LastLogin = custInfo.LastLoginTime,
                            Country = custInfo.CountryCode,
                            custInfo.AreaCode,
                            custInfo.PhoneNo,
                            custInfo.AddressCountry,
                            custInfo.AddressState,
                            custInfo.AddressPostCode,
                            custInfo.AddressCity,
                            custInfo.AddressLineFirst,
                            custInfo.AddressLineSecond
                        };
                    }
                    else
                    {
                        apiResult.Message = _selLangBLL.GetMsg("msg_UpdateFailed");
                    }
                }
            }
            catch (Exception ex)
            {
                apiResult.ErrorCode = 9000;
                apiResult.RawMessage = ex.ToString();
                apiResult.Message = ex.ToString();
            }
            return apiResult;
        }

        private Dictionary<string, string> SetCountryCodeAndAreaCode(Dictionary<string, string> custProfile, CustInfoProfile beforeCustInfo, int siteId)
        {
            custProfile.TryGetValue("CountryCode", out var reqCountryCode);

            if (string.IsNullOrEmpty(reqCountryCode) || !_areaCodeMap.Any(i => i.Code == reqCountryCode))
            {
                if (!string.IsNullOrEmpty(beforeCustInfo.AreaCode))
                {
                    custProfile["CountryCode"] = _custInfoBLL.GetCountryNameByAreaCode(beforeCustInfo.AreaCode);
                }
                else if (siteId is (int)SiteId.Goal123 or (int)SiteId.Bwing)
                {
                    custProfile["CountryCode"] = "VN";
                }
            }

            custProfile.TryGetValue("AreaCode", out var reqAreaCode);

            if (string.IsNullOrEmpty(reqAreaCode) || !_areaCodeMap.Any(i => i.AreaCode == reqAreaCode))
            {
                if (!string.IsNullOrEmpty(beforeCustInfo.AreaCode))
                {
                    custProfile["AreaCode"] = beforeCustInfo.AreaCode;
                }
                else
                {
                    custProfile["AreaCode"] = _custInfoBLL.GetAreaCodeByCountryName(custProfile["CountryCode"]);
                }
            }

            return custProfile;

        }

        [HttpPost]
        public ApiResult<object> GetCustProfile()
        {
            var apiResult = new ApiResult<object>(_selLangBLL);
            var authData = _authorizeBLL.GetAuthData();

            try
            {
                if (authData is { CustId: > 0 })
                {
                    var custInfo = _custInfoBLL.GetCustInfoProfile(authData.CustId);
                    var _perCountry = "";
                    if (custInfo != null)
                    {
                        if (custInfo.CurrencyId == 96)
                        {
                            _perCountry = _custInfoBLL.GetCountryNameByAreaCode(custInfo.AreaCode);
                        }
                        else
                        {
                            _perCountry = _custInfoBLL.GetCountryNameByAreaCode(custInfo.AreaCode);
                        }
                    }

                    var isLineBinding = _lineLoginBll.GetCustLineProfile(authData.CustId, null) != null;

                    apiResult.Data = new
                    {
                        custInfo.UserName,
                        custInfo.Email,
                        custInfo.AccountName,
                        custInfo.Birth_d,
                        custInfo.Birth_m,
                        custInfo.Birth_y,
                        custInfo.RegTime,
                        LastLogin = custInfo.LastLoginTime,
                        Country = string.IsNullOrEmpty(custInfo.CountryCode) ? null : custInfo.CountryCode,
                        custInfo.AreaCode,
                        custInfo.PhoneNo,
                        PerCountry = _perCountry,
                        custInfo.CurrencyId,
                        langId = custInfo.UserLang,
                        custInfo.UserLevel,
                        custInfo.PhoneVerified,
                        custInfo.PhoneReward,
                        custInfo.EmailVerified,
                        custInfo.EmailReward,
                        custInfo.EnablePromotion,
                        custInfo.ActStatus,
                        IsLineBinding = isLineBinding,
                        custInfo.AddressCountry,
                        custInfo.AddressState,
                        custInfo.AddressPostCode,
                        custInfo.AddressCity,
                        custInfo.AddressLineFirst,
                        custInfo.AddressLineSecond,
                        custInfo.IsResetPwd
                    };

                    apiResult.ErrorCode = 0;
                }
            }
            catch (Exception ex)
            {
                apiResult.RawMessage = ex.Message;
            }

            return apiResult;
        }

        private readonly Dictionary<string, System.Drawing.Imaging.ImageFormat> _contentTypesCheck = new Dictionary<string, System.Drawing.Imaging.ImageFormat>(comparer: StringComparer.OrdinalIgnoreCase)
        {
            {"image/png",System.Drawing.Imaging.ImageFormat.Png},
            {"image/jpeg",System.Drawing.Imaging.ImageFormat.Jpeg},
            { "image/gif",System.Drawing.Imaging.ImageFormat.Gif}
        };

        public class CustPhotoIdReq
        {
            public IFormFile PhotoId { get; set; }
        }

        [HttpPost]
        public async Task<ApiResult> UploadCustPhotoId([FromForm] CustPhotoIdReq custPhotoIdReq)
        {
            var apiResult = new ApiResult();
            var authData = _authorizeBLL.GetAuthData();
            if (authData is { CustId: > 0 })
            {
                var custInfo = _custInfoBLL.GetCustInfoWithCache(authData.CustId);

                if (!_contentTypesCheck.ContainsKey(custPhotoIdReq.PhotoId.ContentType))
                {
                    return new ApiResult { ErrorCode = -1, Message = _selLangBLL.GetMsg("msg_UpdateFailed") };
                }

                var result = await _custPhoto.UploadPhotoIdAsync(custInfo.CustId, custInfo.SiteId, custPhotoIdReq.PhotoId);

                if (result > 0)
                {
                    apiResult.ErrorCode = 0;
                }
                else
                {
                    apiResult.ErrorCode = -1;
                }
            }

            return apiResult;
        }

        public class ResetPasswordModel
        {
            public string NewPassword { set; get; }
        }

        [HttpPost]
        public ApiResult ResetPassword(ResetPasswordModel resetPassword)
        {
            var apiResult = new ApiResult();
            var authData = _authorizeBLL.GetAuthData();
            if (authData is { CustId: > 0 })
            {
                if (_custInfoBLL.SetResetPwd(authData.CustId, resetPassword.NewPassword))
                {
                    apiResult.ErrorCode = 0;
                    apiResult.Message = _selLangBLL.GetMsg("ResetPwdSuccess");
                }
                else
                {
                    apiResult.Message = _selLangBLL.GetMsg("msg_UpdateFailed");
                }
            }

            return apiResult;
        }

        [HttpPost]
        public async Task<ApiResult<SendOTPSMSResult>> SendOTPSMSAsync()
        {
            var apiResult = new ApiResult<SendOTPSMSResult>();
            var authData = _authorizeBLL.GetAuthData();
            if (authData is { CustId: > 0 })
            {
                apiResult = await _commonBLL.SendOTPSMSAsync(authData.CustId);
            }

            return apiResult;
        }

        public class VerifyOTPReq
        {
            public string inputOTPcode { set; get; }
        }

        [HttpPost]
        public async Task<ApiResult> VerifyOTPAsync(VerifyOTPReq verifyOTPReq)
        {
            var apiResult = new ApiResult();
            var authData = _authorizeBLL.GetAuthData();
            if (authData is { CustId: > 0 })
            {
                apiResult = await _custInfoBLL.VerifyOTPAsync(authData.CustId, verifyOTPReq.inputOTPcode);
            }

            return apiResult;
        }

        [HttpPost]
        public async Task<ApiResult<SendOTPSMSResult>> GetOTPVerifyInfo()
        {
            var apiResult = new ApiResult<SendOTPSMSResult>();
            var authData = _authorizeBLL.GetAuthData();
            if (authData is { CustId: > 0 })
            {
                apiResult = await _commonBLL.GetOTPVerifyInfo(authData.CustId);
            }

            return apiResult;
        }

        public class SendOTPEmailReq
        {
            public string route { get; set; }
        }

        [HttpPost]
        public async Task<ApiResult<SendOTPEmailResult>> SendOTPEmailAsync(SendOTPEmailReq req)
        {
            var apiResult = new ApiResult<SendOTPEmailResult>();
            var authData = _authorizeBLL.GetAuthData();
            var profileUrl = _serverInfoBLL.GetHttpDomain();
            var langId = _selLangBLL.GetLangId();

            var host = Url.ActionContext.HttpContext.Request.Host.Host;
            if (host.Split('.')[0] == "app")
            {
                profileUrl = $"{Url.ActionContext.HttpContext.Request.Scheme}://{host.Replace("app", "m")}";
                req.route = host == "app.n17881788.com" ? "profile" : "member";
            }
            if (!string.IsNullOrEmpty(langId) && req.route != "member")
                profileUrl += $"/{langId}";

            profileUrl += $"/{req.route}";

            if (authData is { CustId: > 0 })
            {
                apiResult = await _commonBLL.SendOTPEmailAsync(authData.CustId, profileUrl);
            }

            return apiResult;
        }

        public class VerifyEmailReq
        {
            public string key { get; set; }
            public string code { get; set; }
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<ApiResult> VerifyEmailAsync(VerifyEmailReq req, [FromRoute] string langId)
        {
            var apiResult = new ApiResult(_selLangBll);
            try
            {
                if (_authBll.Authenticate(req.key, out CustInfoProfile custInfoProfile))
                {
                    Customer customer = null;
                    var result = _custInfoBLL.GetCustomer(custInfoProfile.UserName, ref customer);
                    if (result != 0)
                    {
                        apiResult.ErrorCode = result;
                        return apiResult;
                    }

                    var authData = _authorizeBLL.GetAuthData();
                    if (authData is { CustId: > 0 } && authData.CustId != customer.CustId)
                    {
                        apiResult.ErrorCode = 1;
                    }
                    else
                    {
                        var verifyResult = await _custInfoBLL.VerifyEmailAsync(customer.CustId, req.code);
                        apiResult.ErrorCode = verifyResult.ErrorCode;
                    }
                }
                else
                {
                    apiResult.ErrorCode = 403;
                }
            }
            catch (Exception e)
            {
                apiResult.ErrorCode = 403;
                _logger.LogError(e, GetCurrentMethod());
            }

            return apiResult;
        }

        public class EWalletInfo
        {
            public int SysId { set; get; }
            public string AccountNo { set; get; }
        }

        [HttpPost]
        public ApiResult SetCustEWalletInfo(EWalletInfo eWalletInfo)
        {
            var apiResult = new ApiResult(_selLangBLL);
            var authData = _authorizeBLL.GetAuthData();

            if (authData is { CustId: > 0 })
            {
                var custInfo = _custInfoBLL.GetCustInfoWithCache(authData.CustId);

                var eWalletInfoSysList = _bankInfoBLL.GetEWalletInfoSysListByCurrency(custInfo.CurrencyId);

                var blockEWalletAccountList = _bankInfoBLL.GetBlockEWalletAccount(eWalletInfo.SysId, eWalletInfo.AccountNo).ToList();

                var custEWalletDetail = _custInfoBLL.GetCustEWalletDetail(eWalletInfo.SysId, eWalletInfo.AccountNo, custInfo.SiteId).ToList();

                if (eWalletInfoSysList == null || eWalletInfoSysList.Count(x => x.SysId == eWalletInfo.SysId) == 0)
                {
                    apiResult.ErrorCode = -1;
                    apiResult.Message = _selLangBLL.GetMsg("msg_UpdateFailed");
                    return apiResult;
                }

                if (custEWalletDetail.Count > 0)
                {
                    var bankDetail = custEWalletDetail.Find(x => x.CustId == authData.CustId);
                    if (bankDetail == null)
                    {
                        apiResult.ErrorCode = -5;
                        apiResult.Message = _selLangBLL.GetMsg("msg_BankAccountDuplicate");
                        return apiResult;
                    }
                }

                if (blockEWalletAccountList.Count > 0)
                {
                    apiResult.ErrorCode = -1;
                    apiResult.Message = _selLangBLL.GetMsg("msg_UpdateFailed");
                    return apiResult;
                }

                var custEWalletInfo = _custInfoBLL.GetCustEWalletInfo(authData.CustId);

                if (custEWalletInfo != null && custEWalletInfo?.Count() >= 3)
                {
                    apiResult.ErrorCode = -3;
                    apiResult.Message = _selLangBLL.GetMsg("msg_UpdateFailed");
                    return apiResult;
                }
                else if (custEWalletInfo != null)
                {
                    if (custEWalletInfo.Count(x => x.SysId == eWalletInfo.SysId && x.AccountNo == eWalletInfo.AccountNo) > 0)
                    {
                        apiResult.ErrorCode = -4;
                        apiResult.Message = _selLangBLL.GetMsg("msg_BankAccountExists");
                        return apiResult;
                    }
                }

                apiResult.ErrorCode = _custInfoBLL.InsertCustEWalletDetail(authData.CustId,
                                                                           eWalletInfo.SysId,
                                                                           eWalletInfo.AccountNo,
                                                                           CustEWalletDetailLogStatus.Create);
                apiResult.Message = _selLangBLL.GetMsg("msg_Updatesuccessful");
            }

            return apiResult;
        }

        [HttpPost]
        public ApiResult DelCustEWalletInfo(EWalletInfo eWalletInfo)
        {
            var apiResult = new ApiResult(_selLangBLL);
            var authData = _authorizeBLL.GetAuthData();

            if (authData is { CustId: > 0 })
            {
                var custEWalletInfo = _custInfoBLL.GetCustEWalletInfo(authData.CustId);
                if (custEWalletInfo == null)
                {
                    apiResult.ErrorCode = -2;
                    apiResult.Message = _selLangBLL.GetMsg("msg_UpdateFailed");
                    return apiResult;
                }
                apiResult.ErrorCode = _custInfoBLL.DeleteCustEWalletDetail(authData.CustId, eWalletInfo.SysId, eWalletInfo.AccountNo, CustEWalletDetailStatus.Disable);
                apiResult.Message = _selLangBLL.GetMsg(apiResult.ErrorCode == 0 ? "msg_Updatesuccessful" : "msg_UpdateFailed");
            }

            return apiResult;
        }

        public class CustCryptoWalletreq
        {
            public string NickName { set; get; }
            public string Address { set; get; }
            public string Network { set; get; }
            public int CurrencyId { set; get; }
        }
        [HttpPost]
        public ApiResult SetCustCryptoWallet(CustCryptoWalletreq custCryptoWalletreq)
        {
            var apiResult = new ApiResult(_selLangBLL);
            var authData = _authorizeBLL.GetAuthData();
            custCryptoWalletreq.Address = custCryptoWalletreq.Address.Trim();

            if (!_cryptoPayInfoBLL.CryptoAddressValidation(custCryptoWalletreq.Network, custCryptoWalletreq.Address))
            {
                apiResult.ErrorCode = -5; // 錢包地址格式錯誤
                apiResult.Message = _selLangBLL.GetMsg("msg_InvalidCryptoWalletAddress");
                return apiResult;
            }

            if (authData is { CustId: > 0 })
            {
                var custInfo = _custInfoBLL.GetCustInfoWithCache(authData.CustId);
                var custBankInfo = _custInfoBLL.GetCustBankInfo(authData.CustId);
                var custCryptoWalletDetail = _custInfoBLL.GetCustCryptoWalletDetail(custCryptoWalletreq.Address, custInfo.SiteId).ToList();
                var blockCryptoWallet = _bankInfoBLL.GetBlockCryptoWallet(custCryptoWalletreq.Address).ToList();

                if (custBankInfo == null)
                {
                    apiResult.ErrorCode = -2;
                    apiResult.Message = _selLangBLL.GetMsg("msg_NeedAccountName");
                    return apiResult;
                }
                else if (custBankInfo.usdtDetail?.Count() >= 3)
                {
                    apiResult.ErrorCode = -3;
                    apiResult.Message = _selLangBLL.GetMsg("msg_UpdateFailed");
                    return apiResult;
                }
                else if (custBankInfo.usdtDetail != null && custBankInfo.usdtDetail.Any(x => x.USDTAddress == custCryptoWalletreq.Address && x.CurrencyId == custCryptoWalletreq.CurrencyId))
                {
                    // 可能同一個CUST幣種不同但錢包位址相同,這種狀況是允許的
                    apiResult.ErrorCode = -4;
                    apiResult.Message = _selLangBLL.GetMsg("msg_CryptoWalletDuplicate");
                    return apiResult;
                }
                else if (custCryptoWalletDetail.Count > 0 && custCryptoWalletDetail.Find(x => x.CustId == authData.CustId) == null)
                {
                    apiResult.ErrorCode = -6;
                    apiResult.Message = _selLangBLL.GetMsg("msg_CryptoWalletDuplicate");
                    return apiResult;
                }
                else if (blockCryptoWallet.Count > 0)
                {
                    apiResult.ErrorCode = -1;
                    apiResult.Message = _selLangBLL.GetMsg("msg_UpdateFailed");
                    return apiResult;
                }

                apiResult.ErrorCode = _custInfoBLL.InsertCustCryptoWalletDetail(authData.CustId, custCryptoWalletreq.CurrencyId, custCryptoWalletreq.Network, custCryptoWalletreq.Address, custCryptoWalletreq.NickName, custBankInfo.AccountName);
                apiResult.Message = _selLangBLL.GetMsg(apiResult.ErrorCode == 0 ? "msg_Updatesuccessful" : "msg_UpdateFailed");
            }

            return apiResult;
        }

        [HttpPost]
        public ApiResult DelCustCryptoWallet(CustCryptoWalletreq custCryptoWalletreq)
        {
            var apiResult = new ApiResult(_selLangBLL);
            var authData = _authorizeBLL.GetAuthData();

            if (authData is { CustId: > 0 })
            {
                var custCryptoWalletDetail = _custInfoBLL.GetCustCryptoWalletDetail(authData.CustId);
                if (custCryptoWalletDetail == null)
                {
                    apiResult.ErrorCode = -2;
                    apiResult.Message = _selLangBLL.GetMsg("msg_UpdateFailed");
                    return apiResult;
                }
                apiResult.ErrorCode = _custInfoBLL.DeleteCustCryptoWalletDetail(authData.CustId, custCryptoWalletreq.CurrencyId, custCryptoWalletreq.Network, custCryptoWalletreq.Address, custCryptoWalletreq.NickName);
                apiResult.Message = _selLangBLL.GetMsg(apiResult.ErrorCode == 0 ? "msg_Updatesuccessful" : "msg_UpdateFailed");
            }

            return apiResult;
        }
    }
}
