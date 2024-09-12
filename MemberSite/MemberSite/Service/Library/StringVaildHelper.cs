using System.Text.RegularExpressions;

namespace MemberSite.Service.Library;

public class StringVaildHelper
{
    private readonly Regex _regexAccountName = new(@"^[^\d\-!@#$%^&_\""()+;~～<>=|?\[\]\\`~{}\p{Cs}]+$");
    private readonly Regex _regexPhoneNumber = new(@"^\+60[0-9]{9,10}$|\+62[0-9]{10,13}$|\+86[0-9]{10,11}$|\+91[0-9]{10}$|\+66[0-9]{9}$|\+880[0-9]{10}$|\+855[0-9]{8,10}$|\+84[0-9]{9}|\+82[0-9]{9,10}|\+81[0-9]{10}|\+55[0-9]{10,11}$");
    private readonly Regex _regexUserName = new(@"^[a-zA-Z0-9]{6,18}$");
    private readonly Regex _regexEmail = new(@"^\w+((-\w+)|(\.\w+))*@[A-Za-z0-9]+((\.|-)[A-Za-z0-9]+)*\.[A-Za-z]+$");
    
    public bool VaildAccountNameForSetProfile(string accountName)
    {
        var result = _regexAccountName.IsMatch(accountName);
        return result;
    }
    
    public bool VaildAreaCodePhoneNo(string areaCode, string phoneNo)
    {
        var result = _regexPhoneNumber.IsMatch(areaCode + phoneNo);
        return result;
    }
    
    public bool VaildEmail(string email)
    {
        var result = _regexEmail.IsMatch(email);
        return result;
    }
    
    public bool VaildUserName(string userName)
    {
        var result = _regexUserName.IsMatch(userName);
        return result;
    }
}
