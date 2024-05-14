using System.Text.RegularExpressions;

using MemberSite.Controllers.Api;
using MemberSite.Service.Domain;
using MemberSite.Service.Library;
using MemberSite.Service.Model;

namespace MemberSite.Service.BLL;

public class SiteBll
{
    private readonly CustInfoBll _custInfoBLL;
    private readonly SelLangBLL _selLangBll;
    private readonly IovationBLL _iovationBll;
    private readonly CommonBLL _commonBll;
    private readonly Tools _tools;
    private readonly ShieldBLL _ShieldBll;
    private readonly Regex _regexUserName = new(@"^[a-zA-Z0-9]{6,18}$");
    private readonly Regex _regexPhoneNumber = new(@"^\+60[0-9]{9,10}$|\+62[0-9]{10,13}$|\+86[0-9]{10,11}$|\+91[0-9]{10}$|\+66[0-9]{9}$|\+880[0-9]{10}$|\+855[0-9]{8,10}$|\+84[0-9]{9}$");
    private readonly Regex _regexEmail = new(@"^\w+((-\w+)|(\.\w+))*@[A-Za-z0-9]+((\.|-)[A-Za-z0-9]+)*\.[A-Za-z]+$");
    private readonly Regex _regexPhoneNo = new(@"^\d+$");
    private readonly RecaptchaBLL _recaptcha;
    private readonly ServerInfoBll _serverInfoBll;
    private readonly ILogger<SiteBll> _logger;

    public SiteBll(IServiceProvider provider)
    {
        _custInfoBLL = provider.GetRequiredService<CustInfoBll>();
        _selLangBll = provider.GetRequiredService<SelLangBLL>();
        _iovationBll = provider.GetRequiredService<IovationBLL>();
        _commonBll = provider.GetRequiredService<CommonBLL>();
        _tools = provider.GetRequiredService<Tools>();
        _ShieldBll = provider.GetRequiredService<ShieldBLL>();
        _recaptcha = provider.GetRequiredService<RecaptchaBLL>();
        _serverInfoBll = provider.GetRequiredService<ServerInfoBll>();
        _logger = provider.GetRequiredService<ILogger<SiteBll>>();
    }

