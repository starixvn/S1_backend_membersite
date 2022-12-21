using MemberSite.Service.DAL;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Security.Cryptography;

namespace MemberSite.Service.BLL
{
    public class AuthBLL
    {
        private readonly AuthService authService;

        public AuthBLL(AuthService authService)
        {
            this.authService = authService;
        }

        public bool Authenticate<T>(string sToken, out T t)
        {
            var sPermissionData = "";
            t = default;
            sPermissionData = authService.Authenticate(sToken);

            if (!string.IsNullOrEmpty(sPermissionData))
            {
                t = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(sPermissionData);
                return true;
            }

            return false;
        }

        public bool RequestAuth(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return false;
            var jbody = JObject.Parse(content);
            var timestamp = long.Parse(jbody["Timestamp"].ToString());
            if (!CheckTimestamp(timestamp)) return false;

            var token = jbody["Token"].ToString();
            var requestStr = content.Replace(token, "1qaz2wsx");
            if (token == SHA256(requestStr))
                return true;
            else
                return false;
        }

        private bool CheckTimestamp(long timestamp)
        {
            //30 秒內要收到request並驗證
            if (((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds() - timestamp < 30)
                return true;
            else
                return false;
        }

        private string SHA256(string input)
        {
            var inputBytes = Encoding.UTF8.GetBytes(input);
            var outputBytes = new SHA256CryptoServiceProvider().ComputeHash(inputBytes);
            return BitConverter.ToString(outputBytes).Replace("-", "").ToLower();
        }
    }
}
