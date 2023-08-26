using System.Security.Cryptography;
using System.Text;

namespace MemberSite.Service.Library;

public class CryptoHelper
{
    public string SHA256(string input)
    {
        var inputBytes = Encoding.UTF8.GetBytes(input);
        var outputBytes = new SHA256CryptoServiceProvider().ComputeHash(inputBytes);
        return BitConverter.ToString(outputBytes).Replace("-", "").ToLower();
    }
}
