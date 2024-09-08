using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

using MemberSite.Exceptions;
using MemberSite.Service.BLL;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

using Microsoft.IdentityModel.Tokens;

namespace MemberSite.Service.Library
{
    public class Tools
    {
        private readonly string _backEndKey;
        private readonly JwtConfig jwtConfig;
        private readonly Dictionary<string, string> _loginDeviceConfig;
        private readonly Regex _regexPassword = new(@"^(?=.{6,15}$)[a-zA-Z0-9!@#$%^&*]*$", RegexOptions.Singleline);
        public IHttpContextAccessor httpContextAccessor { get; }

        public Tools(IServiceProvider provider)
        {
            httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();

            var configBll = provider.GetRequiredService<ConfigBLL>();
            _backEndKey = configBll.GetConfigValue("ApiPaymentGateWayKey");
            jwtConfig = configBll.GetConfigValueByEnvironment<JwtConfig>("JwtSettings");
            _loginDeviceConfig = configBll.GetConfigValueByEnvironment<Dictionary<string, string>>("LoginDeviceConfig");
        }

        /// <summary>
        /// MD5加密
        /// </summary>
        /// <param name="pass"></param>
        /// <returns></returns>
        public string MD5Encrypt(string pass)
        {
            var md5Hasher = new MD5CryptoServiceProvider();
            var data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(pass));
            var sBuilder = new StringBuilder();
            for (var i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }
            return sBuilder.ToString();
        }

        /// <summary>
        /// 验证用户名 必须是字母和数字的组合
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        public bool CheckUserName(string userName)
        {
            if (string.IsNullOrEmpty(userName) || userName.Length < 6 || userName.Length > 18)
                return false;

            var regex = new Regex("^[a-zA-Z0-9]*$", RegexOptions.Singleline);
            var mPwd = regex.Match(userName);

            return mPwd.Success;
        }

        /// <summary>
        /// 驗證用戶密碼: Nova88 / Gaol / Bwing 支援6~15碼大小寫英文數字跟特殊符號!@#$%^&*
        /// </summary>
        /// <param name="userPwd"></param>
        /// <param name="siteId"></param>
        /// <returns></returns>
        public bool CheckUserInputPassword(string userPwd)
        {
            if (string.IsNullOrEmpty(userPwd))
                return false;

            var result = _regexPassword.Match(userPwd);
            return result.Success;
        }

        public string GetToken()
        {
            var g = Guid.NewGuid();
            var GuidString = Convert.ToBase64String(g.ToByteArray());
            GuidString = GuidString.Replace("=", "");
            GuidString = GuidString.Replace("+", "");

            return GuidString;
        }

        #region IP

        public uint GetIpToLong(string ipAddress)
        {
            if (ipAddress.IndexOf(".", StringComparison.Ordinal) > 0)
            {
                try
                {
                    var b = IPAddress.Parse(ipAddress).GetAddressBytes();
                    Array.Reverse(b);
                    return BitConverter.ToUInt32(b, 0);
                }
                catch (Exception e)
                {
                    throw new IPFormatException("ipv4 format error");
                }
            }
            else if (ipAddress.IndexOf(":", StringComparison.Ordinal) > 0)
            {
                throw new IPFormatException("no support ipv6");
            }
            else
            {
                throw new IPFormatException("ip format error");
            }
        }

        /// <summary>
        /// 获取客户端IP
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public string GetClientIP()
        {
            return GetClientIP(httpContextAccessor.HttpContext);
        }
        public string GetClientIP(HttpContext httpContext)
        {
            var HeaderNames = new string[] { "X-Client-IP", "X-Forwarded-For", "HTTP_X_FORWARDED_FOR", "HTTP_INCAP_CLIENT_IP", "HTTP_TRUE_CLIENT_IP", "HTTP_X_REAL_IP", "REMOTE_ADDR" };

            foreach (var Header in HeaderNames)
            {

                if (httpContext.Request.Headers.ContainsKey(Header))
                {
                    string sServerVariables = httpContext.Request.Headers[Header];
                    var ip = GetIPAddress(sServerVariables.Split(','));
                    if (!string.IsNullOrEmpty(ip))
                    {
                        return ip;
                    }
                }
            }

            return httpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();
        }
        private string GetIPAddress(string[] IPArry)
        {
            foreach (var ip in IPArry)
            {
                var IPAddress = ip.Trim();
                if (IsIPAddress(IPAddress)
                    && !isInnerIP(IPAddress)) return IPAddress.Trim();
            }

            return null;
        }
        private bool isInnerIP(string ipAddress)
        {
            var ipNum = getIpNum(ipAddress);

            long aBegin = 167772160;// getIpNum("10.0.0.0");
            long aEnd = 184549375;// getIpNum("10.255.255.255");
            long bBegin = 2886729728; //getIpNum("172.16.0.0");
            long bEnd = 2887778303; //getIpNum("172.31.255.255");
            long cBegin = 3232235520; //getIpNum("192.168.0.0");
            long cEnd = 3232301055; //getIpNum("192.168.255.255");

            // Logger.Log("aBegin:" + aBegin + ",aEnd:" + aEnd + ",bBegin:" + bBegin + ",bEnd:" + bEnd + ",cBegin:" + cBegin + ",cEnd:" + cEnd);

            var isInnerIp = isInner(ipNum, aBegin, aEnd) || isInner(ipNum, bBegin, bEnd) || isInner(ipNum, cBegin, cEnd) || ipAddress == "127.0.0.1";
            return isInnerIp;
        }
        private long getIpNum(string ipAddress)
        {
            var ip = ipAddress.Split('.');
            long a = Convert.ToInt32(ip[0]);
            long b = Convert.ToInt32(ip[1]);
            long c = Convert.ToInt32(ip[2]);
            long d = Convert.ToInt32(ip[3]);

            var ipNum = a * 256 * 256 * 256 + b * 256 * 256 + c * 256 + d;
            return ipNum;
        }
        private bool IsIPAddress(string str1)
        {
            if (string.IsNullOrEmpty(str1) || str1.Length < 7 || str1.Length > 15) return false;
            const string regformat = @"^\d{1,3}[\.]\d{1,3}[\.]\d{1,3}[\.]\d{1,3}$";

            var regex = new Regex(regformat, RegexOptions.IgnoreCase);
            return regex.IsMatch(str1);

        }
        private bool isInner(long userIp, long begin, long end)
        {
            return (userIp >= begin) && (userIp <= end);
        }

