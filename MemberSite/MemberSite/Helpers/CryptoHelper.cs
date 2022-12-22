using System.Text;
using System.Security.Cryptography;

namespace MemberSite.Helpers;

public class CryptoHelper
{
    public string SHA256(string input)
    {
        var inputBytes = Encoding.UTF8.GetBytes(input);
        var outputBytes = new SHA256CryptoServiceProvider().ComputeHash(inputBytes);
        return BitConverter.ToString(outputBytes).Replace("-", "").ToLower();
    }
}
