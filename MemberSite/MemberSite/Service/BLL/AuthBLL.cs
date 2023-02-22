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
        private readonly string _publicApiPassToken;
        private readonly IHostEnvironment _hostEnvironment;

        public AuthBLL(IServiceProvider provider)
        {
            this.authService = provider.GetRequiredService<AuthService>();
            _cryptoHelper = provider.GetRequiredService<CryptoHelper>();
            _publicApiAuthToken = provider.GetRequiredService<IConfiguration>().GetValue<string>("PublicApiAuthToken");
            _publicApiPassToken = provider.GetRequiredService<IConfiguration>().GetValue<string>("PublicApiPassToken");
            _logger = provider.GetRequiredService<ILogger<AuthBLL>>();
            _hostEnvironment = provider.GetRequiredService<IHostEnvironment>();
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

        public bool RequestAuth(string fromUrl, string content)
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

            #region Development Pass Token
            if (_hostEnvironment.IsDevelopment())
            {
                // 暫不檢查是否有帶token
                //if (!jbody.ContainsKey("token"))
                //{
                //    _logger.LogError("Request vaild fail：Lost Key (token).");
                //    return false;
                //}

                if (jbody.ContainsKey("token"))
                    if (jbody["token"].ToString() == _publicApiPassToken)
                        return true;
            }

            //https://appstag.n88dev.com這組domain判讀不到dev環境，先讓appteam可以測試
            if (fromUrl.Contains("appstag"))
            {
                // 暫不檢查是否有帶token
                //if (!jbody.ContainsKey("token"))
                //{
                //    _logger.LogError("Request vaild fail：Lost Key (token).");
                //    return false;
                //}

                if (jbody.ContainsKey("token"))

                    if (jbody["token"].ToString() == "1c5a0a1684a94ddcf1602cce0a6426460cace6892cb236cb920b8ccdcfbfb3f7")
                        return true;
            }
            #endregion

            //暫不檢查是否有帶token、timestamp
            //if (!jbody.ContainsKey("timestamp")
            //    || !jbody.ContainsKey("token"))
            //{
            //    _logger.LogError("Request vaild fail：Lost Key (timestamp and token).");
            //    return false;
            //}

            //如果有timestamp，則檢查
            if (jbody.ContainsKey("timestamp"))
            {
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
            }

            //如果有token，則檢查
            if (jbody.ContainsKey("token"))
            {
                var token = jbody["token"].ToString();
                var requestStr = content.Replace(token, _publicApiAuthToken);
                if (token != _cryptoHelper.SHA256(requestStr))
                {
                    _logger.LogError($"Request vaild fail：Check crypto token fail. request.token={token}. correct token={_cryptoHelper.SHA256(requestStr)}. plaintext={requestStr}.");
                    return false;
                }
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
            //return true; //For Debug
            //30 秒內要收到request並驗證
            if (((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds() * 1000 - timestamp < 30 * 1000)
                return true;
            else
                return false;
        }
    }
}
