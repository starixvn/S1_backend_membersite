using MemberSite.Controllers;
using MemberSite.Service.DAL;
using MemberSite.Service.Library;
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
        private readonly string _publicApiAuthToken;

        public AuthBLL(IServiceProvider provider)
        {
            this.authService = provider.GetRequiredService<AuthService>();
            _cryptoHelper = provider.GetRequiredService<CryptoHelper>();
            _publicApiAuthToken = provider.GetRequiredService<IConfiguration>().GetValue<string>("PublicApiAuthToken");
            _logger = provider.GetRequiredService<ILogger<AuthBLL>>();
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
            if (string.IsNullOrWhiteSpace(content))
            {
                _logger.LogError("Request vaild fail：Content is null.");
                return false;
            }

            if (!ConvertToJObject(content, out JObject jbody))
            {
                return false;
            }

            if (!jbody.ContainsKey("timestamp") 
                || !jbody.ContainsKey("token"))
            {
                _logger.LogError("Request vaild fail：Lost Key (timestamp and token).");
                return false;
            }

            if (!long.TryParse(jbody["timestamp"].ToString(), out long timestamp))
            {
                _logger.LogError("Request vaild fail：Convert timestamp fail.");
                return false;
            }
            if (!CheckTimestamp(timestamp))
            {
                _logger.LogError("Request vaild fail：Check Timestamp fail.");
                return false;
            }

            var token = jbody["token"].ToString();
            var requestStr = content.Replace(token, _publicApiAuthToken);
            if (token != _cryptoHelper.SHA256(requestStr))
            {
                _logger.LogError("Request vaild fail：Check crypto token fail.");
                return false;
            }
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
