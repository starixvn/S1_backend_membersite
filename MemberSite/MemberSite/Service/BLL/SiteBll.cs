using System.Drawing.Drawing2D;
using System.Text.RegularExpressions;
using MemberSite.Controllers.Api;
using MemberSite.Service.DAL;
using MemberSite.Service.DAL.Interface;
using MemberSite.Service.Domain;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using Org.BouncyCastle.Asn1.Tsp;
using static MemberSite.Controllers.Api.AccountStrictController;

namespace MemberSite.Service.BLL;

public class SiteBll
{
    private readonly CustInfoBll _custInfoBll;
    private readonly SelLangBLL _selLangBll;
    private readonly CommonBLL _commonBll;
    private readonly BankInfoBLL _bankInfoBll;
    private readonly Tools _tools;
    private readonly IAffiliateService _affiliateService;
    private readonly AgentInfoService _agentInfoService;
    private readonly Regex _regexUserName = new(@"^[a-zA-Z0-9]{6,18}$");
    private readonly Regex _regexPhoneNumber = new(@"^\+60[0-9]{9,10}$|\+62[0-9]{10,13}$|\+86[0-9]{10,11}$|\+91[0-9]{10}$|\+66[0-9]{9}$|\+880[0-9]{10}$|\+855[0-9]{8,10}$|\+84[0-9]{9}|\+82[0-9]{9,10}|\+81[0-9]{10}|\+55[0-9]{10,11}$");
    private readonly Regex _regexEmail = new(@"^\w+((-\w+)|(\.\w+))*@[A-Za-z0-9]+((\.|-)[A-Za-z0-9]+)*\.[A-Za-z]+$");
    private readonly Regex _regexPhoneNo = new(@"^\d+$");
    private readonly RecaptchaBLL _recaptcha;
    private readonly ServerInfoBll _serverInfoBll;
    private readonly ILogger<SiteBll> _logger;

    public SiteBll(IServiceProvider provider)
    {
        _custInfoBll = provider.GetRequiredService<CustInfoBll>();
        _selLangBll = provider.GetRequiredService<SelLangBLL>();
        _commonBll = provider.GetRequiredService<CommonBLL>();
        _bankInfoBll = provider.GetRequiredService<BankInfoBLL>();
        _tools = provider.GetRequiredService<Tools>();
        _affiliateService = provider.GetRequiredService<IAffiliateService>();
        _agentInfoService = provider.GetRequiredService<AgentInfoService>();
        _recaptcha = provider.GetRequiredService<RecaptchaBLL>();
        _serverInfoBll = provider.GetRequiredService<ServerInfoBll>();
        _logger = provider.GetRequiredService<ILogger<SiteBll>>();
    }

    public Customer Signup(SiteController.SignupModel signupModel, ApiResult apiResult, bool isAuthenticated,
        string fromUrl, string userAgent, bool checkReCaptchaToken)
    {
        AffCodeObject affCodeObj = null;
        var check = CheckSignUpData(signupModel, apiResult, isAuthenticated, fromUrl, checkReCaptchaToken, affCodeObj ,out var agent , out var deviceType ,out var signupIp);

        if (!check)
        {
            return null;
        }
        
        var result = _custInfoBll.AddCustInfo(signupModel.UserName, signupModel.Userpwd, signupModel.Email,
            signupModel.CurrencyId, signupModel.UserLang, signupModel.RefId, signupModel.AffId, signupModel.Aff,
            affCodeObj, signupModel.LineUserId, deviceType, agent, out var custId, signupModel.SignupUrl, signupModel.ReferralUrl, signupModel.PhoneNo,
            signupModel.AreaCode, signupModel.RealName, signupModel.PromoCode, signupModel.Birth_d,
            signupModel.Birth_m, signupModel.Birth_y, signupIp);

        AfterSignUp(signupModel, fromUrl, userAgent, deviceType, result, custId, apiResult, out var customer); 
            
        return customer;
    }

