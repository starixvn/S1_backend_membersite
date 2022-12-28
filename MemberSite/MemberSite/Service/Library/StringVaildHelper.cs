using System.Text.RegularExpressions;

namespace MemberSite.Service.Library;

public class StringVaildHelper
{
    public bool VaildAccountNameForSetProfile(string accountName)
    {
        string patternStrict = @"^[^\d\-!@#$%^&_\""'()+,;<>=|?\[\]\\`~{}]+$";
        return Regex.IsMatch(accountName, patternStrict, RegexOptions.Compiled);
    }
}