        #endregion

        public string GetFromUrl()
        {
            var fromUrl = "";
            // 先拿Header的Host
            if (string.IsNullOrWhiteSpace(fromUrl) && httpContextAccessor.HttpContext.Request.Headers.TryGetValue("Host", out var headerHost))
            {
                fromUrl = new Uri(headerHost).Host;
            }

            // 先拿Header的origin-host
            if (string.IsNullOrWhiteSpace(fromUrl) && httpContextAccessor.HttpContext.Request.Headers.TryGetValue("origin-host", out var originHost))
            {
                fromUrl = new Uri(originHost).Host;
            }

            // 先拿Header的ai-host
            if (string.IsNullOrWhiteSpace(fromUrl) && httpContextAccessor.HttpContext.Request.Headers.TryGetValue("ai-host", out var aiHost))
            {
                fromUrl = new Uri(aiHost).Host;
            }

            if (string.IsNullOrWhiteSpace(fromUrl) && httpContextAccessor.HttpContext.Request.Headers.TryGetValue("Referer", out var referer))
            {
                fromUrl = new Uri(referer).Host;
            }

            if (string.IsNullOrWhiteSpace(fromUrl) || fromUrl.Contains("localhost"))
            {
                fromUrl = httpContextAccessor.HttpContext.Request.Host.Host;
            }

            return fromUrl;
        }


        /// <summary>
        /// 保留两位小数的金额
        /// </summary>
        /// <param name="d"></param>
        /// <param name="decimals"></param>
        /// <returns></returns>
        public decimal Floor(decimal d, int decimals)
        {
            var value = (long)Math.Pow(10.0, decimals);
            return Convert.ToDecimal(Math.Floor(d * (decimal)value) / (decimal)value);
        }