    public Customer SignupPhone(string otpNum ,SiteController.SignupModel signupModel, ApiResult apiResult, bool isAuthenticated,
        string fromUrl, string userAgent, bool checkReCaptchaToken)
    {
        AffCodeObject affCodeObj = null;
        signupModel.UserName = $"P{signupModel.AreaPhoneNo.Trim('+')}";
        
        var check = CheckSignUpData(signupModel, apiResult, isAuthenticated, fromUrl, checkReCaptchaToken, affCodeObj , out var agent , out var deviceType ,out var signupIp);

        if (!check)
        {
            return null;
        }
        
        #region OTP
        
        // No need anymore
        // var verifyResult = _custInfoBll.SignUpVerifyOTPAsync(signupModel.AreaPhoneNo, otpNum).Result;
        // if (verifyResult.ErrorCode != 0)
        // {
        //     apiResult.ErrorCode = -11;
        //     apiResult.Message = $"Wrong Otp Number";
        //     return null;
        //     //todo翻譯
        // }

        #endregion
        
        var result = _custInfoBll.AddCustInfo(signupModel.UserName, signupModel.Userpwd, signupModel.Email, 
            signupModel.CurrencyId, signupModel.UserLang, signupModel.RefId, signupModel.AffId, signupModel.Aff,
            affCodeObj, signupModel.LineUserId, deviceType, agent, out var custId, signupModel.SignupUrl, signupModel.ReferralUrl, signupModel.PhoneNo,
            signupModel.AreaCode, signupModel.RealName, signupModel.PromoCode, signupModel.Birth_d,
            signupModel.Birth_m, signupModel.Birth_y, signupIp, signupModel.AccountNo, signupModel.IFSCCode, signupModel.SysId);
        
        AfterSignUp(signupModel, fromUrl, userAgent, deviceType, result, custId, apiResult, out var customer);

        // No need anymore
        // if (apiResult.ErrorCode == 0)
        // {
        //     _commonBll.SetOTPLogCustInfo(customer.SiteId, customer.CustId ,customer.CurrencyId ,customer.UserName ,otpNum);
        // }
        
        return customer;
    }
    
    /// <summary>
/// 只給Ivorysport使用
/// </summary>
/// <param name="signupModel"></param>
/// <param name="apiResult"></param>
/// <param name="isAuthenticated"></param>
/// <param name="host"></param>
/// <param name="userAgent"></param>
/// <param name="checkReCaptchaToken"></param>
/// <returns></returns>
    public Customer IvorysportSignup(SiteController.IvorysportSignupModel signupModel, ApiResult apiResult, bool isAuthenticated,
        string host, string userAgent, bool checkReCaptchaToken)
    {
        AffCodeObject affCodeObj = null;
        var check = CheckSignUpData(signupModel, apiResult, isAuthenticated, host, checkReCaptchaToken, affCodeObj, out var agent , out var deviceType ,out var signupIp);
        if (!check)
        {
            return null;
        }

        #region CheckSignUpDataIvorysport

        if (string.IsNullOrWhiteSpace(signupModel.Nationality))
        {
            apiResult.ErrorCode = -30;
            apiResult.Message = _selLangBll.GetMsg("msg_NeedNationality");
            return null;
        }

        if (string.IsNullOrWhiteSpace(signupModel.Country) || string.IsNullOrWhiteSpace(signupModel.City) ||
            string.IsNullOrWhiteSpace(signupModel.PostCode) || string.IsNullOrWhiteSpace(signupModel.LineFirst))
        {
            apiResult.ErrorCode = -31;
            apiResult.Message = _selLangBll.GetMsg("msg_NeedAddress");
            return null;
        }

        if (!_regexEmail.IsMatch(signupModel.Email))
        {
            apiResult.ErrorCode = -10;
            apiResult.Message = _selLangBll.GetMsg("msg_EmailFormatVerify");
            return null;
        }
        
        if (!string.IsNullOrWhiteSpace(signupModel.Email))
        {
            if (_custInfoBll.ExistsEmail(0, signupModel.Email))
            {
                apiResult.ErrorCode = -10;
                apiResult.Message = $"{_selLangBll.GetResourceValue("lbl_Email")} {_selLangBll.GetMsg("msg_HasExist")}";
                return null;
            }
        }
        
        #endregion
        
        var result = _custInfoBll.IvorysportAddCustInfo(signupModel.UserName, signupModel.Userpwd, signupModel.Email,
            signupModel.CurrencyId, signupModel.UserLang, signupModel.RefId, signupModel.AffId, signupModel.Aff,
            affCodeObj, signupModel.LineUserId, deviceType, signupModel.PhotoId, agent ,out var custId, signupModel.SignupUrl, signupModel.ReferralUrl, signupModel.PhoneNo,
            signupModel.AreaCode, signupModel.RealName, signupModel.PromoCode, signupModel.Birth_d,
            signupModel.Birth_m, signupModel.Birth_y, signupIp, signupModel.Nationality, signupModel.Occupation, signupModel.SourceOfIncome, signupModel.Country, signupModel.City, signupModel.PostCode, signupModel.LineFirst);

        AfterSignUp(signupModel, host, userAgent, deviceType, result, custId, apiResult, out var customer); 
            
        return customer;
    }
    
