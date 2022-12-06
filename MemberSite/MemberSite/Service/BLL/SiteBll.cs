using System.Text.RegularExpressions;
using MemberSite.Controllers.Api;
using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

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
    private readonly Regex _regexAffCode = new(@"^[0-9]{5,20}$");
    private readonly RecaptchaBLL _recaptcha;

    public SiteBll(IServiceProvider provider)
    {
        _custInfoBLL = provider.GetRequiredService<CustInfoBll>();
        _selLangBll = provider.GetRequiredService<SelLangBLL>();
        _iovationBll = provider.GetRequiredService<IovationBLL>();
        _commonBll = provider.GetRequiredService<CommonBLL>();
        _tools = provider.GetRequiredService<Tools>();
        _ShieldBll = provider.GetRequiredService<ShieldBLL>();
        _recaptcha = provider.GetRequiredService<RecaptchaBLL>();
    }

    public Customer Signup(SiteController.SignupModel signupModel, ApiResult apiResult, bool isAuthenticated,
        string host, string userAgent)
    {
        Customer customer = null;
        if (isAuthenticated ||
            (string.IsNullOrEmpty(signupModel.AreaCode) && !string.IsNullOrEmpty(signupModel.PhoneNo)))
        {
            apiResult.ErrorCode = 9000;
            return customer;
        }

        if (long.TryParse(signupModel.PhoneNo, out var phoneNo))
        {
            signupModel.PhoneNo = phoneNo.ToString();
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

        if (!string.IsNullOrEmpty(signupModel.ReCaptchaToken))
        {
            var verifyResult = _recaptcha.Verify(signupModel.ReCaptchaToken);
            if (!verifyResult.success)
            {
                apiResult.ErrorCode = -97;
                apiResult.Message = verifyResult.errorCodes.FirstOrDefault();
                return customer;
            }
        }

        if (!string.IsNullOrEmpty(signupModel.AffCode?.Trim()))
        {
            signupModel.AffCode = signupModel.AffCode.Trim();
            if (!_regexAffCode.IsMatch(signupModel.AffCode))
            {
                apiResult.ErrorCode = -9;
                apiResult.Message = _selLangBll.GetMsg("msg_AffCodeValidate");
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
        
        var result = _custInfoBLL.AddCustInfo(signupModel.UserName, signupModel.Userpwd, signupModel.Email,
            signupModel.CurrencyId, signupModel.UserLang, signupModel.RefId, signupModel.AffId, signupModel.Aff,
            signupModel.AffCode, signupModel.LineUserId, deviceType , out var custId, signupModel.SignupUrl, signupModel.ReferralUrl, signupModel.PhoneNo,
            signupModel.AreaCode, signupModel.RealName, signupModel.PromoCode, signupModel.Birth_d,
            signupModel.Birth_m,
            signupModel.Birth_y);

        switch (result)
        {
            case -1:
                apiResult.ErrorCode = result;
                apiResult.Message = _selLangBll.GetMsg("err_ThisCurrencyNotSupport");
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

                        if (signupModel.blackbox != null)
                        {
                            var apiResultIovation = await _iovationBll.IovationChecks(custId, signupModel.CurrencyId,
                                signupModel.UserName, signupModel.blackbox, IovationType.registration);
                            if (apiResultIovation.ErrorCode == 0 & apiResultIovation.Data != null)
                                _iovationBll.SetIovation(custId, IovationType.registration, apiResultIovation.Data);
                        }
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
                                        if (!string.IsNullOrEmpty(signupModel.RealName))
                                        {
                                            _custInfoBLL.SetCustBankInfo(custId, signupModel.RealName, forceUseMainDb: true);
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