        public bool IsMobile()
        {

            if (httpContextAccessor.HttpContext.Request.Headers.ContainsKey("User-Agent"))
            {
                string u = httpContextAccessor.HttpContext.Request.Headers["User-Agent"];
                var b = new Regex(@"(android|bb\d+|meego).+mobile|avantgo|bada\/|blackberry|blazer|compal|elaine|fennec|hiptop|iemobile|ip(hone|od)|iris|kindle|lge |maemo|midp|mmp|mobile.+firefox|netfront|opera m(ob|in)i|palm( os)?|phone|p(ixi|re)\/|plucker|pocket|psp|series(4|6)0|symbian|treo|up\.(browser|link)|vodafone|wap|windows ce|xda|xiino", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                var v = new Regex(@"1207|6310|6590|3gso|4thp|50[1-6]i|770s|802s|a wa|abac|ac(er|oo|s\-)|ai(ko|rn)|al(av|ca|co)|amoi|an(ex|ny|yw)|aptu|ar(ch|go)|as(te|us)|attw|au(di|\-m|r |s )|avan|be(ck|ll|nq)|bi(lb|rd)|bl(ac|az)|br(e|v)w|bumb|bw\-(n|u)|c55\/|capi|ccwa|cdm\-|cell|chtm|cldc|cmd\-|co(mp|nd)|craw|da(it|ll|ng)|dbte|dc\-s|devi|dica|dmob|do(c|p)o|ds(12|\-d)|el(49|ai)|em(l2|ul)|er(ic|k0)|esl8|ez([4-7]0|os|wa|ze)|fetc|fly(\-|_)|g1 u|g560|gene|gf\-5|g\-mo|go(\.w|od)|gr(ad|un)|haie|hcit|hd\-(m|p|t)|hei\-|hi(pt|ta)|hp( i|ip)|hs\-c|ht(c(\-| |_|a|g|p|s|t)|tp)|hu(aw|tc)|i\-(20|go|ma)|i230|iac( |\-|\/)|ibro|idea|ig01|ikom|im1k|inno|ipaq|iris|ja(t|v)a|jbro|jemu|jigs|kddi|keji|kgt( |\/)|klon|kpt |kwc\-|kyo(c|k)|le(no|xi)|lg( g|\/(k|l|u)|50|54|\-[a-w])|libw|lynx|m1\-w|m3ga|m50\/|ma(te|ui|xo)|mc(01|21|ca)|m\-cr|me(rc|ri)|mi(o8|oa|ts)|mmef|mo(01|02|bi|de|do|t(\-| |o|v)|zz)|mt(50|p1|v )|mwbp|mywa|n10[0-2]|n20[2-3]|n30(0|2)|n50(0|2|5)|n7(0(0|1)|10)|ne((c|m)\-|on|tf|wf|wg|wt)|nok(6|i)|nzph|o2im|op(ti|wv)|oran|owg1|p800|pan(a|d|t)|pdxg|pg(13|\-([1-8]|c))|phil|pire|pl(ay|uc)|pn\-2|po(ck|rt|se)|prox|psio|pt\-g|qa\-a|qc(07|12|21|32|60|\-[2-7]|i\-)|qtek|r380|r600|raks|rim9|ro(ve|zo)|s55\/|sa(ge|ma|mm|ms|ny|va)|sc(01|h\-|oo|p\-)|sdk\/|se(c(\-|0|1)|47|mc|nd|ri)|sgh\-|shar|sie(\-|m)|sk\-0|sl(45|id)|sm(al|ar|b3|it|t5)|so(ft|ny)|sp(01|h\-|v\-|v )|sy(01|mb)|t2(18|50)|t6(00|10|18)|ta(gt|lk)|tcl\-|tdg\-|tel(i|m)|tim\-|t\-mo|to(pl|sh)|ts(70|m\-|m3|m5)|tx\-9|up(\.b|g1|si)|utst|v400|v750|veri|vi(rg|te)|vk(40|5[0-3]|\-v)|vm40|voda|vulc|vx(52|53|60|61|70|80|81|83|85|98)|w3c(\-| )|webc|whit|wi(g |nc|nw)|wmlb|wonu|x700|yas\-|your|zeto|zte\-", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                var tablet = new Regex(@"Mobile|iP(hone|od|ad)|Android|BlackBerry|IEMobile|Kindle|NetFront|Silk-Accelerated|(hpw|web)OS|Fennec|Minimo|Opera M(obi|ini)|Blazer|Dolfin|Dolphin|Skyfire|Zune", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                return (b.IsMatch(u) || v.IsMatch(u.Substring(0, 4)) || tablet.IsMatch(u));
            }
            else
            {
                return false;
            }
        }

        public string GenerateJwtTokens(Claim[] claims)
        {
            var issuer = jwtConfig.Issuer;
            var signKey = jwtConfig.SignKey;
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signKey));
            var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha512Signature);
            //var userClaimsIdentity = new ClaimsIdentity(claims);

            //var jwtToken = new SecurityTokenDescriptor
            //{
            //    Issuer = issuer,
            //    Subject = userClaimsIdentity,
            //    Expires = DateTime.Now.AddMinutes(30),
            //    SigningCredentials = signingCredentials
            //};

            //var tokenHandler = new JwtSecurityTokenHandler();
            //var securityToken = tokenHandler.CreateToken(jwtToken);
            //var accessToken = tokenHandler.WriteToken(securityToken);

            var jwtToken = new JwtSecurityToken
            (
                issuer,
                null,
                claims,
                null,
                DateTime.Now.AddDays(30),
                signingCredentials
            );

            var tokenHandler = new JwtSecurityTokenHandler();
            var accessToken = tokenHandler.WriteToken(jwtToken);

            return accessToken;
        }

        public DeviceId GetDeviceId(string url)
        {
            var deviceType = DeviceId.Desktop;

            if (httpContextAccessor.HttpContext.Request.Headers.TryGetValue("ai-device", out var device))
            {
                deviceType = (DeviceId)Enum.Parse(typeof(DeviceId), device);
                return deviceType;
            }

            foreach (var (key, value) in _loginDeviceConfig)
            {
                if (url.Contains(value))
                {
                    deviceType = (DeviceId)Enum.Parse(typeof(DeviceId), key);
                    break;
                }
            }

            return deviceType;
        }

        /// <summary>
        /// CreateBackendToken
        /// </summary>
        /// <returns>後台API Token生成</returns>
        public string CreateBackendToken(object para)
        {
            return MD5Encrypt($"{para}{_backEndKey}");
        }


    }
}