    private bool CheckSignUpData(SiteController.SignupModel signupModel, ApiResult apiResult, bool isAuthenticated, string host, bool checkReCaptchaToken,AffCodeObject affCodeObj ,
        out Customer agent ,out int deviceType,out string signupIp)
    {
        agent = null;
        deviceType = 0;
        signupIp = null;
        
        if (isAuthenticated || (string.IsNullOrEmpty(signupModel.AreaCode) && !string.IsNullOrEmpty(signupModel.PhoneNo)))
        {
            _logger.LogError($"isAuthenticated={isAuthenticated} AreaCode={signupModel.AreaCode} PhoneNo={signupModel.PhoneNo}");
            apiResult.ErrorCode = 9000;
            return false;
        }

        var serverInfo = _serverInfoBll.GetServerInfo();
        if (serverInfo.SiteId is 1 or 5)
        {
            switch (signupModel.AreaCode)
            {
                case "+60":
                case "+62":
                case "+86":
                case "+91":
                case "+66":
                case "+880":
                case "+855":
                case "+84":
                case "+82":
                case "+81":
                case "+55":
                    if (!_regexPhoneNumber.IsMatch(signupModel.AreaCode + signupModel.PhoneNo))
                    {
                        var message = signupModel.AreaCode switch
                        {
                            "+60" => "9-10",
                            "+62" => "10-13",
                            "+86" => "10-11",
                            "+91" => "10",
                            "+66" => "9",
                            "+880" => "10",
                            "+855" => "8-10",
                            "+84" => "9",
                            "+82" => "9-10",
                            "+81" => "10",
                            "+55" => "10-11",
                            _ => ""
                        };
                        apiResult.ErrorCode = -20;
                        apiResult.Message = string.Format(_selLangBll.GetMsg("msg_PhoneFormatVerify"), message);
                        return false;
                    }
                    break;
            }
        }

        if (checkReCaptchaToken && !string.IsNullOrEmpty(signupModel.ReCaptchaToken))
        {
            var verifyResult = _recaptcha.Verify(signupModel.ReCaptchaToken);
            if (!verifyResult.success)
            {
                apiResult.ErrorCode = -97;
                apiResult.Message = verifyResult.errorCodes.FirstOrDefault();
                return false;
            }
        }

        if (_regexPhoneNo.IsMatch(signupModel.PhoneNo))
        {
            if (_custInfoBll.ExistsPhoneNo(0, signupModel.AreaCode, signupModel.PhoneNo))
            {
                apiResult.ErrorCode = -6;
                apiResult.Message = $"{_selLangBll.GetResourceValue("lbl_Phone")} {_selLangBll.GetMsg("msg_HasExist")}";
                return false;
            }
        }
        else if (!string.IsNullOrEmpty(signupModel.PhoneNo))
        {
            apiResult.ErrorCode = -20;
            apiResult.Message = $"{_selLangBll.GetResourceValue("lbl_Phone")} {_selLangBll.GetMsg("msg_PhoneWrongFormat")}";
            return false;
        }

        //if (!string.IsNullOrWhiteSpace(signupModel.Email))
        //{
        //    if (_custInfoBLL.ExistsEmail(0, signupModel.Email))
        //    {
        //        apiResult.ErrorCode = -10;
        //        apiResult.Message = $"{_selLangBll.GetResourceValue("lbl_Email")} {_selLangBll.GetMsg("msg_HasExist")}";
        //        return customer;
        //    }
        //}
        
        if (!_tools.CheckUserName(signupModel.UserName))
        {
            apiResult.ErrorCode = -2;
            apiResult.Message = _selLangBll.GetMsg("msg_UsernameValidate");
            return false;
        }
        
        if (!_regexUserName.IsMatch(signupModel.UserName))
        {
            apiResult.ErrorCode = -8;
            apiResult.Message = _selLangBll.GetMsg("msg_UsernameValidate");
            return false;
        }
        
        if (!string.IsNullOrWhiteSpace(signupModel.AffCode))
        {
            if (signupModel.AffCode.Trim().Length == 5)
            {
                 affCodeObj = new AffCodeObject(signupModel.AffCode);
                if (!affCodeObj.IsValid())
                {
                    apiResult.ErrorCode = -9;
                    apiResult.Message = _selLangBll.GetMsg("msg_AffCodeValidate");
                    return false;
                }
            }
            else if (signupModel.AffCode.Trim().Length == 8)
            {
                var rffCodeObj = new ReferralObject(signupModel.AffCode);
                if (!rffCodeObj.IsValid())
                {
                    apiResult.ErrorCode = -9;
                    apiResult.Message = _selLangBll.GetMsg("msg_AffCodeValidate");
                    return false;
                }
                if (rffCodeObj.IsExists(_custInfoBll.GetAffiliateAgentBll) < 0)
                {
                    apiResult.ErrorCode = -10;
                    apiResult.Message = _selLangBll.GetMsg("msg_AffCodeNotFound");
                    return false;
                }
                if (rffCodeObj.CurrencyId != signupModel.CurrencyId)
                {
                    apiResult.ErrorCode = -10;
                    apiResult.Message = _selLangBll.GetMsg("msg_AffCodeNotFound");
                    return false;
                }
                signupModel.AffId = signupModel.AffCode;
                signupModel.AffCode = null;
            }
            else
            {
                apiResult.ErrorCode = -10;
                apiResult.Message = _selLangBll.GetMsg("msg_AffCodeNotFound");
                return false;
            }
        }

        if (string.IsNullOrEmpty(signupModel.SignupUrl))
        {
            signupModel.SignupUrl = host;
        }

        // URL tag with specific affcode
        if (host is "www.nova88fun.com" or "m.nova88fun.com")
        {
            signupModel.AffCode = "nova88tvads";
        }
        
        if (string.IsNullOrEmpty(signupModel.AffId) && !serverInfo.ICurrencyList.Contains(signupModel.CurrencyId))
        {
            apiResult.ErrorCode = -1;
            apiResult.Message = _selLangBll.GetMsg("msg_ThisCurrencyNotSupport");
            return false;
        }

        if (!_tools.CheckUserInputPassword(signupModel.Userpwd))
        {
            apiResult.ErrorCode = -3;
            apiResult.Message = _selLangBll.GetMsg("msg_PasswordValidate");
            return false;
        }

        if (!serverInfo.ILangList.Contains(signupModel.UserLang))
        {
            apiResult.ErrorCode = -4;
            apiResult.Message = "";
            return false;
        }

        if (affCodeObj != null && !affCodeObj.IsExists(serverInfo.SiteId, _affiliateService.ExistsAffiliateCode))
        {
            apiResult.ErrorCode = -10;
            apiResult.Message = _selLangBll.GetMsg("msg_AffCodeNotFound");
            return false;
        }

        //如果從CreditSite註冊，且有輸入邀請碼(Referral)，則該用戶屬於邀請碼下線
        if (!string.IsNullOrEmpty(signupModel.RefId))
        {
            agent = _agentInfoService.GetAffiliateAgent($"{signupModel.RefId}_{signupModel.CurrencyId}", serverInfo.SiteId);

            if (agent?.CurrencyId != signupModel.CurrencyId)
            {
                apiResult.ErrorCode = -19;
                apiResult.Message = _selLangBll.GetMsg("mag_AffiliateNotExist");
                return false;
                /*agent Currency != CurrencyId*/
            }
        }
        else if (!string.IsNullOrEmpty(signupModel.AffId))
        {
            agent = _agentInfoService.GetAffiliateAgent(signupModel.AffId, serverInfo.SiteId);

            if (agent == null)
            {
                apiResult.ErrorCode = -19;
                apiResult.Message = _selLangBll.GetMsg("mag_AffiliateNotExist");
                return false;
            }
        }

        //tuk55註冊要帶銀行卡號相關資訊
        if (!string.IsNullOrEmpty(signupModel.AccountNo) && string.IsNullOrEmpty(signupModel.RealName) && signupModel.SysId != 0)
        {
            var bankInfoSysList = _bankInfoBll.GetBankInfoSysListByCurrencyForCtrl(signupModel.CurrencyId);
            var blockBankAccountList = _bankInfoBll.GetBlockBankAccount(signupModel.SysId, signupModel.AccountNo).ToList();
            var custBankDetail = _custInfoBll.GetCustBankDetail(signupModel.SysId, signupModel.AccountNo, serverInfo.SiteId).ToList();

            if (bankInfoSysList == null || !bankInfoSysList.Any(x => x.SysId == signupModel.SysId))
            {
                apiResult.ErrorCode = -20;
                return false;
            }

            if (blockBankAccountList.Count > 0)
            {
                apiResult.ErrorCode = -21;
                return false;
            }

            if (custBankDetail.Count > 0)
            {
                apiResult.ErrorCode = -22;
                apiResult.Message = _selLangBll.GetMsg("msg_BankAccountDuplicate");
                return false;
            }
        }

        agent ??= _agentInfoService.GetSuperAgent(serverInfo.SiteId, signupModel.CurrencyId);
        deviceType = (int)_tools.GetDeviceId(host);
        signupIp = _tools.GetClientIP();
        
        return true;
    }

