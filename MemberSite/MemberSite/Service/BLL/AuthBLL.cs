using MemberSite.Controllers;
using MemberSite.Helpers;
using MemberSite.Service.DAL;
using MemberSite.Service.Model;
using Newtonsoft.Json.Linq;
using System.Text;

namespace MemberSite.Service.BLL
{
    public class AuthBLL
    {
        private readonly AuthService authService;
        private readonly CryptoHelper _cryptoHelper;
        private readonly ILogger<AuthBLL> _logger;

        public AuthBLL(AuthService authService, CryptoHelper cryptoHelper, ILogger<AuthBLL> logger)
        {
            this.authService = authService;
            _cryptoHelper = cryptoHelper;
            _logger = logger;
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

            if (!ConvertToJObject(content, out JObject jbody)) return false;

            if (jbody["Timestamp"] == null) return false;

            long.TryParse(jbody["Timestamp"].ToString(), out long timestamp);
            if (!CheckTimestamp(timestamp)) return false;

            if (jbody["Token"] == null) return false;

            var token = jbody["Token"].ToString();
            var requestStr = content.Replace(token, "1qaz2wsx");
            if (token != _cryptoHelper.SHA256(requestStr)) return false;

            return true;
        }

        private bool ConvertToJObject(string content, out JObject jObject)
        {
            jObject = null;
            try
            {
                jObject = JObject.Parse(content);
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message, "Unable to parse jsonObject.");
                return false;
            }
        }

        private bool CheckTimestamp(long timestamp)
        {
            //30 秒內要收到request並驗證
            if (((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds() - timestamp < 30)
                return true;
            else
                return false;
        }
    }
}