    public Customer Signup(SiteController.SignupModel signupModel, ApiResult apiResult, bool isAuthenticated,
        string host, string userAgent, bool checkReCaptchaToken)
    {
        Customer customer = null;
        if (isAuthenticated ||
            (string.IsNullOrEmpty(signupModel.AreaCode) && !string.IsNullOrEmpty(signupModel.PhoneNo)))
        {
            _logger.LogError($"isAuthenticated={isAuthenticated} AreaCode={signupModel.AreaCode} PhoneNo={signupModel.PhoneNo}");
            apiResult.ErrorCode = 9000;
            return customer;
        }

        var serverInfo = _serverInfoBll.GetServerInfo();
        if (serverInfo.SiteId == 1)
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
                            _ => ""
                        };
                        apiResult.ErrorCode = -20;
                        apiResult.Message = string.Format(_selLangBll.GetMsg("msg_PhoneFormatVerify"), message);
                        return customer;
                    }
                    break;
            }

            //if (!_regexEmail.IsMatch(signupModel.Email))
            //{
            //    apiResult.ErrorCode = -10;
            //    apiResult.Message = _selLangBll.GetMsg("msg_EmailFormatVerify");
            //    return customer;
            //}
        }

        if (checkReCaptchaToken && !string.IsNullOrEmpty(signupModel.ReCaptchaToken))
        {
            var verifyResult = _recaptcha.Verify(signupModel.ReCaptchaToken);
            if (!verifyResult.success)
            {
                apiResult.ErrorCode = -97;
                apiResult.Message = verifyResult.errorCodes.FirstOrDefault();
                return customer;
            }
        }

        if (_regexPhoneNo.IsMatch(signupModel.PhoneNo))
        {
            if (_custInfoBLL.ExistsPhoneNo(0, signupModel.AreaCode, signupModel.PhoneNo))
            {
                apiResult.ErrorCode = -6;
                apiResult.Message = $"{_selLangBll.GetResourceValue("lbl_Phone")} {_selLangBll.GetMsg("msg_HasExist")}";
                return customer;
            }
        }
        else if (!string.IsNullOrEmpty(signupModel.PhoneNo))
        {
            apiResult.ErrorCode = -20;
            apiResult.Message = $"{_selLangBll.GetResourceValue("lbl_Phone")} {_selLangBll.GetMsg("msg_PhoneWrongFormat")}";
            return customer;
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

        if (!_regexUserName.IsMatch(signupModel.UserName))
        {
            apiResult.ErrorCode = -8;
            apiResult.Message = _selLangBll.GetMsg("msg_UsernameValidate");
            return customer;
        }

        AffCodeObject affCodeObj = null;
        if (!string.IsNullOrWhiteSpace(signupModel.AffCode))
        {
            if (signupModel.AffCode.Trim().Length == 5)
            {
                affCodeObj = new AffCodeObject(signupModel.AffCode);
                if (!affCodeObj.IsValid())
                {
                    apiResult.ErrorCode = -9;
                    apiResult.Message = _selLangBll.GetMsg("msg_AffCodeValidate");
                    return customer;
                }
            }
            else if (signupModel.AffCode.Trim().Length == 8)
            {
                var rffCodeObj = new ReferralObject(signupModel.AffCode);
                if (!rffCodeObj.IsValid())
                {
                    apiResult.ErrorCode = -9;
                    apiResult.Message = _selLangBll.GetMsg("msg_AffCodeValidate");
                    return customer;
                }
                if (rffCodeObj.IsExists(_custInfoBLL.GetAffiliateAgentBll) < 0)
                {
                    apiResult.ErrorCode = -10;
                    apiResult.Message = _selLangBll.GetMsg("msg_AffCodeNotFound");
                    return customer;
                }
                if (rffCodeObj.CurrencyId != signupModel.CurrencyId)
                {
                    apiResult.ErrorCode = -10;
                    apiResult.Message = _selLangBll.GetMsg("msg_AffCodeNotFound");
                    return customer;
                }
                signupModel.AffId = signupModel.AffCode;
                signupModel.AffCode = null;
            }
            else
            {
                apiResult.ErrorCode = -10;
                apiResult.Message = _selLangBll.GetMsg("msg_AffCodeNotFound");
                return customer;
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

        var fromUrl = _tools.GetFromUrl();
        var deviceType = (int)_tools.GetDeviceId(fromUrl);
        var signupIp = _tools.GetClientIP();

        var result = _custInfoBLL.AddCustInfo(signupModel.UserName, signupModel.Userpwd, signupModel.Email,
            signupModel.CurrencyId, signupModel.UserLang, signupModel.RefId, signupModel.AffId, signupModel.Aff,
            affCodeObj, signupModel.LineUserId, deviceType, out var custId, signupModel.SignupUrl, signupModel.ReferralUrl, signupModel.PhoneNo,
            signupModel.AreaCode, signupModel.RealName, signupModel.PromoCode, signupModel.Birth_d,
            signupModel.Birth_m,
            signupModel.Birth_y, signupIp);

        switch (result)
        {
            case -1:
                apiResult.ErrorCode = result;
                apiResult.Message = _selLangBll.GetMsg("msg_ThisCurrencyNotSupport");
                break;
            case -2:
                apiResult.ErrorCode = result;
                apiResult.Message = _selLangBll.GetMsg("msg_UsernameValidate");
                break;
            case -3:
                apiResult.ErrorCode = result;
                apiResult.Message = _selLangBll.GetMsg("msg_PasswordValidate");
                break;
            case -4:
                apiResult.ErrorCode = result; /**/
                apiResult.Message = "";
                break;
            case -5:
                apiResult.ErrorCode = result;
                apiResult.Message = _selLangBll.GetMsg("msg_UsernameNotAvailable");
                break;
            case -7:
                apiResult.ErrorCode = result;
                apiResult.Message = _selLangBll.GetMsg("msg_PromoCodeIncorrect");
                break;
            case -10:
                apiResult.ErrorCode = result;
                apiResult.Message = _selLangBll.GetMsg("msg_AffCodeNotFound");
                break;
            case -19:
                apiResult.ErrorCode = result;
                apiResult.Message = _selLangBll.GetMsg("mag_AffiliateNotExist");
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
                        //註冊的時候改成不填email

                        //_ = Task.Run(async () =>
                        //{
                        //    _commonBll.SendWelcomeMail(custId);

                        //    // Signup Remove Iovation
                        //    //if (signupModel.blackbox != null)
                        //    //{
                        //    //    var apiResultIovation = await _iovationBll.IovationChecks(custId, signupModel.CurrencyId,
                        //    //        signupModel.UserName, signupModel.blackbox, IovationType.registration);
                        //    //    if (apiResultIovation.ErrorCode == 0 & apiResultIovation.Data != null)
                        //    //        _iovationBll.SetIovation(custId, IovationType.registration, apiResultIovation.Data);
                        //    //}
                        //});

                        var userLoginResult = _custInfoBLL.UserLogin(false, 0, signupModel.UserName, signupModel.Userpwd,
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
                                            _custInfoBLL.SetCustBankInfo(custId, signupModel.RealName.Trim(), forceUseMainDb: true);
                                        }

                                        customer.Token = token;

                                        apiResult.ErrorCode = 0;
                                        return customer;
                                    }

                                    break;
                                }
                        }
                    }

                    break;
                }
        }

        return customer;
    }

    //只給Ivorysport使用
    public Customer IvorysportSignup(SiteController.IvorysportSignupModel signupModel, ApiResult apiResult, bool isAuthenticated,
        string host, string userAgent, bool checkReCaptchaToken)
    {
        Customer customer = null;
        if (isAuthenticated ||
            (string.IsNullOrEmpty(signupModel.AreaCode) && !string.IsNullOrEmpty(signupModel.PhoneNo)))
        {
            apiResult.ErrorCode = 9000;
            return customer;
        }

        var serverInfo = _serverInfoBll.GetServerInfo();

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
                        _ => ""
                    };
                    apiResult.ErrorCode = -20;
                    apiResult.Message = string.Format(_selLangBll.GetMsg("msg_PhoneFormatVerify"), message);
                    return customer;
                }
                break;
        }

        if (string.IsNullOrWhiteSpace(signupModel.Nationality))
        {
            apiResult.ErrorCode = -30;
            apiResult.Message = _selLangBll.GetMsg("msg_NeedNationality");
            return customer;
        }

        if (string.IsNullOrWhiteSpace(signupModel.Country) || string.IsNullOrWhiteSpace(signupModel.City) ||
            string.IsNullOrWhiteSpace(signupModel.PostCode) || string.IsNullOrWhiteSpace(signupModel.LineFirst))
        {
            apiResult.ErrorCode = -31;
            apiResult.Message = _selLangBll.GetMsg("msg_NeedAddress");
            return customer;
        }

        if (!_regexEmail.IsMatch(signupModel.Email))
        {
            apiResult.ErrorCode = -10;
            apiResult.Message = _selLangBll.GetMsg("msg_EmailFormatVerify");
            return customer;
        }

        if (checkReCaptchaToken && !string.IsNullOrEmpty(signupModel.ReCaptchaToken))
        {
            var verifyResult = _recaptcha.Verify(signupModel.ReCaptchaToken);
            if (!verifyResult.success)
            {
                apiResult.ErrorCode = -97;
                apiResult.Message = verifyResult.errorCodes.FirstOrDefault();
                return customer;
            }
        }
        
        if(_regexPhoneNo.IsMatch(signupModel.PhoneNo))
        {
            if (_custInfoBLL.ExistsPhoneNo(0, signupModel.AreaCode, signupModel.PhoneNo))
            {
                apiResult.ErrorCode = -6;
                apiResult.Message = $"{_selLangBll.GetResourceValue("lbl_Phone")} {_selLangBll.GetMsg("msg_HasExist")}";
                return customer;
            }
        }
        else if (!string.IsNullOrEmpty(signupModel.PhoneNo))
        {
            apiResult.ErrorCode = -20;
            apiResult.Message = $"{_selLangBll.GetResourceValue("lbl_Phone")} {_selLangBll.GetMsg("msg_PhoneWrongFormat")}";
            return customer;
        }

        if (!string.IsNullOrWhiteSpace(signupModel.Email))
        {
            if (_custInfoBLL.ExistsEmail(0, signupModel.Email))
            {
                apiResult.ErrorCode = -10;
                apiResult.Message = $"{_selLangBll.GetResourceValue("lbl_Email")} {_selLangBll.GetMsg("msg_HasExist")}";
                return customer;
            }
        }

        if (!_regexUserName.IsMatch(signupModel.UserName))
        {
            apiResult.ErrorCode = -8;
            apiResult.Message = _selLangBll.GetMsg("msg_UsernameValidate");
            return customer;
        }

        AffCodeObject affCodeObj = null;

        if (string.IsNullOrEmpty(signupModel.SignupUrl))
        {
            signupModel.SignupUrl = host;
        }

        var fromUrl = _tools.GetFromUrl();
        var deviceType = (int)_tools.GetDeviceId(fromUrl);
        var signupIp = _tools.GetClientIP();

        var result = _custInfoBLL.IvorysportAddCustInfo(signupModel.UserName, signupModel.Userpwd, signupModel.Email,
            signupModel.CurrencyId, signupModel.UserLang, signupModel.RefId, signupModel.AffId, signupModel.Aff,
            affCodeObj, signupModel.LineUserId, deviceType, signupModel.PhotoId, out var custId, signupModel.SignupUrl, signupModel.ReferralUrl, signupModel.PhoneNo,
            signupModel.AreaCode, signupModel.RealName, signupModel.PromoCode, signupModel.Birth_d,
            signupModel.Birth_m,
            signupModel.Birth_y, signupIp, signupModel.Nationality, signupModel.Occupation, signupModel.SourceOfIncome, signupModel.Country, signupModel.City, signupModel.PostCode, signupModel.LineFirst);

        switch (result)
        {
            case -1:
                apiResult.ErrorCode = result;
                apiResult.Message = _selLangBll.GetMsg("msg_ThisCurrencyNotSupport");
                break;
            case -2:
                apiResult.ErrorCode = result;
                apiResult.Message = _selLangBll.GetMsg("msg_UsernameValidate");
                break;
            case -3:
                apiResult.ErrorCode = result;
                apiResult.Message = _selLangBll.GetMsg("msg_PasswordValidate");
                break;
            case -4:
                apiResult.ErrorCode = result; /**/
                apiResult.Message = "";
                break;
            case -5:
                apiResult.ErrorCode = result;
                apiResult.Message = _selLangBll.GetMsg("msg_UsernameNotAvailable");
                break;
            case -7:
                apiResult.ErrorCode = result;
                apiResult.Message = _selLangBll.GetMsg("msg_PromoCodeIncorrect");
                break;
            case -10:
                apiResult.ErrorCode = result;
                apiResult.Message = _selLangBll.GetMsg("msg_AffCodeNotFound");
                break;
            case -19:
                apiResult.ErrorCode = result;
                apiResult.Message = _selLangBll.GetMsg("mag_AffiliateNotExist");
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
                            _commonBll.SendWelcomeMail(custId);

                            // Signup Remove Iovation
                            //if (signupModel.blackbox != null)
                            //{
                            //    var apiResultIovation = await _iovationBll.IovationChecks(custId, signupModel.CurrencyId,
                            //        signupModel.UserName, signupModel.blackbox, IovationType.registration);
                            //    if (apiResultIovation.ErrorCode == 0 & apiResultIovation.Data != null)
                            //        _iovationBll.SetIovation(custId, IovationType.registration, apiResultIovation.Data);
                            //}
                        });

                        var userLoginResult = _custInfoBLL.UserLogin(false, 0, signupModel.UserName, signupModel.Userpwd,
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
                                        apiResult.ErrorCode = 9000;
                                    }
                                    else
                                    {
                                        if (!string.IsNullOrWhiteSpace(signupModel.RealName))
                                        {
                                            _custInfoBLL.SetCustBankInfo(custId, signupModel.RealName.Trim(), forceUseMainDb: true);
                                        }

                                        customer.Token = token;

                                        apiResult.ErrorCode = 0;
                                        return customer;
                                    }

                                    break;
                                }
                        }
                    }

                    break;
                }
        }

        return customer;
    }
}