    private void AfterSignUp(SiteController.SignupModel signupModel ,string fromUrl, string userAgent, int deviceType, int result , int custId ,ApiResult apiResult ,out Customer customer)
    {
        customer = null;
     
        switch (result)
        {
            case -5:
                apiResult.ErrorCode = result;
                apiResult.Message = _selLangBll.GetMsg("msg_UsernameNotAvailable");
                break;
            case -7:
                apiResult.ErrorCode = result;
                apiResult.Message = _selLangBll.GetMsg("msg_PromoCodeIncorrect");
                break;
            default:
                {
                    if (custId == 0)
                    {
                        apiResult.ErrorCode = result;
                        apiResult.Message = _selLangBll.GetMsg("msg_UsernameNotAvailable");
                    }
                    else if (result != 0)
                    {
                        apiResult.ErrorCode = result;
                        apiResult.Message = _selLangBll.GetMsg("msg_UsernameNotAvailable");
                    }
                    else
                    {
                        _ = Task.Run(async () =>
                        {
                            var serverInfo = _serverInfoBll.GetServerInfo();
                            if (serverInfo.SiteId == 5)
                            {
                                _commonBll.SendWelcomeMail(custId);
                                
                                // Signup Remove Iovation
                                //if (signupModel.blackbox != null)
                                //{
                                //    var apiResultIovation = await _iovationBll.IovationChecks(custId, signupModel.CurrencyId,
                                //        signupModel.UserName, signupModel.blackbox, IovationType.registration);
                                //    if (apiResultIovation.ErrorCode == 0 & apiResultIovation.Data != null)
                                //        _iovationBll.SetIovation(custId, IovationType.registration, apiResultIovation.Data);
                                //}
                            }
                        });
                        
                        var userLoginResult = _custInfoBll.UserLogin(false, 0, signupModel.UserName, signupModel.Userpwd,
                            _tools.GetClientIP(), fromUrl, userAgent, deviceType, ref customer, out var token);

                        switch (userLoginResult)
                        {
                            case -1:
                                apiResult.ErrorCode = 1101;
                                apiResult.Message = _selLangBll.GetMsg("msg_DepositProcessLogin_LoginFailed");
                                break;
                            case -2:
                                apiResult.ErrorCode = 1001;
                                apiResult.Message = _selLangBll.GetMsg("msg_DepositProcessLogin_LoginFailed");
                                break;
                            case -3:
                                apiResult.ErrorCode = 1102;
                                apiResult.Message = _selLangBll.GetMsg("msg_DepositProcessLogin_ACLocked");
                                break;
                            default:
                                {
                                    if (userLoginResult != 0)
                                    {
                                        _logger.LogError($"userLoginResult={userLoginResult}");
                                        apiResult.ErrorCode = 9000;
                                    }
                                    else
                                    {
                                        if (!string.IsNullOrWhiteSpace(signupModel.RealName))
                                        {
                                            _custInfoBll.SetCustBankInfo(custId, signupModel.RealName.Trim(), forceUseMainDb: true);
                                        }
                                        customer.Token = token;
                                        apiResult.ErrorCode = 0;
                                    }
                                    break;
                                }
                        }
                    }
                    break;
                }
        }
    }
    
    
}